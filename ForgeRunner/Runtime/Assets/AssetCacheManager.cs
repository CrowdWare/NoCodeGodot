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

using Runtime.Logging;
using Runtime.Manifest;
using Runtime.Sml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Runtime.Assets;

public sealed class AssetCacheManager
{
    private static readonly System.Net.Http.HttpClient HttpClient = new();
    private const string MetadataFileName = "metadata.sml";

    private readonly string _cacheRoot;

    public AssetCacheManager()
    {
        var userDataRoot = Godot.ProjectSettings.GlobalizePath("user://");
        _cacheRoot = Path.Combine(userDataRoot, "cache");
    }

    public async Task<AssetSyncPlan> BuildSyncPlanAsync(ManifestDocument manifest, CancellationToken cancellationToken = default)
    {
        var appCacheRoot = GetAppCacheRoot(manifest.SourceManifestUrl);
        var assetsRoot = Path.Combine(appCacheRoot, "files");
        var metadataPath = Path.Combine(appCacheRoot, MetadataFileName);

        Directory.CreateDirectory(assetsRoot);

        var metadata = await LoadMetadataAsync(metadataPath, cancellationToken);
        var previousByPath = metadata.Items.ToDictionary(x => x.RelativePath, StringComparer.OrdinalIgnoreCase);

        var downloadCount = 0;
        long plannedBytes = 0;
        var unknownSizeCount = 0;

        foreach (var asset in manifest.Assets)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var relativePath = NormalizeRelativePath(asset.Path);
            var absolutePath = Path.Combine(assetsRoot, relativePath);

            if (!ShouldDownloadAsset(previousByPath, relativePath, absolutePath, asset.Hash))
            {
                continue;
            }

            downloadCount++;
            if (asset.Size is { } size && size > 0)
            {
                plannedBytes += size;
            }
            else
            {
                unknownSizeCount++;
            }
        }

