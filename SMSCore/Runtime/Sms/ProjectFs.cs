using System.Text;

namespace Runtime.Sms;

public enum ProjectFsErrorCode
{
    NotFound,
    AccessDenied,
    InvalidPath,
    TooLarge,
    AlreadyExists,
    InvalidOperation
}

public sealed class ProjectFsException(ProjectFsErrorCode code, string message, Exception? inner = null)
    : Exception(message, inner)
{
    public ProjectFsErrorCode Code { get; } = code;
}

public sealed record ProjectFsEntry(string Path, string Name, bool IsDirectory);

public sealed class ProjectFs
{
    private readonly string _projectRoot;

    public ProjectFs(string projectRoot, long maxReadBytes = 10 * 1024 * 1024, long maxWriteBytes = 10 * 1024 * 1024)
    {
        if (string.IsNullOrWhiteSpace(projectRoot))
        {
            throw new ArgumentException("Project root must not be empty.", nameof(projectRoot));
        }

        _projectRoot = EnsureDirectoryPath(Path.GetFullPath(projectRoot));
        Directory.CreateDirectory(_projectRoot);
        MaxReadBytes = maxReadBytes;
        MaxWriteBytes = maxWriteBytes;
    }

    public long MaxReadBytes { get; }
    public long MaxWriteBytes { get; }

