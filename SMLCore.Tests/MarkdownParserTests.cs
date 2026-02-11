using Runtime.Sml;
using Xunit;

namespace SMLCore.Tests;

public class MarkdownParserTests
{
    [Fact]
    public void Parse_WithHeading_CreatesHeadingBlock()
    {
        var result = MarkdownParser.Parse("# Title");

        var block = Assert.Single(result.Blocks);
        Assert.Equal(MarkdownBlockKind.Heading, block.Kind);
        Assert.Equal(1, block.HeadingLevel);
        Assert.Equal("Title", block.Text);
    }

    [Fact]
    public void Parse_WithPropertyBlock_BindsToPreviousElement()
    {
        const string markdown = """
        ![Alt](img.png)
        {
            align: center
            size: 320, 240
        }
        """;

        var result = MarkdownParser.Parse(markdown);

        var block = Assert.Single(result.Blocks);
        Assert.Equal(MarkdownBlockKind.Image, block.Kind);
        Assert.Equal("center", block.Properties["align"]);
        Assert.Equal("320, 240", block.Properties["size"]);
    }

    [Fact]
    public void Parse_WithHardLineBreak_KeepsNewlineInParagraph()
    {
        const string markdown = "Line one  \nLine two";

        var result = MarkdownParser.Parse(markdown);

        var block = Assert.Single(result.Blocks);
        Assert.Equal(MarkdownBlockKind.Paragraph, block.Kind);
        Assert.Equal("Line one\nLine two", block.Text);
    }

    [Fact]
    public void Parse_WithHtml_KeepsHtmlAsPlainText()
    {
        const string markdown = "<div>Hello</div>";

        var result = MarkdownParser.Parse(markdown);

        var block = Assert.Single(result.Blocks);
        Assert.Equal(MarkdownBlockKind.Paragraph, block.Kind);
        Assert.Equal("<div>Hello</div>", block.Text);
    }
}
