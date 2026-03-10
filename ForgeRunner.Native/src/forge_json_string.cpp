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

#include "forge_json_string.h"

#include <cstdint>

namespace forge {
namespace {

int hex_to_int(char c) {
    if (c >= '0' && c <= '9') return c - '0';
    if (c >= 'a' && c <= 'f') return 10 + (c - 'a');
    if (c >= 'A' && c <= 'F') return 10 + (c - 'A');
    return -1;
}

void append_utf8(std::string& out, std::uint32_t cp) {
    if (cp <= 0x7F) {
        out.push_back(static_cast<char>(cp));
        return;
    }
    if (cp <= 0x7FF) {
        out.push_back(static_cast<char>(0xC0 | ((cp >> 6) & 0x1F)));
        out.push_back(static_cast<char>(0x80 | (cp & 0x3F)));
        return;
    }
    if (cp <= 0xFFFF) {
        out.push_back(static_cast<char>(0xE0 | ((cp >> 12) & 0x0F)));
        out.push_back(static_cast<char>(0x80 | ((cp >> 6) & 0x3F)));
        out.push_back(static_cast<char>(0x80 | (cp & 0x3F)));
        return;
    }
    if (cp <= 0x10FFFF) {
        out.push_back(static_cast<char>(0xF0 | ((cp >> 18) & 0x07)));
        out.push_back(static_cast<char>(0x80 | ((cp >> 12) & 0x3F)));
        out.push_back(static_cast<char>(0x80 | ((cp >> 6) & 0x3F)));
        out.push_back(static_cast<char>(0x80 | (cp & 0x3F)));
        return;
    }
    out.push_back('?');
}

bool parse_hex4(const std::string& input, std::size_t start, std::uint32_t& out) {
    if (start + 4 > input.size()) return false;
    std::uint32_t value = 0;
    for (std::size_t i = 0; i < 4; ++i) {
        const int digit = hex_to_int(input[start + i]);
        if (digit < 0) return false;
        value = static_cast<std::uint32_t>((value << 4) | static_cast<std::uint32_t>(digit));
    }
    out = value;
    return true;
}

bool is_high_surrogate(std::uint32_t cp) {
    return cp >= 0xD800 && cp <= 0xDBFF;
}

bool is_low_surrogate(std::uint32_t cp) {
    return cp >= 0xDC00 && cp <= 0xDFFF;
}

} // namespace

bool decode_json_string_literal(const std::string& input, std::string& output) {
    if (input.size() < 2 || input.front() != '"' || input.back() != '"') {
        return false;
    }

    std::string decoded;
    decoded.reserve(input.size() - 2);

    for (std::size_t i = 1; i + 1 < input.size(); ++i) {
        const char c = input[i];
        if (c != '\\') {
            decoded.push_back(c);
            continue;
        }

        if (i + 2 >= input.size()) return false;
        const char esc = input[++i];
        switch (esc) {
            case '"':  decoded.push_back('"'); break;
            case '\\': decoded.push_back('\\'); break;
            case '/':  decoded.push_back('/'); break;
            case 'b':  decoded.push_back('\b'); break;
            case 'f':  decoded.push_back('\f'); break;
            case 'n':  decoded.push_back('\n'); break;
            case 'r':  decoded.push_back('\r'); break;
            case 't':  decoded.push_back('\t'); break;
            case 'u': {
                std::uint32_t cp = 0;
                if (!parse_hex4(input, i + 1, cp)) return false;
                i += 4;

                if (is_high_surrogate(cp)) {
                    if (i + 6 >= input.size() || input[i + 1] != '\\' || input[i + 2] != 'u') {
                        return false;
                    }
                    std::uint32_t low = 0;
                    if (!parse_hex4(input, i + 3, low) || !is_low_surrogate(low)) {
                        return false;
                    }
                    i += 6;
                    cp = 0x10000 + (((cp - 0xD800) << 10) | (low - 0xDC00));
                } else if (is_low_surrogate(cp)) {
                    return false;
                }

                append_utf8(decoded, cp);
                break;
            }
            default:
                return false;
        }
    }

    output = std::move(decoded);
    return true;
}

std::string decode_json_string_or_fallback(const std::string& input) {
    std::string output;
    if (decode_json_string_literal(input, output)) {
        return output;
    }
    return input;
}

} // namespace forge

