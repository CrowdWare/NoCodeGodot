using System;
using System.Collections.Generic;

namespace Runtime.Assets;

public sealed class AssetCacheMetadata
{
    public int Version { get; set; } = 1;
    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public string? ManifestVersion { get; set; }
    public string? ManifestContentHash { get; set; }
    public string? EntryPoint { get; set; }
    public List<AssetCacheItem> Items { get; set; } = [];
}

public sealed class AssetCacheItem
{
    public required string Id { get; set; }
    public required string Hash { get; set; }
    public required string RelativePath { get; set; }
    public required string SourceUrl { get; set; }
    public long? Size { get; set; }
}

public readonly record struct AssetSyncResult(
    int DownloadedCount,
    int ReusedCount,
    int FailedCount,
    long DownloadedBytes,
    bool CacheHit,
    string ManifestStatus,
    string? EntryFileUrl);
