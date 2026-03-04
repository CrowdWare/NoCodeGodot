namespace Forge.Ai.Util;

public static class DataUri
{
    public static string FromFile(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Path is required.", nameof(path));
        }

        if (!File.Exists(path))
        {
            throw new FileNotFoundException("Input file not found.", path);
        }

        var mimeType = GetMimeType(path);
        var bytes = File.ReadAllBytes(path);
        var base64 = Convert.ToBase64String(bytes);
        return $"data:{mimeType};base64,{base64}";
    }

    public static string GetMimeType(string path)
    {
        return Path.GetExtension(path).ToLowerInvariant() switch
        {
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".webp" => "image/webp",
            ".gif" => "image/gif",
            ".bmp" => "image/bmp",
            ".mp4" => "video/mp4",
            ".mov" => "video/quicktime",
            ".webm" => "video/webm",
            _ => "application/octet-stream"
        };
    }

    public static string SafePreview(string value, int maxLength = 96)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        var normalized = value.Replace('\n', ' ').Replace('\r', ' ');
        return normalized.Length <= maxLength ? normalized : normalized[..maxLength] + "...";
    }
}
