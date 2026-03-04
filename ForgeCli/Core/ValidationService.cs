using Runtime.Sml;
using Runtime.Sms;

namespace ForgeCli.Core;

internal sealed record FileValidationResult(string FilePath, bool IsValid, string? Error, IReadOnlyList<string> Warnings);

internal sealed record ValidationReport(IReadOnlyList<FileValidationResult> Files)
{
    public bool IsValid => Files.All(f => f.IsValid);
    public int WarningCount => Files.Sum(f => f.Warnings.Count);
}

internal static class ValidationService
{
    public static ValidationReport ValidateProject(string projectDirectory)
    {
        var root = Path.GetFullPath(projectDirectory);
        if (!Directory.Exists(root))
        {
            throw new InvalidOperationException($"Project directory does not exist: {root}");
        }

        var files = new List<FileValidationResult>
        {
            ValidateSml(Path.Combine(root, "app.sml")),
            ValidateSml(Path.Combine(root, "main.sml")),
            ValidateSms(Path.Combine(root, "main.sms"))
        };

        return new ValidationReport(files);
    }

    public static FileValidationResult ValidateSml(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return new FileValidationResult(filePath, false, "File not found.", []);
        }

        try
        {
            var doc = new SmlParser(File.ReadAllText(filePath)).ParseDocument();
            return new FileValidationResult(filePath, true, null, doc.Warnings);
        }
        catch (Exception ex)
        {
            return new FileValidationResult(filePath, false, ex.Message, []);
        }
    }

    public static FileValidationResult ValidateSms(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return new FileValidationResult(filePath, false, "File not found.", []);
        }

        try
        {
            new ScriptEngine().ValidateSyntax(File.ReadAllText(filePath));
            return new FileValidationResult(filePath, true, null, []);
        }
        catch (Exception ex)
        {
            return new FileValidationResult(filePath, false, ex.Message, []);
        }
    }

    public static void PrintReport(ValidationReport report, bool verbose)
    {
        foreach (var file in report.Files)
        {
            var status = file.IsValid ? "OK" : "FAIL";
            Console.WriteLine($"[{status}] {file.FilePath}");

            if (!file.IsValid && file.Error is not null)
            {
                Console.WriteLine($"  error: {file.Error}");
            }

            if (verbose && file.Warnings.Count > 0)
            {
                foreach (var warning in file.Warnings)
                {
                    Console.WriteLine($"  warning: {warning}");
                }
            }
        }

        Console.WriteLine();
        Console.WriteLine(report.IsValid
            ? $"Validation passed ({report.WarningCount} warning(s))."
            : "Validation failed.");
    }
}
