using System;
using System.IO;

namespace ExportGlbPlugin;

public static class EntryPoints
{
    public static string ResolveOutputPath(string savePath)
    {
        if (string.IsNullOrWhiteSpace(savePath))
        {
            return savePath;
        }

        var normalized = savePath.Trim();
        var ext = Path.GetExtension(normalized);
        if (!string.Equals(ext, ".glb", StringComparison.OrdinalIgnoreCase))
        {
            normalized += ".glb";
        }

        return normalized;
    }

    public static bool GetIncludeAnimation(string projectPath)
    {
        return true;
    }

    public static bool GetIncludeProps(string projectPath)
    {
        return true;
    }

    public static bool GetAnimationOnlyCharacter(string projectPath)
    {
        return false;
    }

    public static string DescribePreset(string projectPath)
    {
        return "GLB preset: include animation + props";
    }
}
