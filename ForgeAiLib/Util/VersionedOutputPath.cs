namespace Forge.Ai.Util;

public static class VersionedOutputPath
{
    public static string Resolve(string rawPath)
    {
        if (string.IsNullOrWhiteSpace(rawPath) || !rawPath.Contains("<version>", StringComparison.Ordinal))
        {
            return rawPath;
        }

        var full = Path.GetFullPath(rawPath);
        var directory = Path.GetDirectoryName(full) ?? Directory.GetCurrentDirectory();
        var fileName = Path.GetFileName(full);
        var prefix = fileName.Replace("<version>", string.Empty, StringComparison.Ordinal);
        var stem = Path.GetFileNameWithoutExtension(prefix);
        var extension = Path.GetExtension(prefix);

        Directory.CreateDirectory(directory);

        var maxVersion = 0;
        var existing = Directory.GetFiles(directory, stem + "_*" + extension);
        foreach (var file in existing)
        {
            var name = Path.GetFileNameWithoutExtension(file);
            var idx = name.LastIndexOf('_');
            if (idx >= 0 && idx < name.Length - 1 && int.TryParse(name[(idx + 1)..], out var version))
            {
                maxVersion = Math.Max(maxVersion, version);
            }
        }

        return Path.Combine(directory, $"{stem}_{maxVersion + 1}{extension}");
    }
}
