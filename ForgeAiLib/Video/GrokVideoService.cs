using System.Text.Json;
using Forge.Ai.Core;
using Forge.Ai.Util;

namespace Forge.Ai.Video;

public sealed class GrokVideoService
{
    private static readonly HashSet<string> TerminalStates = new(StringComparer.OrdinalIgnoreCase)
    {
        "succeeded",
        "completed",
        "done",
        "failed",
        "error",
        "cancelled"
    };

    private readonly XaiApiClient _client;

    public GrokVideoService(ForgeAiClientOptions options, HttpClient? httpClient = null)
    {
        _client = new XaiApiClient(options, httpClient);
    }

    public async Task<GrokVideoStylizeResult> StylizeVideoAsync(GrokVideoStylizeRequest request, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(request.InputVideoPath))
        {
            throw new FileNotFoundException("Input video not found.", request.InputVideoPath);
        }

        if (string.IsNullOrWhiteSpace(request.Prompt))
        {
            throw new ForgeAiException("Prompt is required.");
        }

        var payload = new Dictionary<string, object?>
        {
            ["model"] = request.Model,
            ["prompt"] = request.Prompt,
            ["video"] = DataUri.FromFile(request.InputVideoPath)
        };

        if (!string.IsNullOrWhiteSpace(request.NegativePrompt))
        {
            payload["negative_prompt"] = request.NegativePrompt;
        }

        using var submitResponse = await _client.PostJsonAsync(request.SubmitEndpoint, payload, cancellationToken).ConfigureAwait(false);
        var jobId = ExtractString(submitResponse.RootElement, "id")
                    ?? ExtractString(submitResponse.RootElement, "job_id")
                    ?? throw new ForgeAiException("Video submit response does not contain job id.");

        var deadline = DateTimeOffset.UtcNow.AddSeconds(Math.Max(1, request.TimeoutSeconds));
        string? finalStatus = null;
        string? outputUrl = ExtractOutputUrl(submitResponse.RootElement);

        while (DateTimeOffset.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var statusEndpoint = request.StatusEndpointTemplate.Replace("{id}", Uri.EscapeDataString(jobId), StringComparison.Ordinal);
            using var statusJson = await _client.GetJsonAsync(statusEndpoint, cancellationToken).ConfigureAwait(false);

            finalStatus = ExtractString(statusJson.RootElement, "status") ?? "unknown";
            outputUrl ??= ExtractOutputUrl(statusJson.RootElement);

            if (TerminalStates.Contains(finalStatus))
            {
                break;
            }

            await Task.Delay(Math.Max(250, request.PollIntervalMs), cancellationToken).ConfigureAwait(false);
        }

        if (string.IsNullOrWhiteSpace(finalStatus))
        {
            throw new ForgeAiException("Video job status polling failed: no status received.");
        }

        if (!finalStatus.Equals("succeeded", StringComparison.OrdinalIgnoreCase)
            && !finalStatus.Equals("completed", StringComparison.OrdinalIgnoreCase)
            && !finalStatus.Equals("done", StringComparison.OrdinalIgnoreCase))
        {
            throw new ForgeAiException($"Video job '{jobId}' ended with status '{finalStatus}'.");
        }

        if (string.IsNullOrWhiteSpace(outputUrl))
        {
            throw new ForgeAiException($"Video job '{jobId}' succeeded but no output URL was returned.");
        }

        var bytes = await _client.DownloadBytesAsync(outputUrl, cancellationToken).ConfigureAwait(false);
        var directory = Path.GetDirectoryName(request.OutputPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await File.WriteAllBytesAsync(request.OutputPath, bytes, cancellationToken).ConfigureAwait(false);
        return new GrokVideoStylizeResult(jobId, finalStatus, request.OutputPath, outputUrl, request.Model);
    }

    private static string? ExtractOutputUrl(JsonElement root)
    {
        // Common formats:
        // { output_url: "..." }
        // { result: { url: "..." } }
        // { data: [{ url: "..." }] }
        if (TryGetString(root, "output_url", out var direct)) return direct;

        if (root.TryGetProperty("result", out var resultObj)
            && resultObj.ValueKind == JsonValueKind.Object
            && TryGetString(resultObj, "url", out var resultUrl))
        {
            return resultUrl;
        }

        if (root.TryGetProperty("data", out var data)
            && data.ValueKind == JsonValueKind.Array
            && data.GetArrayLength() > 0
            && data[0].ValueKind == JsonValueKind.Object
            && TryGetString(data[0], "url", out var dataUrl))
        {
            return dataUrl;
        }

        return null;
    }

    private static string? ExtractString(JsonElement root, string key)
    {
        return TryGetString(root, key, out var value) ? value : null;
    }

    private static bool TryGetString(JsonElement root, string key, out string? value)
    {
        value = null;
        if (!root.TryGetProperty(key, out var element))
        {
            return false;
        }

        if (element.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        value = element.GetString();
        return !string.IsNullOrWhiteSpace(value);
    }
}
