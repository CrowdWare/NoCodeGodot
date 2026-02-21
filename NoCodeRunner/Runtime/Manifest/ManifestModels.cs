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
