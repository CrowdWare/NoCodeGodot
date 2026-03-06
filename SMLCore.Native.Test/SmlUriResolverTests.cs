using Runtime.Sml;
using Xunit;

namespace SMLCore.Tests;

public sealed class SmlUriResolverTests
{
    [Theory]
    [InlineData("res:/docs/app.sml", "res://docs/app.sml")]
    [InlineData("user:/cache/a.bin", "user://cache/a.bin")]
    [InlineData("ipfs://bafybeicid/path/file.md", "ipfs:/bafybeicid/path/file.md")]
    [InlineData("ipfs:/bafybeicid", "ipfs:/bafybeicid")]
    [InlineData("https://example.com/app.sml", "https://example.com/app.sml")]
    public void Normalize_MapsKnownShortForms(string input, string expected)
    {
        var actual = SmlUriResolver.Normalize(input);
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("res://docs/app.sml", SmlUriSchemeKind.Res)]
    [InlineData("user://cache/app.sml", SmlUriSchemeKind.User)]
    [InlineData("file:///tmp/a.sml", SmlUriSchemeKind.File)]
    [InlineData("http://example.com/a.sml", SmlUriSchemeKind.Http)]
    [InlineData("https://example.com/a.sml", SmlUriSchemeKind.Https)]
    [InlineData("ipfs:/bafybeicid/file", SmlUriSchemeKind.Ipfs)]
    [InlineData("images/pic.png", SmlUriSchemeKind.Relative)]
    public void ClassifyScheme_DetectsExpectedKind(string uri, SmlUriSchemeKind expected)
    {
        var kind = SmlUriResolver.ClassifyScheme(uri);
        Assert.Equal(expected, kind);
    }

    [Fact]
    public void ResolveRelative_ForResBase_ResolvesPathSegments()
    {
        var resolved = SmlUriResolver.ResolveRelative("images/pic.png", "res:/docs/book/ch1.md");
        Assert.Equal("res://docs/book/images/pic.png", resolved);
    }

    [Fact]
    public void ResolveRelative_ForHttpBase_ResolvesPathSegments()
    {
        var resolved = SmlUriResolver.ResolveRelative("images/pic.png", "https://example.com/docs/book/ch1.md");
        Assert.Equal("https://example.com/docs/book/images/pic.png", resolved);
    }

    [Fact]
    public void ResolveRelative_LeavesAbsoluteUrisUntouched()
    {
        var resolved = SmlUriResolver.ResolveRelative("https://cdn.example.com/i.png", "res://docs/ch1.md");
        Assert.Equal("https://cdn.example.com/i.png", resolved);
    }

    [Fact]
    public void MapIpfsToHttp_UsesGateway()
    {
        var mapped = SmlUriResolver.MapIpfsToHttp("ipfs:/bafybeicid/path/file.png", "https://gateway.example/ipfs");
        Assert.Equal("https://gateway.example/ipfs/bafybeicid/path/file.png", mapped);
    }
}