    public IReadOnlyList<ProjectFsEntry> List(string dir)
    {
        var absolute = ResolveDirectoryPath(dir);
        if (!Directory.Exists(absolute))
        {
            throw new ProjectFsException(ProjectFsErrorCode.NotFound, $"Directory not found: '{dir}'.");
        }

        var entries = new List<ProjectFsEntry>();
        foreach (var child in Directory.EnumerateFileSystemEntries(absolute))
        {
            var name = Path.GetFileName(child);
            var isDir = Directory.Exists(child);
            entries.Add(new ProjectFsEntry(ToRelativePath(child), name, isDir));
        }

        return entries.OrderBy(x => x.IsDirectory ? 0 : 1).ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase).ToList();
    }

    public bool Exists(string path)
    {
        var absolute = ResolvePath(path, requireExistingParent: false);
        return File.Exists(absolute) || Directory.Exists(absolute);
    }

    public string ReadText(string path)
    {
        var absolute = ResolvePath(path, requireExistingTarget: true);
        if (!File.Exists(absolute))
        {
            throw new ProjectFsException(ProjectFsErrorCode.NotFound, $"File not found: '{path}'.");
        }

        var info = new FileInfo(absolute);
        if (info.Length > MaxReadBytes)
        {
            throw new ProjectFsException(ProjectFsErrorCode.TooLarge, $"File '{path}' exceeds read limit ({MaxReadBytes} bytes).");
        }

        return File.ReadAllText(absolute, Encoding.UTF8);
    }

    public void WriteText(string path, string content)
    {
        content ??= string.Empty;
        var bytes = Encoding.UTF8.GetByteCount(content);
        if (bytes > MaxWriteBytes)
        {
            throw new ProjectFsException(ProjectFsErrorCode.TooLarge, $"Write payload exceeds limit ({MaxWriteBytes} bytes).");
        }

        var absolute = ResolvePath(path, requireExistingParent: true);
        var parent = Path.GetDirectoryName(absolute);
        if (string.IsNullOrWhiteSpace(parent) || !Directory.Exists(parent))
        {
            throw new ProjectFsException(ProjectFsErrorCode.NotFound, $"Parent directory does not exist for '{path}'.");
        }

        File.WriteAllText(absolute, content, Encoding.UTF8);
    }

    public void CreateDir(string path)
    {
        var absolute = ResolveDirectoryPath(path, requireExistingParent: true);
        if (Directory.Exists(absolute))
        {
            return;
        }

        Directory.CreateDirectory(absolute);
    }

    public void Delete(string path)
    {
        var absolute = ResolvePath(path, requireExistingTarget: true);
        if (File.Exists(absolute))
        {
            File.Delete(absolute);
            return;
        }

        if (Directory.Exists(absolute))
        {
            if (Directory.EnumerateFileSystemEntries(absolute).Any())
            {
                throw new ProjectFsException(ProjectFsErrorCode.InvalidOperation, "Directory is not empty.");
            }

            Directory.Delete(absolute, recursive: false);
            return;
        }

        throw new ProjectFsException(ProjectFsErrorCode.NotFound, $"Path not found: '{path}'.");
    }

    private string ResolveDirectoryPath(string path, bool requireExistingParent = false)
    {
        var absolute = ResolvePath(path, requireExistingParent: requireExistingParent);
        return absolute;
    }

    private string ResolvePath(string path, bool requireExistingTarget = false, bool requireExistingParent = false)
    {
        var normalized = NormalizeRelativePath(path);
        var combined = Path.GetFullPath(Path.Combine(_projectRoot, normalized));
        EnsureUnderRoot(combined);

        if (requireExistingTarget)
        {
            EnsureExistingPathInsideRoot(combined);
        }
        else if (requireExistingParent)
        {
            var parent = Path.GetDirectoryName(combined);
            if (string.IsNullOrWhiteSpace(parent) || !Directory.Exists(parent))
            {
                throw new ProjectFsException(ProjectFsErrorCode.NotFound, $"Parent directory does not exist for '{path}'.");
            }

            EnsureExistingPathInsideRoot(parent);
        }

        return combined;
    }

    private static string NormalizeRelativePath(string path)
    {
        var raw = (path ?? string.Empty).Replace('\\', '/').Trim();
        if (string.IsNullOrWhiteSpace(raw) || raw == ".")
        {
            return ".";
        }

        if (Path.IsPathRooted(raw) || raw.StartsWith("/", StringComparison.Ordinal))
        {
            throw new ProjectFsException(ProjectFsErrorCode.InvalidPath, "Absolute paths are not allowed.");
        }

        var segments = raw.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (segments.Any(s => s == ".."))
        {
            throw new ProjectFsException(ProjectFsErrorCode.InvalidPath, "Path traversal is not allowed.");
        }

        return string.Join(Path.DirectorySeparatorChar, segments);
    }

    private void EnsureUnderRoot(string absolutePath)
    {
        var normalized = EnsureDirectoryPath(Path.GetFullPath(absolutePath));
        if (!normalized.StartsWith(_projectRoot, StringComparison.Ordinal))
        {
            throw new ProjectFsException(ProjectFsErrorCode.AccessDenied, "Path escapes project root.");
        }
    }

    private void EnsureExistingPathInsideRoot(string existingPath)
    {
        var real = ResolvePathFollowingSymlinks(existingPath);
        var normalizedReal = EnsureDirectoryPath(real);
        if (!normalizedReal.StartsWith(_projectRoot, StringComparison.Ordinal))
        {
            throw new ProjectFsException(ProjectFsErrorCode.AccessDenied, "Path resolves outside project root.");
        }
    }

    private string ResolvePathFollowingSymlinks(string path)
    {
        var absolute = Path.GetFullPath(path);
        if (!File.Exists(absolute) && !Directory.Exists(absolute))
        {
            throw new ProjectFsException(ProjectFsErrorCode.NotFound, $"Path not found: '{path}'.");
        }

        var relative = Path.GetRelativePath(_projectRoot, absolute);
        var segments = relative.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);
        var current = _projectRoot.TrimEnd(Path.DirectorySeparatorChar);

        foreach (var segment in segments)
        {
            current = Path.Combine(current, segment);

            FileSystemInfo? info = Directory.Exists(current)
                ? new DirectoryInfo(current)
                : File.Exists(current)
                    ? new FileInfo(current)
                    : null;

            if (info is null)
            {
                continue;
            }

            if (info.LinkTarget is null)
            {
                continue;
            }

            var target = info.ResolveLinkTarget(returnFinalTarget: true)
                         ?? throw new ProjectFsException(ProjectFsErrorCode.AccessDenied, "Failed to resolve symlink target.");

            current = target.FullName;
        }

        return Path.GetFullPath(current);
    }

    private string ToRelativePath(string absolute)
    {
        var relative = Path.GetRelativePath(_projectRoot, absolute);
        return relative.Replace('\\', '/');
    }

    private static string EnsureDirectoryPath(string path)
    {
        var full = Path.GetFullPath(path);
        if (!full.EndsWith(Path.DirectorySeparatorChar))
        {
            full += Path.DirectorySeparatorChar;
        }

        return full;
    }
}
