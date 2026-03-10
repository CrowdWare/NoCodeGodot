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

namespace Forge.Ai.Chat;

public sealed class GrokChatService
{
    private readonly XaiApiClient _client;

    public GrokChatService(ForgeAiClientOptions options, HttpClient? httpClient = null)
    {
        _client = new XaiApiClient(options, httpClient);
    }

    public async Task<GrokChatResult> CompleteAsync(GrokChatRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Prompt))
        {
            throw new ForgeAiException("Prompt is required.");
        }

        var messages = new List<object>();
        if (!string.IsNullOrWhiteSpace(request.SystemPrompt))
        {
            messages.Add(new { role = "system", content = request.SystemPrompt });
        }

        messages.Add(new { role = "user", content = request.Prompt });

        var payload = new Dictionary<string, object?>
        {
            ["model"] = request.Model,
            ["messages"] = messages,
            ["temperature"] = request.Temperature
        };

        if (request.MaxTokens is not null)
        {
            payload["max_tokens"] = request.MaxTokens.Value;
        }

        using var json = await _client.PostJsonAsync("chat/completions", payload, cancellationToken).ConfigureAwait(false);
        return ParseChatCompletion(json.RootElement);
    }

    public async Task<string> AnalyzeImageAsync(GrokImageAnalysisRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.ImagePath))
        {
            throw new ForgeAiException("ImagePath is required.");
        }

        if (!File.Exists(request.ImagePath))
        {
            throw new FileNotFoundException("Image file not found.", request.ImagePath);
        }

        var imageDataUri = DataUri.FromFile(request.ImagePath);

        var payload = new
        {
            model = request.Model,
            messages = new object[]
            {
                new
                {
                    role = "user",
                    content = new object[]
                    {
                        new { type = "text", text = request.Prompt },
                        new { type = "image_url", image_url = new { url = imageDataUri } }
                    }
                }
            },
            max_tokens = request.MaxTokens
        };

        using var json = await _client.PostJsonAsync("chat/completions", payload, cancellationToken).ConfigureAwait(false);
        var parsed = ParseChatCompletion(json.RootElement);
        return parsed.Content;
    }

    private static GrokChatResult ParseChatCompletion(JsonElement root)
    {
        if (!root.TryGetProperty("choices", out var choices) || choices.ValueKind != JsonValueKind.Array || choices.GetArrayLength() == 0)
        {
            throw new ForgeAiException("Chat response does not contain choices.");
        }

        var first = choices[0];
        var finishReason = first.TryGetProperty("finish_reason", out var finishEl) ? finishEl.GetString() : null;
        if (!first.TryGetProperty("message", out var message) || !message.TryGetProperty("content", out var contentEl))
        {
            throw new ForgeAiException("Chat response does not contain message content.");
        }

        var content = contentEl.GetString();
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new ForgeAiException("Chat response content is empty.");
        }

        var model = root.TryGetProperty("model", out var modelEl) ? modelEl.GetString() ?? string.Empty : string.Empty;
        return new GrokChatResult(content, model, finishReason);
    }
}
