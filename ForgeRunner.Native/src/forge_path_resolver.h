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
