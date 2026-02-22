/*
 * Copyright (C) 2026 CrowdWare
 *
 * This file is part of ForgeRunner.
 *
 *  ForgeRunner is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  ForgeRunner is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with ForgeRunner.  If not, see <http://www.gnu.org/licenses/>.
 */

using Godot;
using Runtime.Logging;
using Runtime.Sml;
using System;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Runtime.Assets;

public sealed class RunnerUriResolver
{
    private static readonly System.Net.Http.HttpClient HttpClient = new();

    private readonly string _userRootAbsolute;
    private readonly string _cacheRootAbsolute;
    private readonly string _cacheRootUserUri = "user://cache/uri";

    public RunnerUriResolver(string? ipfsGateway = null)
    {
        _userRootAbsolute = ProjectSettings.GlobalizePath("user://");
        _cacheRootAbsolute = Path.Combine(_userRootAbsolute, "cache", "uri");
        Directory.CreateDirectory(_cacheRootAbsolute);
        IpfsGateway = string.IsNullOrWhiteSpace(ipfsGateway)
            ? SmlUriResolver.DefaultIpfsGateway
            : ipfsGateway.Trim().TrimEnd('/');
    }

    public string IpfsGateway { get; }

    public string Normalize(string rawUri) => SmlUriResolver.Normalize(rawUri);

    public string ResolveReference(string rawUri, string? baseUri = null)
    {
        var normalized = Normalize(rawUri);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return normalized;
        }

        var referenceKind = SmlUriResolver.ClassifyScheme(normalized);
        if (referenceKind != SmlUriSchemeKind.Relative)
        {
            if (referenceKind == SmlUriSchemeKind.Res
                && TryResolveBaseDirectory(baseUri, out var baseDirectory))
            {
                var localRelative = normalized["res://".Length..].TrimStart('/', '\\');
                var combined = Path.GetFullPath(Path.Combine(baseDirectory, localRelative));
                return NormalizeFilePathIfNeeded(combined);
            }

            return NormalizeFilePathIfNeeded(normalized);
        }

        if (string.IsNullOrWhiteSpace(baseUri))
        {
            return normalized;
        }

        var resolved = SmlUriResolver.ResolveRelative(normalized, baseUri);
        return NormalizeFilePathIfNeeded(resolved);
    }

    public async Task<string> ResolveForResourceLoadAsync(string rawUri, string? baseUri = null, CancellationToken cancellationToken = default)
    {
        var resolved = ResolveReference(rawUri, baseUri);
        if (string.IsNullOrWhiteSpace(resolved))
        {
            return resolved;
        }

        var kind = SmlUriResolver.ClassifyScheme(resolved);
        if (SmlUriResolver.IsLocalScheme(kind))
        {
            return resolved;
        }

        if (!SmlUriResolver.IsRemoteScheme(kind))
        {
            return resolved;
        }

        var cached = await CacheRemoteAsync(resolved, kind, cancellationToken);
        RunnerLogger.Info("URI", $"Resolved remote URI '{resolved}' -> '{cached}'.");
        return cached;
    }

    public async Task<string> LoadTextAsync(string rawUri, string? baseUri = null, CancellationToken cancellationToken = default)
    {
        var resolved = ResolveReference(rawUri, baseUri);
        if (string.IsNullOrWhiteSpace(resolved))
        {
            throw new ArgumentException("URI must not be empty.", nameof(rawUri));
        }

        var kind = SmlUriResolver.ClassifyScheme(resolved);
        if (SmlUriResolver.IsRemoteScheme(kind))
        {
            var cached = await CacheRemoteAsync(resolved, kind, cancellationToken);
            var absoluteCached = ProjectSettings.GlobalizePath(cached);
            return await File.ReadAllTextAsync(absoluteCached, cancellationToken);
        }

        var absolute = ToAbsolutePath(resolved, kind);
        if (!File.Exists(absolute))
        {
            throw new FileNotFoundException($"File not found for URI '{resolved}'.", absolute);
        }

        return await File.ReadAllTextAsync(absolute, cancellationToken);
    }

    private async Task<string> CacheRemoteAsync(string resolvedRemoteUri, SmlUriSchemeKind kind, CancellationToken cancellationToken)
    {
        var downloadUri = kind == SmlUriSchemeKind.Ipfs
            ? SmlUriResolver.MapIpfsToHttp(resolvedRemoteUri, IpfsGateway)
            : resolvedRemoteUri;

        var hash = ComputeSha256Hex(resolvedRemoteUri);
        var extension = ExtractExtension(downloadUri);
        var userPath = $"{_cacheRootUserUri}/{hash}{extension}";
        var absolutePath = ProjectSettings.GlobalizePath(userPath);

        if (File.Exists(absolutePath))
        {
            return userPath;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(absolutePath)!);

        var tempPath = absolutePath + ".tmp";
        try
        {
            using var response = await HttpClient.GetAsync(downloadUri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();

            await using (var input = await response.Content.ReadAsStreamAsync(cancellationToken))
            await using (var output = File.Create(tempPath))
            {
                await input.CopyToAsync(output, cancellationToken);
                await output.FlushAsync(cancellationToken);
            }

            if (File.Exists(absolutePath))
            {
                File.Delete(absolutePath);
            }

            File.Move(tempPath, absolutePath);
            return userPath;
        }
        catch
        {
            if (File.Exists(absolutePath))
            {
                return userPath;
            }

            throw;
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }

    private static string ExtractExtension(string uri)
    {
        if (!Uri.TryCreate(uri, UriKind.Absolute, out var parsed))
        {
            return string.Empty;
        }

        var extension = Path.GetExtension(parsed.AbsolutePath);
        return string.IsNullOrWhiteSpace(extension) ? string.Empty : extension.ToLowerInvariant();
    }

    private static string ComputeSha256Hex(string input)
    {
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string NormalizeFilePathIfNeeded(string uriOrPath)
    {
        if (Path.IsPathRooted(uriOrPath))
        {
            return new Uri(uriOrPath).AbsoluteUri;
        }

        return uriOrPath;
    }

    private static bool TryResolveBaseDirectory(string? baseUri, out string directory)
    {
        directory = string.Empty;
        if (string.IsNullOrWhiteSpace(baseUri))
        {
            return false;
        }

        var normalizedBase = SmlUriResolver.Normalize(baseUri);
        var baseKind = SmlUriResolver.ClassifyScheme(normalizedBase);
        if (baseKind != SmlUriSchemeKind.File)
        {
            return false;
        }

        if (Uri.TryCreate(normalizedBase, UriKind.Absolute, out var fileUri))
        {
            directory = Path.GetDirectoryName(fileUri.LocalPath) ?? string.Empty;
            return !string.IsNullOrWhiteSpace(directory);
        }

        if (Path.IsPathRooted(normalizedBase))
        {
            directory = Path.GetDirectoryName(normalizedBase) ?? string.Empty;
            return !string.IsNullOrWhiteSpace(directory);
        }

        return false;
    }

    private static string ToAbsolutePath(string resolved, SmlUriSchemeKind kind)
    {
        return kind switch
        {
            SmlUriSchemeKind.Res or SmlUriSchemeKind.User => ProjectSettings.GlobalizePath(resolved),
            SmlUriSchemeKind.File => Uri.TryCreate(resolved, UriKind.Absolute, out var fileUri)
                ? fileUri.LocalPath
                : resolved,
            _ => resolved
        };
    }
}