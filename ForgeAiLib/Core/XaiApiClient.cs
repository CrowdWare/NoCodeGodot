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

using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Forge.Ai.Util;

namespace Forge.Ai.Core;

public sealed class XaiApiClient
{
    private readonly HttpClient _http;
    private readonly ForgeAiClientOptions _options;

    public XaiApiClient(ForgeAiClientOptions options, HttpClient? httpClient = null)
    {
        _options = options;
        _http = httpClient ?? new HttpClient();
    }

    public async Task<JsonDocument> PostJsonAsync(string relativePath, object payload, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, BuildUri(relativePath));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        using var response = await _http.SendAsync(request, cancellationToken).ConfigureAwait(false);
        var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            throw new ForgeAiException($"xAI request failed ({(int)response.StatusCode}): {DataUri.SafePreview(content, 220)}");
        }

        try
        {
            return JsonDocument.Parse(content);
        }
        catch (Exception ex)
        {
            throw new ForgeAiException("xAI response is not valid JSON.", ex);
        }
    }

    public async Task<JsonDocument> GetJsonAsync(string relativeOrAbsolutePath, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, BuildUri(relativeOrAbsolutePath));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);

        using var response = await _http.SendAsync(request, cancellationToken).ConfigureAwait(false);
        var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            throw new ForgeAiException($"xAI request failed ({(int)response.StatusCode}): {DataUri.SafePreview(content, 220)}");
        }

        try
        {
            return JsonDocument.Parse(content);
        }
        catch (Exception ex)
        {
            throw new ForgeAiException("xAI response is not valid JSON.", ex);
        }
    }

    public async Task<byte[]> DownloadBytesAsync(string absoluteUrl, CancellationToken cancellationToken = default)
    {
        if (!Uri.TryCreate(absoluteUrl, UriKind.Absolute, out _))
        {
            throw new ForgeAiException("Download URL must be absolute.");
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, absoluteUrl);
        using var response = await _http.SendAsync(request, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            throw new ForgeAiException($"Download failed ({(int)response.StatusCode}): {DataUri.SafePreview(content, 220)}");
        }

        return await response.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
    }

    private string BuildUri(string relativeOrAbsolutePath)
    {
        if (Uri.TryCreate(relativeOrAbsolutePath, UriKind.Absolute, out var absoluteUri))
        {
            return absoluteUri.ToString();
        }

        var baseUrl = _options.BaseUrl.TrimEnd('/');
        var path = relativeOrAbsolutePath.TrimStart('/');
        return $"{baseUrl}/{path}";
    }
}
