using ForgeCli.Core;

namespace ForgeCli.Ai;

internal sealed record GenerationRequest(string ProjectRoot, string Prompt, int MaxIterations, bool DryRun);

internal sealed record GenerationResult(
    bool Success,
    int IterationsUsed,
    string Feedback,
    IReadOnlyList<string> Warnings,
    string MainSml,
    string MainSms);

internal sealed class GenerationRunner(IAiProvider provider)
{
    public async Task<GenerationResult> RunAsync(GenerationRequest request, CancellationToken cancellationToken = default)
    {
        var feedback = "No previous attempt.";
        var latestSml = string.Empty;
        var latestSms = string.Empty;
        var latestWarnings = new List<string>();

        for (var iteration = 1; iteration <= request.MaxIterations; iteration++)
        {
            Console.WriteLine($"AI iteration {iteration}/{request.MaxIterations}...");
            var candidate = await provider.GenerateAsync(request.Prompt, feedback, cancellationToken);
            latestSml = candidate.MainSml;
            latestSms = candidate.MainSms;

            var tempDir = Path.Combine(Path.GetTempPath(), "forgecli", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            var mainSmlPath = Path.Combine(tempDir, "main.sml");
            var mainSmsPath = Path.Combine(tempDir, "main.sms");
            File.WriteAllText(mainSmlPath, latestSml);
            File.WriteAllText(mainSmsPath, latestSms);

            var smlResult = ValidationService.ValidateSml(mainSmlPath);
            var smsResult = ValidationService.ValidateSms(mainSmsPath);

            latestWarnings = [.. smlResult.Warnings];

            if (smlResult.IsValid && smsResult.IsValid)
            {
                if (!request.DryRun)
                {
                    File.WriteAllText(Path.Combine(request.ProjectRoot, "main.sml"), latestSml);
                    File.WriteAllText(Path.Combine(request.ProjectRoot, "main.sms"), latestSms);
                }

                var summary = latestWarnings.Count == 0
                    ? "Validation passed."
                    : $"Validation passed with {latestWarnings.Count} warning(s).";

                return new GenerationResult(true, iteration, summary, latestWarnings, latestSml, latestSms);
            }

            feedback = BuildFeedback(smlResult, smsResult);
        }

        return new GenerationResult(false, request.MaxIterations, feedback, latestWarnings, latestSml, latestSms);
    }

    private static string BuildFeedback(FileValidationResult sml, FileValidationResult sms)
    {
        var lines = new List<string>
        {
            "Previous generation failed validation.",
            sml.IsValid ? "main.sml: OK" : $"main.sml error: {sml.Error}",
            sms.IsValid ? "main.sms: OK" : $"main.sms error: {sms.Error}"
        };

        if (sml.Warnings.Count > 0)
        {
            lines.Add("main.sml warnings:");
            lines.AddRange(sml.Warnings.Select(w => "- " + w));
        }

        lines.Add("Please return corrected JSON only.");
        return string.Join(Environment.NewLine, lines);
    }

    public static void PrintResult(GenerationResult result)
    {
        Console.WriteLine();
        Console.WriteLine(result.Success
            ? $"Generation succeeded after {result.IterationsUsed} iteration(s)."
            : $"Generation failed after {result.IterationsUsed} iteration(s).");

        Console.WriteLine(result.Feedback);

        if (result.Warnings.Count > 0)
        {
            foreach (var warning in result.Warnings)
            {
                Console.WriteLine($"warning: {warning}");
            }
        }
    }
}
