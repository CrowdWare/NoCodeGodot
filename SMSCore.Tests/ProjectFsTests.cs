using Runtime.Sms;
using Xunit;

namespace SMSCore.Tests;

public sealed class ProjectFsTests : IDisposable
{
    private readonly string _root;

    public ProjectFsTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "smscore-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
    }

    [Fact]
    public void ReadWriteText_WorksInsideRoot()
    {
        var fs = new ProjectFs(_root);
        fs.CreateDir("src");
        fs.WriteText("src/app.sms", "fun main() = 1");

        var text = fs.ReadText("src/app.sms");

        Assert.Equal("fun main() = 1", text);
        Assert.True(fs.Exists("src/app.sms"));
    }

    [Fact]
    public void Traversal_IsRejected()
    {
        var fs = new ProjectFs(_root);

        var ex = Assert.Throws<ProjectFsException>(() => fs.ReadText("../secret.txt"));

        Assert.Equal(ProjectFsErrorCode.InvalidPath, ex.Code);
    }

    [Fact]
    public void AbsolutePath_IsRejected()
    {
        var fs = new ProjectFs(_root);

        var ex = Assert.Throws<ProjectFsException>(() => fs.Exists("/etc/passwd"));

        Assert.Equal(ProjectFsErrorCode.InvalidPath, ex.Code);
    }

    [Fact]
    public void SymlinkEscape_IsRejected()
    {
        var outsideDir = Path.Combine(_root, "..", "outside-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(outsideDir);
        var outsideFile = Path.Combine(outsideDir, "secret.txt");
        File.WriteAllText(outsideFile, "secret");

        var linkPath = Path.Combine(_root, "escape-link");
        try
        {
            Directory.CreateSymbolicLink(linkPath, outsideDir);
        }
        catch
        {
            // Symbolic links may be restricted on some environments.
            return;
        }

        var fs = new ProjectFs(_root);
        var ex = Assert.Throws<ProjectFsException>(() => fs.ReadText("escape-link/secret.txt"));
        Assert.Equal(ProjectFsErrorCode.AccessDenied, ex.Code);
    }

    [Fact]
    public void SizeLimits_AreEnforced()
    {
        var fs = new ProjectFs(_root, maxReadBytes: 4, maxWriteBytes: 4);
        fs.WriteText("tiny.txt", "1234");

        var writeEx = Assert.Throws<ProjectFsException>(() => fs.WriteText("too-large.txt", "12345"));
        Assert.Equal(ProjectFsErrorCode.TooLarge, writeEx.Code);

        File.WriteAllText(Path.Combine(_root, "read-too-large.txt"), "12345");
        var readEx = Assert.Throws<ProjectFsException>(() => fs.ReadText("read-too-large.txt"));
        Assert.Equal(ProjectFsErrorCode.TooLarge, readEx.Code);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }
}
