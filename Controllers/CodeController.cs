using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Cors;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis;
using System.Reflection;
using System.Text;

namespace TryCSharp.Controllers;

[ApiController]
[Route("api/[controller]")]
[EnableCors("AllowAll")]
public class CodeController : ControllerBase
{
    private const int MaxExecutionTimeMs = 5000;
    private const int MaxOutputLength = 10000;

    private static readonly HashSet<string> AllowedNamespaces =
    [
        "System",
        "System.Collections.Generic",
        "System.Linq",
        "System.Text",
        "System.Math"
    ];

    private static readonly HashSet<string> BlockedKeywords =
    [
        "unsafe",
        "fixed",
        "stackalloc",
        "extern",
        "ref",
        "out",
        "params",
        "checked",
        "unchecked"
    ];

    private static readonly HashSet<string> BlockedTypeKeywords =
    [
        "Process",
        "Thread",
        "ThreadPool",
        "Task",
        "Parallel",
        "Http",
        "Web",
        "Network",
        "Socket",
        "File",
        "Directory",
        "Path",
        "Stream",
        "Assembly",
        "Reflection",
        "Type",
        "Activator",
        "AppDomain",
        "Marshal",
        "Unsafe",
        "MemoryMappedFile",
        "EventLog",
        "Registry",
        "Service",
        "Management",
        "Cryptography",
        "Security",
        "Principal",
        "Environment"
    ];

    [HttpPost("run")]
    public async Task<ActionResult<CodeExecutionResult>> RunCode([FromBody] CodeRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Code))
            {
                return Ok(new CodeExecutionResult
                {
                    Success = false,
                    Output = "Error: Code cannot be empty"
                });
            }

            var validationResult = ValidateCode(request.Code);
            if (!validationResult.IsValid)
            {
                return Ok(new CodeExecutionResult
                {
                    Success = false,
                    Output = validationResult.ErrorMessage
                });
            }

            var scriptOptions = CreateSecureScriptOptions();
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
            var originalIn = Console.In;
            Console.SetOut(consoleOutput);

            try
            {
                Console.SetIn(new MultiInputTextReader(request.Inputs ?? []));

                var cts = new CancellationTokenSource();
                cts.CancelAfter(MaxExecutionTimeMs);

                var resultTask = script.RunAsync(cancellationToken: cts.Token);
                var completedTask = await Task.WhenAny(resultTask, Task.Delay(Timeout.Infinite, cts.Token));

                if (completedTask != resultTask)
                {
                    return Ok(new CodeExecutionResult
                    {
                        Success = false,
                        Output = "[!] Execution timed out. Code execution is limited to " + MaxExecutionTimeMs / 1000 + " seconds."
                    });
                }

                var result = await resultTask;
                var consoleText = consoleOutput.ToString();
                
                if (consoleText.Length > MaxOutputLength)
                {
                    consoleText = consoleText[..MaxOutputLength] + "\n[!] Output truncated due to length limit.";
                }

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
            catch (OperationCanceledException)
            {
                return Ok(new CodeExecutionResult
                {
                    Success = false,
                    Output = "[!] Execution timed out."
                });
            }
            catch (Exception ex)
            {
                return Ok(new CodeExecutionResult
                {
                    Success = false,
                    Output = $"[!] Runtime error: {ex.Message}"
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
                Output = $"[!] Error: {ex.Message}"
            });
        }
    }

    private static ScriptOptions CreateSecureScriptOptions()
    {
        var allowedAssemblies = new List<Assembly>
        {
            typeof(object).Assembly,
            typeof(string).Assembly,
            typeof(List<>).Assembly,
            typeof(Enumerable).Assembly,
            typeof(StringBuilder).Assembly,
            typeof(Convert).Assembly,
            typeof(Math).Assembly,
            typeof(Console).Assembly
        };

        return ScriptOptions.Default
            .WithImports(AllowedNamespaces)
            .WithReferences(allowedAssemblies)
            .WithMetadataResolver(ScriptMetadataResolver.Default)
            .WithSourceResolver(ScriptSourceResolver.Default);
    }

    private static ValidationResult ValidateCode(string code)
    {
        var codeLower = code.ToLower();

        foreach (var keyword in BlockedKeywords)
        {
            if (ContainsKeyword(code, keyword))
            {
                return new ValidationResult(false, $"Error: Use of '{keyword}' is not allowed");
            }
        }

        foreach (var blockedType in BlockedTypeKeywords)
        {
            if (codeLower.Contains(blockedType, StringComparison.CurrentCultureIgnoreCase))
            {
                return new ValidationResult(false, $"Error: Use of '{blockedType}' is not allowed");
            }
        }

        if (codeLower.Contains("system.io"))
        {
            return new ValidationResult(false, "Error: System.IO namespace is not allowed");
        }

        if (codeLower.Contains("system.reflection"))
        {
            return new ValidationResult(false, "Error: System.Reflection namespace is not allowed");
        }

        if (codeLower.Contains("system.threading"))
        {
            return new ValidationResult(false, "Error: System.Threading namespace is not allowed");
        }

        if (codeLower.Contains("system.diagnostics"))
        {
            return new ValidationResult(false, "Error: System.Diagnostics namespace is not allowed");
        }

        if (codeLower.Contains("system.net"))
        {
            return new ValidationResult(false, "Error: System.Net namespace is not allowed");
        }

        return new ValidationResult(true, string.Empty);
    }

    private static bool ContainsKeyword(string code, string keyword)
    {
        string pattern = @"\b" + keyword + @"\b";
        return System.Text.RegularExpressions.Regex.IsMatch(code, pattern);
    }
}

public class ValidationResult(bool isValid, string errorMessage)
{
    public bool IsValid { get; } = isValid;
    public string ErrorMessage { get; } = errorMessage;
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