using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Cors;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis;
using System;

namespace TryCSharp.Controllers;

[ApiController]
[Route("api/[controller]")]
[EnableCors("AllowAll")]
public class CodeController : ControllerBase
{
    [HttpPost("run")]
    public async Task<ActionResult<CodeExecutionResult>> RunCode([FromBody] CodeRequest request)
    {
        try
        {
            var scriptOptions = ScriptOptions.Default
                .WithImports("System", "System.Collections.Generic", "System.Linq", "System.Text", "System.IO", "System.Threading.Tasks")
                .WithReferences(
                    typeof(object).Assembly,
                    typeof(Console).Assembly,
                    typeof(List<>).Assembly,
                    typeof(Enumerable).Assembly);

            var script = CSharpScript.Create(request.Code, scriptOptions);
            var compilation = script.GetCompilation();
            var diagnostics = compilation.GetDiagnostics();
            var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();

            if (errors.Count != 0)
            {
                return Ok(new CodeExecutionResult
                {
                    Success = false,
                    Output = "--- Compilation Error ---\n" + string.Join("\n", from e in errors select e.ToString())
                });
            }

            using var consoleOutput = new StringWriter();
            var originalOut = Console.Out;
            var originalIn = Console.In;
            Console.SetOut(consoleOutput);

            try
            {
                var hasInputs = request.Inputs != null && request.Inputs.Count > 0;
                // Use a custom input reader that can handle multiple inputs
                Console.SetIn(new MultiInputTextReader(request.Inputs ?? throw new InputRequiredException()));

                var result = await script.RunAsync();
                var consoleText = consoleOutput.ToString();
                var output = string.IsNullOrEmpty(consoleText) ? "/* Code executed successfully (no output) */" : consoleText;

                return Ok(new CodeExecutionResult
                {
                    Success = true,
                    Output = output,
                    RequiresInput = false
                });
            }
            catch (InputRequiredException)
            {
                var consoleText = consoleOutput.ToString();
                return Ok(new CodeExecutionResult
                {
                    Success = true,
                    Output = consoleText,
                    RequiresInput = true,
                    InputPrompt = consoleText.TrimEnd('\n', ' ')
                });
            }
            catch (Exception ex)
            {
                return Ok(new CodeExecutionResult
                {
                    Success = false,
                    Output = $"[!] Runtime error: {ex.Message}\n{ex.StackTrace}",
                    RequiresInput = false
                });
            }
            finally
            {
                Console.SetOut(originalOut);
                Console.SetIn(originalIn);
            }
        }
        catch (Exception ex)
        {
            return Ok(new CodeExecutionResult
            {
                Success = false,
                Output = $"[!] Error: {ex.Message}\n{ex.StackTrace}"
            });
        }
    }
}

public class CodeRequest
{
    public string Code { get; set; } = string.Empty;
    public List<string> Inputs { get; set; } = [];
}

public class CodeExecutionResult
{
    public bool Success { get; set; }
    public string Output { get; set; } = string.Empty;
    public bool RequiresInput { get; set; } = false;
    public string? InputPrompt { get; set; } = null;
}

[Serializable]
public class InputRequiredException() : Exception("User input required")
{
}

public class MultiInputTextReader(List<string> inputs) : TextReader
{
    private readonly List<string> _inputs = inputs;
    private int _currentIndex = 0;

    public override string ReadLine()
    {
        if (_currentIndex < _inputs.Count)
        {
            return _inputs[_currentIndex++];
        }
        throw new InputRequiredException();
    }

    public override int Read(char[] buffer, int index, int count)
    {
        var line = ReadLine();
        var chars = line.ToCharArray();
        Array.Copy(chars, 0, buffer, index, Math.Min(chars.Length, count));
        return Math.Min(chars.Length, count);
    }

    public override int Peek()
    {
        if (_currentIndex < _inputs.Count)
        {
            var nextLine = _inputs[_currentIndex];
            return nextLine.Length > 0 ? nextLine[0] : -1;
        }
        return -1;
    }
}