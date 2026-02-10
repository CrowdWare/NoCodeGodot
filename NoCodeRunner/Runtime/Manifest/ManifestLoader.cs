using Runtime.Logging;
using Runtime.Sml;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Runtime.Manifest;

public sealed class ManifestLoader
{
    private static readonly HttpClient HttpClient = new();

    public async Task<ManifestDocument> LoadAsync(string manifestUrl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(manifestUrl))
        {
            throw new ArgumentException("Manifest URL must not be empty.", nameof(manifestUrl));
        }

        var content = await DownloadManifestAsync(manifestUrl, cancellationToken);
        return ParseManifest(content, manifestUrl);
    }

    private static async Task<string> DownloadManifestAsync(string manifestUrl, CancellationToken cancellationToken)
    {
        using var response = await HttpClient.GetAsync(manifestUrl, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Failed to load manifest from '{manifestUrl}'. HTTP {(int)response.StatusCode}");
        }

        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    private static ManifestDocument ParseManifest(string content, string sourceManifestUrl)
    {
        var schema = new SmlParserSchema();
        schema.RegisterKnownNode("Manifest");
        schema.RegisterKnownNode("Asset");
        schema.WarnOnUnknownNodes = true;

        var parser = new SmlParser(content, schema);
        var document = parser.ParseDocument();
        foreach (var warning in document.Warnings)
        {
            RunnerLogger.Warn("Manifest", warning);
        }

        if (document.Roots.Count == 0)
        {
            throw new SmlParseException("Manifest is empty.");
        }

        var root = document.Roots[0];
        if (!string.Equals(root.Name, "Manifest", StringComparison.OrdinalIgnoreCase))
        {
            throw new SmlParseException($"Expected root node 'Manifest', but found '{root.Name}'.");
        }

        var version = root.TryGetProperty("version", out var versionValue)
            ? versionValue.AsIntOrThrow("version")
            : 1;

        var baseUrl = root.TryGetProperty("baseUrl", out var baseUrlValue)
            ? baseUrlValue.AsStringOrThrow("baseUrl")
            : null;

        var entryPoint = root.TryGetProperty("entryPoint", out var entryValue)
            ? entryValue.AsStringOrThrow("entryPoint")
            : null;

        var assets = new List<ManifestAssetEntry>();
        foreach (var child in root.Children)
        {
            if (!string.Equals(child.Name, "Asset", StringComparison.OrdinalIgnoreCase))
            {
                RunnerLogger.Warn("Manifest", $"Ignoring unsupported node '{child.Name}' in manifest (line {child.Line}).");
                continue;
            }

            var id = child.GetRequiredProperty("id").AsStringOrThrow("id");
            var path = child.GetRequiredProperty("path").AsStringOrThrow("path");
            var hash = child.GetRequiredProperty("hash").AsStringOrThrow("hash");

            var rawUrl = child.TryGetProperty("url", out var urlValue)
                ? urlValue.AsStringOrThrow("url")
                : path;

            var resolvedUrl = ResolveAssetUrl(sourceManifestUrl, baseUrl, rawUrl);

            var type = child.TryGetProperty("type", out var typeValue)
                ? typeValue.AsStringOrThrow("type")
                : null;

            long? size = null;
            if (child.TryGetProperty("size", out var sizeValue))
            {
                size = sizeValue.AsLongOrThrow("size");
            }

            assets.Add(new ManifestAssetEntry
            {
                Id = id,
                Path = path,
                Hash = hash,
                Url = resolvedUrl,
                Type = type,
                Size = size
            });
        }

        if (assets.Count == 0)
        {
            RunnerLogger.Warn("Manifest", "Manifest contains no assets.");
        }

        return new ManifestDocument
        {
            Version = version,
            BaseUrl = baseUrl,
            EntryPoint = entryPoint,
            SourceManifestUrl = sourceManifestUrl,
            Assets = assets
        };
    }

    private static string ResolveAssetUrl(string sourceManifestUrl, string? baseUrl, string rawUrl)
    {
        if (Uri.TryCreate(rawUrl, UriKind.Absolute, out var absolute))
        {
            return absolute.ToString();
        }

        if (!string.IsNullOrWhiteSpace(baseUrl) && Uri.TryCreate(baseUrl, UriKind.Absolute, out var baseAbsolute))
        {
            return new Uri(baseAbsolute, rawUrl).ToString();
        }

        if (!Uri.TryCreate(sourceManifestUrl, UriKind.Absolute, out var sourceUri))
        {
            throw new SmlParseException($"Manifest URL '{sourceManifestUrl}' is invalid.");
        }

        return new Uri(sourceUri, rawUrl).ToString();
    }
}
