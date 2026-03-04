using System.Text.Json;
using Forge.Ai.Core;
using Forge.Ai.Util;

namespace Forge.Ai.Imaging;

public sealed class GrokImageService
{
    private readonly XaiApiClient _client;

    public GrokImageService(ForgeAiClientOptions options, HttpClient? httpClient = null)
    {
        _client = new XaiApiClient(options, httpClient);
    }

    public async Task<GrokImageEditResult> EditImageAsync(GrokImageEditRequest request, CancellationToken cancellationToken = default)
    {
        ValidateImageInputs(request);

        var generationPrompt = PromptComposer.ComposeImagePrompt(request.Prompt, request.NegativePrompt);

        var imageUrls = BuildImageUrls(request);
        var payload = new Dictionary<string, object?>
        {
            ["model"] = request.Model,
            ["prompt"] = generationPrompt,
            ["images"] = imageUrls.Select(url => new { type = "image_url", url }).ToArray(),
            ["n"] = 1,
            ["response_format"] = "url"
        };

        if (request.ImageStrength.HasValue) payload["image_strength"] = request.ImageStrength.Value;
        if (request.StyleStrength.HasValue) payload["style_strength"] = request.StyleStrength.Value;
        if (request.GuidanceScale.HasValue) payload["guidance_scale"] = request.GuidanceScale.Value;
        if (request.Steps.HasValue) payload["steps"] = request.Steps.Value;
        if (!string.IsNullOrWhiteSpace(request.AspectRatio)) payload["aspect_ratio"] = request.AspectRatio;
        if (!string.IsNullOrWhiteSpace(request.Resolution)) payload["resolution"] = request.Resolution;

        using var json = await _client.PostJsonAsync("images/edits", payload, cancellationToken).ConfigureAwait(false);
        var imageUrl = ExtractOutputUrl(json.RootElement);

        var bytes = await _client.DownloadBytesAsync(imageUrl, cancellationToken).ConfigureAwait(false);
        EnsureOutputDirectory(request.OutputPath);
        await File.WriteAllBytesAsync(request.OutputPath, bytes, cancellationToken).ConfigureAwait(false);

        return new GrokImageEditResult(request.OutputPath, imageUrl, request.Model);
    }

    private static void ValidateImageInputs(GrokImageEditRequest request)
    {
        if (!File.Exists(request.PoseImagePath))
        {
            throw new FileNotFoundException("Pose image not found.", request.PoseImagePath);
        }

        if (!string.IsNullOrWhiteSpace(request.StyleImagePath) && !File.Exists(request.StyleImagePath))
        {
            throw new FileNotFoundException("Style image not found.", request.StyleImagePath);
        }

        if (!string.IsNullOrWhiteSpace(request.ExtraImagePath) && !File.Exists(request.ExtraImagePath))
        {
            throw new FileNotFoundException("Extra image not found.", request.ExtraImagePath);
        }
    }

    private static List<string> BuildImageUrls(GrokImageEditRequest request)
    {
        var urls = new List<string> { DataUri.FromFile(request.PoseImagePath) };
        if (!string.IsNullOrWhiteSpace(request.StyleImagePath)) urls.Add(DataUri.FromFile(request.StyleImagePath));
        if (!string.IsNullOrWhiteSpace(request.ExtraImagePath)) urls.Add(DataUri.FromFile(request.ExtraImagePath));
        return urls;
    }

    private static string ExtractOutputUrl(JsonElement root)
    {
        if (!root.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Array || data.GetArrayLength() == 0)
        {
            throw new ForgeAiException("Image edit response does not contain data[].");
        }

        var first = data[0];
        if (!first.TryGetProperty("url", out var urlEl))
        {
            throw new ForgeAiException("Image edit response does not contain data[0].url.");
        }

        var url = urlEl.GetString();
        if (string.IsNullOrWhiteSpace(url))
        {
            throw new ForgeAiException("Image edit URL is empty.");
        }

        return url;
    }

    private static void EnsureOutputDirectory(string outputPath)
    {
        var dir = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrWhiteSpace(dir))
        {
            Directory.CreateDirectory(dir);
        }
    }
}
