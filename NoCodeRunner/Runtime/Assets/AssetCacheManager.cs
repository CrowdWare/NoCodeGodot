using Runtime.Logging;
using Runtime.Manifest;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Runtime.Assets;

public sealed class AssetCacheManager
{
    private static readonly System.Net.Http.HttpClient HttpClient = new();
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _cacheRoot;
    private readonly string _assetsRoot;
    private readonly string _metadataPath;

    public AssetCacheManager()
    {
        var userDataRoot = Godot.ProjectSettings.GlobalizePath("user://");
        _cacheRoot = Path.Combine(userDataRoot, "cache");
        _assetsRoot = Path.Combine(_cacheRoot, "assets");
        _metadataPath = Path.Combine(_cacheRoot, "metadata.json");
    }

    public async Task<AssetSyncResult> SyncAsync(ManifestDocument manifest, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(_assetsRoot);

        var metadata = await LoadMetadataAsync(cancellationToken);
        var previousById = metadata.Items.ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase);
        var updatedItems = new List<AssetCacheItem>(manifest.Assets.Count);

        var downloaded = 0;
        var reused = 0;
        var failed = 0;

        foreach (var asset in manifest.Assets)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var relativePath = NormalizeRelativePath(asset.Path);
            var absolutePath = Path.Combine(_assetsRoot, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(absolutePath)!);

            var shouldDownload = true;
            if (previousById.TryGetValue(asset.Id, out var existing)
                && string.Equals(existing.Hash, asset.Hash, StringComparison.OrdinalIgnoreCase)
                && File.Exists(absolutePath))
            {
                shouldDownload = false;
            }

            if (shouldDownload)
            {
                try
                {
                    await DownloadToFileAtomically(asset.Url, absolutePath, cancellationToken);

                    var calculatedHash = await ComputeSha256Async(absolutePath, cancellationToken);
                    if (!HashEquals(calculatedHash, asset.Hash))
                    {
                        throw new InvalidDataException($"Hash mismatch for asset '{asset.Id}'. Expected '{asset.Hash}' but got '{calculatedHash}'.");
                    }

                    downloaded++;
                    RunnerLogger.Info("Assets", $"Downloaded '{asset.Id}' -> '{relativePath}'.");
                }
                catch (Exception ex)
                {
                    failed++;
                    RunnerLogger.Error("Assets", $"Failed to download '{asset.Id}' from '{asset.Url}': {ex.Message}");
                    continue;
                }
            }
            else
            {
                reused++;
            }

            updatedItems.Add(new AssetCacheItem
            {
                Id = asset.Id,
                Hash = asset.Hash,
                RelativePath = relativePath,
                SourceUrl = asset.Url,
                Size = asset.Size
            });
        }

        metadata.Items = updatedItems;
        metadata.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await SaveMetadataAtomicAsync(metadata, cancellationToken);

        return new AssetSyncResult(downloaded, reused, failed);
    }

    private async Task<AssetCacheMetadata> LoadMetadataAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_metadataPath))
        {
            return new AssetCacheMetadata();
        }

        try
        {
            await using var stream = File.OpenRead(_metadataPath);
            var metadata = await JsonSerializer.DeserializeAsync<AssetCacheMetadata>(stream, JsonOptions, cancellationToken);
            return metadata ?? new AssetCacheMetadata();
        }
        catch (Exception ex)
        {
            RunnerLogger.Warn("Assets", $"Could not read cache metadata, rebuilding cache index. Reason: {ex.Message}");
            return new AssetCacheMetadata();
        }
    }

    private async Task SaveMetadataAtomicAsync(AssetCacheMetadata metadata, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(_cacheRoot);
        var tempPath = _metadataPath + ".tmp";

        await using (var stream = File.Create(tempPath))
        {
            await JsonSerializer.SerializeAsync(stream, metadata, JsonOptions, cancellationToken);
            await stream.FlushAsync(cancellationToken);
        }

        if (File.Exists(_metadataPath))
        {
            File.Delete(_metadataPath);
        }

        File.Move(tempPath, _metadataPath);
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
}
