// Markdown V1 keyword list for runtime code highlighting.
// The runtime parser reads quoted strings from this file.
internal static class MarkdownSyntaxRules
{
    internal static readonly string[] Keywords =
    {
        "#",
        "##",
        "###",
        "*",
        "**",
        "```",
        "-",
        "[",
        "]",
        "(",
        ")"
    };
}