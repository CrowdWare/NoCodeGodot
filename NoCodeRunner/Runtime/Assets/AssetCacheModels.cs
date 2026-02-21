/*
 * Copyright (C) 2026 CrowdWare
 *
 * This file is part of NoCodeRunner.
 *
 *  NoCodeRunner is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  NoCodeRunner is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with NoCodeRunner.  If not, see <http://www.gnu.org/licenses/>.
 */

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

public readonly record struct AssetSyncPlan(
    int DownloadCount,
    long PlannedBytes,
    int UnknownSizeCount);

public readonly record struct AssetSyncProgress(
    int CompletedCount,
    int TotalCount,
    long DownloadedBytes,
    long PlannedBytes,
    string? CurrentPath);
