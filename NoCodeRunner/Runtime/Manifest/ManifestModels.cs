using System;
using System.Collections.Generic;

namespace Runtime.Manifest;

public sealed class ManifestDocument
{
    public string Version { get; init; } = "1";
    public string? BaseUrl { get; init; }
    public string? EntryPoint { get; init; }
    public required string SourceManifestUrl { get; init; }
    public string RawContent { get; init; } = string.Empty;
    public IReadOnlyList<ManifestAssetEntry> Assets { get; init; } = Array.Empty<ManifestAssetEntry>();
}

public sealed class ManifestAssetEntry
{
    public required string Id { get; init; }
    public required string Path { get; init; }
    public required string Hash { get; init; }
    public required string Url { get; init; }
    public string? Type { get; init; }
    public long? Size { get; init; }
}
