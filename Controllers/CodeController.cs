using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Cors;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis;
using System.Diagnostics;

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
                    typeof(System.Collections.Generic.List<>).Assembly,
                    typeof(System.Linq.Enumerable).Assembly);

            var script = CSharpScript.Create(request.Code, scriptOptions);
            var compilation = script.GetCompilation();
            
            var diagnostics = compilation.GetDiagnostics();
            var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
            
            if (errors.Count != 0)
            {
                return Ok(new CodeExecutionResult
                {
                    Success = false,
                    Output = "--- Compilation Error ---\n" + string.Join("\n", errors.Select(e => e.ToString()))
                });
            }

            using var consoleOutput = new StringWriter();
            var originalOut = Console.Out;
            Console.SetOut(consoleOutput);
            
            try
            {
                var result = await script.RunAsync();
                var consoleText = consoleOutput.ToString();
                var output = string.IsNullOrEmpty(consoleText) ? "/* Code executed successfully (no output) */" : consoleText;
                
                return Ok(new CodeExecutionResult
                {
                    Success = true,
                    Output = output
                });
            }
            catch (Exception ex)
            {
                return Ok(new CodeExecutionResult
                {
                    Success = false,
                    Output = $"Runtime error: {ex.Message}\n{ex.StackTrace}"
                });
            }
            finally
            {
                Console.SetOut(originalOut);
            }
        }
        catch (Exception ex)
        {
            return Ok(new CodeExecutionResult
            {
                Success = false,
                Output = $"Error: {ex.Message}\n{ex.StackTrace}"
            });
        }
    }
}

public class CodeRequest
{
    public string Code { get; set; } = string.Empty;
}

public class CodeExecutionResult
{
    public bool Success { get; set; }
    public string Output { get; set; } = string.Empty;
}