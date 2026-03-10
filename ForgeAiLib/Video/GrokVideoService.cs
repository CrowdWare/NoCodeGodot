/*
#############################################################################
# Copyright (C) 2026 CrowdWare
#
# This file is part of Forge.
#
# SPDX-License-Identifier: GPL-3.0-or-later OR LicenseRef-CrowdWare-Commercial
#
# Forge is free software: you can redistribute it and/or modify
# it under the terms of the GNU General Public License as published by
# the Free Software Foundation, either version 3 of the License, or
# (at your option) any later version.
#
# Forge is distributed in the hope that it will be useful,
# but WITHOUT ANY WARRANTY; without even the implied warranty of
# MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
# GNU General Public License for more details.
#
# You should have received a copy of the GNU General Public License
# along with Forge. If not, see <https://www.gnu.org/licenses/>.
#
# Commercial licensing is available from CrowdWare for proprietary use.
#############################################################################
*/

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

        var videoDataUri = DataUri.FromFile(request.InputVideoPath);
        var generationPrompt = PromptComposer.ComposeVideoPrompt(request.Prompt, request.NegativePrompt);
        var payload = new Dictionary<string, object?>
        {
            ["model"] = request.Model,
            ["prompt"] = generationPrompt,
            // Keep both fields to be compatible with response variants/endpoints.
            ["video_url"] = videoDataUri,
            ["video"] = new Dictionary<string, object?>
            {
                ["url"] = videoDataUri
            }
        };

        using var submitResponse = await SubmitWithRouteFallbackAsync(request.SubmitEndpoint, payload, cancellationToken).ConfigureAwait(false);
        var directUrl = ExtractOutputUrl(submitResponse.RootElement);
        if (!string.IsNullOrWhiteSpace(directUrl))
        {
            var directBytes = await _client.DownloadBytesAsync(directUrl, cancellationToken).ConfigureAwait(false);
            var directDirectory = Path.GetDirectoryName(request.OutputPath);
            if (!string.IsNullOrWhiteSpace(directDirectory))
            {
                Directory.CreateDirectory(directDirectory);
            }

            await File.WriteAllBytesAsync(request.OutputPath, directBytes, cancellationToken).ConfigureAwait(false);
            return new GrokVideoStylizeResult("direct", "done", request.OutputPath, directUrl, request.Model);
        }

        var jobId = ExtractString(submitResponse.RootElement, "request_id")
                    ?? ExtractString(submitResponse.RootElement, "id")
                    ?? ExtractString(submitResponse.RootElement, "job_id")
                    ?? ExtractNestedString(submitResponse.RootElement, "response", "request_id")
                    ?? throw new ForgeAiException("Video submit response does not contain request/job id or direct output URL.");

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
        // { video: { url: "..." } }
        // { result: { url: "..." } }
        // { data: [{ url: "..." }] }
        if (TryGetString(root, "output_url", out var direct)) return direct;

        if (root.TryGetProperty("video", out var videoObj)
            && videoObj.ValueKind == JsonValueKind.Object
            && TryGetString(videoObj, "url", out var videoUrl))
        {
            return videoUrl;
        }

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

    private static string? ExtractNestedString(JsonElement root, string parentKey, string childKey)
    {
        if (root.TryGetProperty(parentKey, out var parent)
            && parent.ValueKind == JsonValueKind.Object
            && TryGetString(parent, childKey, out var value))
        {
            return value;
        }

        return null;
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

    private async Task<JsonDocument> SubmitWithRouteFallbackAsync(string preferredEndpoint, object payload, CancellationToken cancellationToken)
    {
        var candidates = new List<string>();
        if (!string.IsNullOrWhiteSpace(preferredEndpoint))
        {
            candidates.Add(preferredEndpoint);
        }

        // Keep known variants as fallback for API differences across environments.
        foreach (var fallback in new[] { "videos/edits", "videos/generations", "videos" })
        {
            if (!candidates.Contains(fallback, StringComparer.OrdinalIgnoreCase))
            {
                candidates.Add(fallback);
            }
        }

        ForgeAiException? last = null;
        foreach (var endpoint in candidates)
        {
            try
            {
                return await _client.PostJsonAsync(endpoint, payload, cancellationToken).ConfigureAwait(false);
            }
            catch (ForgeAiException ex)
            {
                last = ex;
                if (!ex.Message.Contains("No handler found on route", StringComparison.OrdinalIgnoreCase)
                    && !ex.Message.Contains("(404)", StringComparison.OrdinalIgnoreCase))
                {
                    throw;
                }
            }
        }

        throw last ?? new ForgeAiException("Video submit failed on all known endpoints.");
    }
}
