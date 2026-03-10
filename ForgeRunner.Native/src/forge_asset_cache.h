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

#pragma once

#include <cstdint>
#include <string>
#include <unordered_map>
#include <vector>

namespace forge {

/// Returns the OS-appropriate cache root directory for ForgeRunner.
/// macOS/Linux: $XDG_CACHE_HOME/forge-runner  or  $HOME/.cache/forge-runner
/// Windows:     %LOCALAPPDATA%\ForgeRunner\cache
std::string forge_cache_dir();

/// Persistent disk cache for remote assets.
/// Each URL maps to a local file; SHA-256 hashes are stored in an index so
/// the cache entry can be invalidated when the remote content changes.
class AssetCache {
public:
    AssetCache();                                  ///< Uses forge_cache_dir()
    explicit AssetCache(const std::string& root);

    /// Returns the absolute local path if the URL is cached and the stored
    /// SHA-256 matches expected_sha256 (pass "" to skip hash check).
    /// Returns "" if not cached, file is missing, or hash mismatch.
    std::string resolve(const std::string& url,
                        const std::string& expected_sha256 = "") const;

    /// Atomically write data to the cache, update the index, and return the
    /// local path. Returns "" on I/O failure.
    std::string store(const std::string& url, const std::vector<uint8_t>& data);

    /// SHA-256 of a byte buffer via Godot HashingContext → lowercase hex string.
    static std::string sha256_of(const std::vector<uint8_t>& data);

    const std::string& root() const { return root_; }

private:
    struct Entry {
        std::string sha256;
        std::string local_path;
        std::string timestamp;
    };

    std::string root_;
    mutable std::unordered_map<std::string, Entry> index_;

    std::string index_path() const;
    void load_index();
    void save_index() const;
    std::string url_to_local_path(const std::string& url) const;
    static bool atomic_write(const std::string& path, const std::vector<uint8_t>& data);
};

} // namespace forge
