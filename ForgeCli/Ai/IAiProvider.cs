namespace ForgeCli.Ai;

internal sealed record GenerationCandidate(string MainSml, string MainSms, string? Raw = null);

internal interface IAiProvider
{
    Task<GenerationCandidate> GenerateAsync(string prompt, string feedback, CancellationToken cancellationToken = default);
}