        return new AssetSyncPlan(downloadCount, plannedBytes, unknownSizeCount);
    }

    public static IReadOnlySet<string> BuildEntryPathSet(ManifestDocument manifest)
    {
        var entry = NormalizeRelativePath(manifest.EntryPoint ?? "app.sml");
        var companion = NormalizeRelativePath(Path.ChangeExtension(entry, ".sms"));
        return new HashSet<string>(StringComparer.OrdinalIgnoreCase) { entry, companion };
    }

    public async Task<AssetSyncResult> SyncAsync(
        ManifestDocument manifest,
        CancellationToken cancellationToken = default,
        IProgress<AssetSyncProgress>? progress = null,
        AssetSyncPlan? planOverride = null,
        IReadOnlySet<string>? entryPathsOnly = null)
    {
        var appCacheRoot = GetAppCacheRoot(manifest.SourceManifestUrl);
        var assetsRoot = Path.Combine(appCacheRoot, "files");
        var metadataPath = Path.Combine(appCacheRoot, MetadataFileName);
        var manifestCachePath = Path.Combine(appCacheRoot, "manifest.sml");

        Directory.CreateDirectory(assetsRoot);

        var metadata = await LoadMetadataAsync(metadataPath, cancellationToken);
        var previousByPath = metadata.Items.ToDictionary(x => x.RelativePath, StringComparer.OrdinalIgnoreCase);
        var updatedItems = new List<AssetCacheItem>(manifest.Assets.Count);

        var manifestContentHash = ComputeSha256Hex(manifest.RawContent);
        var manifestStatus = string.IsNullOrWhiteSpace(metadata.ManifestContentHash)
            ? "missing"
            : HashEquals(metadata.ManifestContentHash, manifestContentHash)
                ? "unchanged"
                : "changed";

        var downloaded = 0;
        var reused = 0;
        var failed = 0;
        long downloadedBytes = 0;
        var completedDownloads = 0;

        var plan = planOverride ?? await BuildSyncPlanAsync(manifest, cancellationToken);
        progress?.Report(new AssetSyncProgress(0, plan.DownloadCount, 0, plan.PlannedBytes, null));

        foreach (var asset in manifest.Assets)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var relativePath = NormalizeRelativePath(asset.Path);

            if (entryPathsOnly is not null && !entryPathsOnly.Contains(relativePath))
            {
                continue;
            }

            var absolutePath = Path.Combine(assetsRoot, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(absolutePath)!);

            var shouldDownload = ShouldDownloadAsset(previousByPath, relativePath, absolutePath, asset.Hash);

            if (shouldDownload)
            {
                try
                {
                    var calculatedHash = await DownloadVerifyWithSingleRetryAsync(asset.Url, absolutePath, asset.Hash, cancellationToken);
                    if (!HashEquals(calculatedHash, asset.Hash))
                    {
                        throw new InvalidDataException($"Hash mismatch for asset '{asset.Id}'. Expected '{asset.Hash}' but got '{calculatedHash}'.");
                    }

                    downloaded++;
                    downloadedBytes += new FileInfo(absolutePath).Length;
                    RunnerLogger.Debug("Assets", $"Downloaded '{asset.Id}' -> '{relativePath}'.");
                }
                catch (Exception ex)
                {
                    failed++;
                    RunnerLogger.Error("Assets", $"Failed to download '{asset.Id}' from '{asset.Url}'", ex);

                    if (previousByPath.TryGetValue(relativePath, out var previous)
                        && File.Exists(absolutePath)
                        && HashEquals(previous.Hash, asset.Hash))
                    {
                        RunnerLogger.Warn("Assets", $"Using last known good cached file for '{relativePath}'.");
                        updatedItems.Add(previous);
                        reused++;
                    }

                    continue;
                }
                finally
                {
                    completedDownloads++;
                    progress?.Report(new AssetSyncProgress(completedDownloads, plan.DownloadCount, downloadedBytes, plan.PlannedBytes, relativePath));
                }
            }
            else
            {
                reused++;
            }

            updatedItems.Add(new AssetCacheItem
            {
                Id = string.IsNullOrWhiteSpace(asset.Id) ? relativePath : asset.Id,
                Hash = asset.Hash,
                RelativePath = relativePath,
                SourceUrl = asset.Url,
                Size = asset.Size
            });
        }

        var cacheHit = reused > 0 || (failed == 0 && downloaded == 0 && manifest.Assets.Count > 0);

        if (entryPathsOnly is not null)
        {
            // Entry-only sync must not wipe cached metadata for non-entry assets.
            var mergedByPath = previousByPath;
            foreach (var item in updatedItems)
            {
                mergedByPath[item.RelativePath] = item;
            }

            metadata.Items = mergedByPath.Values.ToList();
        }
        else
        {
            metadata.Items = updatedItems;
        }
        metadata.ManifestVersion = manifest.Version;
        metadata.ManifestContentHash = manifestContentHash;
        metadata.EntryPoint = NormalizeRelativePath(manifest.EntryPoint ?? "app.sml");
        metadata.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await SaveMetadataAtomicAsync(metadataPath, metadata, cancellationToken);
        await SaveManifestAtomicAsync(manifestCachePath, manifest.RawContent, cancellationToken);

        var entryFileUrl = BuildCachedEntryUrl(assetsRoot, metadata.EntryPoint);

        return new AssetSyncResult(downloaded, reused, failed, downloadedBytes, cacheHit, manifestStatus, entryFileUrl);
    }

    private static bool ShouldDownloadAsset(
        Dictionary<string, AssetCacheItem> previousByPath,
        string relativePath,
        string absolutePath,
        string expectedHash)
    {
        return !previousByPath.TryGetValue(relativePath, out var existing)
               || !HashEquals(existing.Hash, expectedHash)
               || !File.Exists(absolutePath);
    }

    public string? TryGetCachedEntryUrl(string manifestUrl)
    {
        var appCacheRoot = GetAppCacheRoot(manifestUrl);
        var metadataPath = Path.Combine(appCacheRoot, MetadataFileName);
        if (!File.Exists(metadataPath))
        {
            return null;
        }

        try
        {
            var content = File.ReadAllText(metadataPath);
            var metadata = ParseMetadataSml(content);
            if (metadata is null)
            {
                return null;
            }

            var assetsRoot = Path.Combine(appCacheRoot, "files");
            var entry = NormalizeRelativePath(metadata.EntryPoint ?? "app.sml");
            return BuildCachedEntryUrl(assetsRoot, entry);
        }
        catch
        {
            return null;
        }
    }

    public void ClearAllCaches()
    {
        if (Directory.Exists(_cacheRoot))
        {
            Directory.Delete(_cacheRoot, recursive: true);
        }
    }

    public void ClearCacheForManifestUrl(string manifestUrl)
    {
        var appCacheRoot = GetAppCacheRoot(manifestUrl);
        if (Directory.Exists(appCacheRoot))
        {
            Directory.Delete(appCacheRoot, recursive: true);
        }
    }

    private async Task<AssetCacheMetadata> LoadMetadataAsync(string metadataPath, CancellationToken cancellationToken)
    {
        if (!File.Exists(metadataPath))
        {
            return new AssetCacheMetadata();
        }

        try
        {
            var content = await File.ReadAllTextAsync(metadataPath, cancellationToken);
            return ParseMetadataSml(content) ?? new AssetCacheMetadata();
        }
        catch (Exception ex)
        {
            RunnerLogger.Warn("Assets", "Could not read cache metadata SML, rebuilding cache index", ex);
            return new AssetCacheMetadata();
        }
    }

    private async Task SaveMetadataAtomicAsync(string metadataPath, AssetCacheMetadata metadata, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(metadataPath)!);
        var tempPath = metadataPath + ".tmp";

        var sml = BuildMetadataSml(metadata);
        await File.WriteAllTextAsync(tempPath, sml, cancellationToken);

        if (File.Exists(metadataPath))
        {
            File.Delete(metadataPath);
        }

        File.Move(tempPath, metadataPath);
    }

    private static AssetCacheMetadata? ParseMetadataSml(string content)
    {
        var schema = new SmlParserSchema();
        schema.RegisterKnownNode("AssetCacheMetadata");
        schema.RegisterKnownNode("Asset");
        schema.WarnOnUnknownNodes = true;

        var parser = new SmlParser(content, schema);
        var document = parser.ParseDocument();
        if (document.Roots.Count == 0)
        {
            return new AssetCacheMetadata();
        }

        var root = document.Roots[0];
        if (!string.Equals(root.Name, "AssetCacheMetadata", StringComparison.OrdinalIgnoreCase))
        {
            return new AssetCacheMetadata();
        }

        var metadata = new AssetCacheMetadata();
        if (root.TryGetProperty("version", out var versionValue))
        {
            metadata.Version = versionValue.AsIntOrThrow("version");
        }

        if (root.TryGetProperty("updatedAtUtc", out var updatedValue))
        {
            var raw = updatedValue.AsStringOrThrow("updatedAtUtc");
            if (DateTimeOffset.TryParse(raw, out var parsed))
            {
                metadata.UpdatedAtUtc = parsed;
            }
        }

        if (root.TryGetProperty("manifestVersion", out var manifestVersionValue))
        {
            metadata.ManifestVersion = manifestVersionValue.AsStringOrThrow("manifestVersion");
        }

        if (root.TryGetProperty("manifestContentHash", out var manifestHashValue))
        {
            metadata.ManifestContentHash = manifestHashValue.AsStringOrThrow("manifestContentHash");
        }

        if (root.TryGetProperty("entryPoint", out var entryPointValue))
        {
            metadata.EntryPoint = entryPointValue.AsStringOrThrow("entryPoint");
        }

        foreach (var child in root.Children)
        {
            if (!string.Equals(child.Name, "Asset", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var item = new AssetCacheItem
            {
                Id = child.TryGetProperty("id", out var idValue) ? idValue.AsStringOrThrow("id") : string.Empty,
                Hash = child.TryGetProperty("hash", out var hashValue) ? hashValue.AsStringOrThrow("hash") : string.Empty,
                RelativePath = child.TryGetProperty("relativePath", out var pathValue) ? pathValue.AsStringOrThrow("relativePath") : string.Empty,
                SourceUrl = child.TryGetProperty("sourceUrl", out var sourceValue) ? sourceValue.AsStringOrThrow("sourceUrl") : string.Empty
            };

            if (child.TryGetProperty("size", out var sizeValue))
            {
                var sizeRaw = sizeValue.AsStringOrThrow("size");
                if (long.TryParse(sizeRaw, out var parsedSize))
                {
                    item.Size = parsedSize;
                }
            }

            if (!string.IsNullOrWhiteSpace(item.Hash) && !string.IsNullOrWhiteSpace(item.RelativePath))
            {
                metadata.Items.Add(item);
            }
        }

        return metadata;
    }

    private static string BuildMetadataSml(AssetCacheMetadata metadata)
    {
        var builder = new StringBuilder();
        builder.AppendLine("AssetCacheMetadata {");
        builder.AppendLine($"    version: {Math.Max(1, metadata.Version)}");
        builder.AppendLine($"    updatedAtUtc: \"{EscapeSmlString(metadata.UpdatedAtUtc.ToString("O"))}\"");

        if (!string.IsNullOrWhiteSpace(metadata.ManifestVersion))
        {
            builder.AppendLine($"    manifestVersion: \"{EscapeSmlString(metadata.ManifestVersion)}\"");
        }

        if (!string.IsNullOrWhiteSpace(metadata.ManifestContentHash))
        {
            builder.AppendLine($"    manifestContentHash: \"{EscapeSmlString(metadata.ManifestContentHash)}\"");
        }

        if (!string.IsNullOrWhiteSpace(metadata.EntryPoint))
        {
            builder.AppendLine($"    entryPoint: \"{EscapeSmlString(metadata.EntryPoint)}\"");
        }

        foreach (var item in metadata.Items)
        {
            builder.AppendLine("    Asset {");
            builder.AppendLine($"        id: \"{EscapeSmlString(item.Id)}\"");
            builder.AppendLine($"        hash: \"{EscapeSmlString(item.Hash)}\"");
            builder.AppendLine($"        relativePath: \"{EscapeSmlString(item.RelativePath)}\"");
            builder.AppendLine($"        sourceUrl: \"{EscapeSmlString(item.SourceUrl)}\"");
            if (item.Size is { } size)
            {
                builder.AppendLine($"        size: \"{size}\"");
            }
            builder.AppendLine("    }");
        }

        builder.AppendLine("}");
        return builder.ToString();
    }

    private static string EscapeSmlString(string value)
    {
        return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }

    private static async Task SaveManifestAtomicAsync(string manifestPath, string manifestContent, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(manifestPath)!);
        var tempPath = manifestPath + ".tmp";
        await File.WriteAllTextAsync(tempPath, manifestContent, cancellationToken);

        if (File.Exists(manifestPath))
        {
            File.Delete(manifestPath);
        }

        File.Move(tempPath, manifestPath);
    }

    private static async Task DownloadToFileAtomically(string url, string destinationPath, CancellationToken cancellationToken)
    {
        using var response = await HttpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        var tempFile = destinationPath + ".download";
        await using (var input = await response.Content.ReadAsStreamAsync(cancellationToken))
        await using (var output = File.Create(tempFile))
        {
            await input.CopyToAsync(output, cancellationToken);
            await output.FlushAsync(cancellationToken);
        }

        if (File.Exists(destinationPath))
        {
            File.Delete(destinationPath);
        }

        File.Move(tempFile, destinationPath);
    }

    private static async Task<string> ComputeSha256Async(string filePath, CancellationToken cancellationToken)
    {
        using var sha = SHA256.Create();
        await using var stream = File.OpenRead(filePath);
        var hash = await sha.ComputeHashAsync(stream, cancellationToken);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static async Task<string> DownloadVerifyWithSingleRetryAsync(string url, string destinationPath, string expectedHash, CancellationToken cancellationToken)
    {
        await DownloadToFileAtomically(url, destinationPath, cancellationToken);
        var hash = await ComputeSha256Async(destinationPath, cancellationToken);
        if (HashEquals(hash, expectedHash))
        {
            return hash;
        }

        RunnerLogger.Warn("Assets", $"Hash mismatch for '{url}'. Retrying once.");
        await DownloadToFileAtomically(url, destinationPath, cancellationToken);
        return await ComputeSha256Async(destinationPath, cancellationToken);
    }

    private static string ComputeSha256Hex(string input)
    {
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static bool HashEquals(string left, string right)
    {
        return string.Equals(NormalizeHash(left), NormalizeHash(right), StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeHash(string hash)
    {
        return hash.Replace("sha256:", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Trim()
            .ToLowerInvariant();
    }

    private static string NormalizeRelativePath(string path)
    {
        var normalized = path.Replace('\\', '/').TrimStart('/');
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new InvalidDataException("Asset path must not be empty.");
        }

        if (normalized.Contains("..", StringComparison.Ordinal))
        {
            throw new InvalidDataException($"Asset path '{path}' is invalid.");
        }

        return normalized;
    }

    private string GetAppCacheRoot(string manifestUrl)
    {
        Directory.CreateDirectory(_cacheRoot);
        var canonical = CanonicalizeUrl(manifestUrl);
        var urlHash = ComputeSha256Hex(canonical);
        return Path.Combine(_cacheRoot, urlHash);
    }

    private static string CanonicalizeUrl(string url)
    {
        if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return uri.ToString();
        }

        return url.Trim();
    }

    private static string? BuildCachedEntryUrl(string assetsRoot, string entryRelativePath)
    {
        var absolutePath = Path.Combine(assetsRoot, entryRelativePath);
        if (!File.Exists(absolutePath))
        {
            return null;
        }

        return new Uri(absolutePath).AbsoluteUri;
    }
}
