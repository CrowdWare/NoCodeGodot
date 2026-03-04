namespace Forge.Ai.Core;

internal static class PromptComposer
{
    private const string StrictImageBasePrompt =
        "SYSTEM RULES (MANDATORY): Preserve source pose/arrangement, camera framing, scene composition, object proportions, and left-right orientation. " +
        "Do not mirror or swap sides. Preserve subject identity and geometry consistency for all visible entities. Remove all UI/editor artifacts: " +
        "gizmos, handles, axis markers, bone overlays, helper meshes, labels, and text. Use style image only for mood, lighting, color palette, " +
        "and rendering style. Use extra image as material/reference where provided.";

    private const string StrictVideoBasePrompt =
        "SYSTEM RULES (MANDATORY): Preserve motion timing, camera framing, scene composition, object proportions, and left-right orientation " +
        "across all frames. Do not mirror or swap sides. Keep temporal consistency and subject identity. Remove UI/editor artifacts in every frame: " +
        "gizmos, handles, axis markers, bone overlays, helper meshes, labels, and text.";

    internal static string ComposeImagePrompt(string? userPrompt, string? negativePrompt)
    {
        return Compose(StrictImageBasePrompt, userPrompt, negativePrompt);
    }

    internal static string ComposeVideoPrompt(string? userPrompt, string? negativePrompt)
    {
        return Compose(StrictVideoBasePrompt, userPrompt, negativePrompt);
    }

    private static string Compose(string strictBasePrompt, string? userPrompt, string? negativePrompt)
    {
        var merged = strictBasePrompt;

        if (!string.IsNullOrWhiteSpace(userPrompt))
        {
            merged += "\n\nUSER DIRECTIVES:\n" + userPrompt.Trim();
        }

        if (!string.IsNullOrWhiteSpace(negativePrompt))
        {
            merged += "\n\nAVOID:\n" + negativePrompt.Trim();
        }

        return merged;
    }
}
