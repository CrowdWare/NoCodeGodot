namespace Forge.Ai.Video;

public sealed record GrokVideoStylizeRequest(
    string InputVideoPath,
    string Prompt,
    string OutputPath,
    string Model = "grok-imagine-video",
    string? NegativePrompt = null,
    int PollIntervalMs = 3000,
    int TimeoutSeconds = 600,
    string SubmitEndpoint = "videos/edits",
    string StatusEndpointTemplate = "videos/{id}");

public sealed record GrokVideoStylizeResult(
    string JobId,
    string Status,
    string OutputPath,
    string SourceUrl,
    string Model);
