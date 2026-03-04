namespace Forge.Ai.Chat;

public sealed record GrokChatRequest(
    string Prompt,
    string? SystemPrompt = null,
    string Model = "grok-4",
    double Temperature = 0.0,
    int? MaxTokens = null);

public sealed record GrokChatResult(
    string Content,
    string Model,
    string? FinishReason);

public sealed record GrokImageAnalysisRequest(
    string ImagePath,
    string Prompt,
    string Model = "grok-4",
    int MaxTokens = 700);
