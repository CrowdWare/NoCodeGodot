namespace Forge.Ai.Imaging;

public sealed record GrokImageEditRequest(
    string Prompt,
    string PoseImagePath,
    string OutputPath,
    string Model = "grok-imagine-image",
    string? StyleImagePath = null,
    string? ExtraImagePath = null,
    string? NegativePrompt = null,
    double? ImageStrength = null,
    double? GuidanceScale = null,
    int? Steps = null,
    string? AspectRatio = null,
    string? Resolution = null,
    double? StyleStrength = null);

public sealed record GrokImageEditResult(
    string OutputPath,
    string SourceUrl,
    string Model);
