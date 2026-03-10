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

#include <algorithm>
#include <cctype>
#include <filesystem>
#include <string>

namespace forge {

inline std::string trim_quoted_copy(std::string value) {
    const auto not_space = [](unsigned char c) { return std::isspace(c) == 0; };
    const auto begin = std::find_if(value.begin(), value.end(), not_space);
    if (begin == value.end()) return {};
    const auto end = std::find_if(value.rbegin(), value.rend(), not_space).base();
    value = std::string(begin, end);
    if (value.size() >= 2) {
        const char first = value.front();
        const char last = value.back();
        if ((first == '"' && last == '"') || (first == '\'' && last == '\'')) {
            value = value.substr(1, value.size() - 2);
        }
    }
    return value;
}

inline std::string dirname_copy(const std::string& path) {
    return std::filesystem::path(path).parent_path().string();
}

// Single source of truth for Forge runtime path/scheme resolution.
inline std::string resolve_runtime_asset_path(
    const std::string& raw_value,
    const std::string& base_dir,
    const std::string& appres_root)
{
    const std::string raw = trim_quoted_copy(raw_value);
    if (raw.empty()) return raw;

    if (raw.rfind("builtin:greybox/", 0) == 0) return raw;

    if (raw.rfind("res://", 0) == 0) return base_dir + "/" + raw.substr(6);
    if (raw.rfind("res:/", 0) == 0) return base_dir + "/" + raw.substr(5);

    // appRes:// is intentionally invalid; require appRes:/ (single slash).
    if (raw.rfind("appRes://", 0) == 0) return {};
    if (raw.rfind("appRes:/", 0) == 0) {
        const std::string tail = raw.substr(8);
        return appres_root.empty() ? (base_dir + "/" + tail) : (appres_root + "/" + tail);
    }

    if (raw.rfind("file://", 0) == 0) {
        std::string path = raw.substr(7);
        if (path.size() >= 9 && path.substr(0, 9) == "localhost") {
            path = path.substr(9);
        }
        if (!path.empty() && path[0] != '/') path = "/" + path;
        return path;
    }

    if (!raw.empty() && raw[0] == '/') return raw;
    return base_dir + "/" + raw;
}

} // namespace forge
