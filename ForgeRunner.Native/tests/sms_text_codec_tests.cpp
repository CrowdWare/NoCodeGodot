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

#include "../src/forge_json_string.h"

#include <chrono>
#include <filesystem>
#include <fstream>
#include <iostream>
#include <optional>
#include <stdexcept>
#include <string>
#include <vector>

namespace {

using TestFn = void(*)();
struct TestCase { const char* name; TestFn fn; };

void assert_true(bool condition, const std::string& message) {
    if (!condition) {
        throw std::runtime_error(message);
    }
}

void assert_equal(const std::string& actual, const std::string& expected, const std::string& message) {
    if (actual != expected) {
        throw std::runtime_error(message + "\nexpected: " + expected + "\nactual:   " + actual);
    }
}

std::string read_text_file(const std::filesystem::path& path) {
    std::ifstream in(path, std::ios::binary);
    if (!in.is_open()) {
        throw std::runtime_error("failed to open file: " + path.string());
    }
    std::string out;
    in.seekg(0, std::ios::end);
    out.resize(static_cast<std::size_t>(in.tellg()));
    in.seekg(0, std::ios::beg);
    in.read(out.data(), static_cast<std::streamsize>(out.size()));
    return out;
}

std::filesystem::path locate_fixture_dir() {
    namespace fs = std::filesystem;
    fs::path cur = fs::current_path();
    for (int i = 0; i < 8; ++i) {
        const fs::path candidate = cur / "docs" / "ForgeDesigner" / "fixtures";
        if (fs::exists(candidate / "string_payload.qml")
            && fs::exists(candidate / "string_payload.html")) {
            return candidate;
        }
        if (!cur.has_parent_path()) break;
        cur = cur.parent_path();
    }
    throw std::runtime_error(
        "fixtures not found: expected docs/ForgeDesigner/fixtures/{string_payload.qml,string_payload.html}");
}

std::optional<std::string> extract_text_payload_at_marker(const std::string& qml, const std::string& marker) {
    const std::size_t marker_pos = qml.find(marker);
    if (marker_pos == std::string::npos) return std::nullopt;
    const std::size_t text_prefix_len = std::string("text: \"").size();
    const std::size_t start = marker_pos + text_prefix_len;
    for (std::size_t i = start; i < qml.size(); ++i) {
        if (qml[i] == '"' && (i == start || qml[i - 1] != '\\')) {
            return qml.substr(start, i - start);
        }
    }
    return std::nullopt;
}

std::string json_quote(const std::string& text) {
    std::string out;
    out.reserve(text.size() + 2);
    out.push_back('"');
    for (const char c : text) {
        switch (c) {
            case '"': out += "\\\""; break;
            case '\\': out += "\\\\"; break;
            case '\n': out += "\\n"; break;
            case '\r': out += "\\r"; break;
            case '\t': out += "\\t"; break;
            default: out.push_back(c); break;
        }
    }
    out.push_back('"');
    return out;
}

void test_decodes_multiline_html() {
    const std::string expected =
        "<header id=\"page-title\">\n"
        "\t<div class=\"container\">\n"
        "\t\t<h1>Sacred Sexuality</h1>\n"
        "\t\t<ul class=\"breadcrumb\">\n"
        "\t\t\t<li><a href=\"index.html\">Home</a></li>\n"
        "\t\t\t<li class=\"active\">Sacred Sexuality</li>\n"
        "\t\t</ul>\n"
        "\t</div>\n"
        "</header>\n";

    const std::string input = json_quote(expected);
    const std::string actual = forge::decode_json_string_or_fallback(input);
    assert_equal(actual, expected, "multiline HTML payload must decode exactly");
}

void test_real_fixture_qml_payload_roundtrip() {
    const std::filesystem::path fixture_dir = locate_fixture_dir();
    const std::string qml = read_text_file(fixture_dir / "string_payload.qml");

    const auto payload_opt =
        extract_text_payload_at_marker(qml, "text: \"&lt;header id=&quot;page-title&quot;&gt;");
    assert_true(payload_opt.has_value(), "failed to extract header text payload from fixture qml");
    const std::string payload = *payload_opt;

    const std::string transport = json_quote(payload);
    const std::string decoded = forge::decode_json_string_or_fallback(transport);
    assert_equal(decoded, payload, "fixture payload must survive JSON transport roundtrip");
    assert_true(decoded.find("&lt;header id=&quot;page-title&quot;&gt;") == 0,
        "decoded payload should contain escaped header markup");
}

void test_real_fixture_generated_html_contains_expected_markup() {
    const std::filesystem::path fixture_dir = locate_fixture_dir();
    const std::string html = read_text_file(fixture_dir / "string_payload.html");

    assert_true(html.find("<!DOCTYPE html>") != std::string::npos, "generated html fixture should be a full html document");
    assert_true(html.find("<header id=\"topNav\">") != std::string::npos, "generated html should contain expected topNav header");
    assert_true(html.find("Sacred Sexuality") != std::string::npos, "generated html should contain Sacred Sexuality content");
}

void test_decodes_unicode_escape() {
    const std::string input = "\"\\u00DCber ForgeRunner\"";
    const std::string actual = forge::decode_json_string_or_fallback(input);
    assert_equal(actual, "Über ForgeRunner", "unicode escape should decode to UTF-8");
}

void test_invalid_escape_falls_back() {
    const std::string input = "\"Broken \\u00G0 escape\"";
    const std::string actual = forge::decode_json_string_or_fallback(input);
    assert_equal(actual, input, "invalid JSON string must fall back to original input");
}

void test_non_string_falls_back() {
    assert_equal(forge::decode_json_string_or_fallback("42"), "42", "number input should pass through");
    assert_equal(forge::decode_json_string_or_fallback("null"), "null", "null input should pass through");
}

void test_perf_large_payload() {
    std::string raw;
    raw.reserve(200000);
    for (int i = 0; i < 1800; ++i) {
        raw += "Line ";
        raw += std::to_string(i);
        raw += ": \"SaveFrame\" &lt;tag&gt; Über runner\n";
    }

    const std::string input = json_quote(raw);
    constexpr int iterations = 1000;

    std::size_t checksum = 0;
    const auto started = std::chrono::steady_clock::now();
    for (int i = 0; i < iterations; ++i) {
        const std::string decoded = forge::decode_json_string_or_fallback(input);
        checksum += decoded.size();
        if (i == iterations - 1) {
            assert_equal(decoded, raw, "decoded payload must match original payload");
        }
    }
    const auto ended = std::chrono::steady_clock::now();
    const auto elapsed_ms =
        std::chrono::duration_cast<std::chrono::milliseconds>(ended - started).count();

    assert_true(checksum > 0, "checksum must be non-zero");
    std::cout << "perf_decode_large_payload: iterations=" << iterations
              << " chars=" << raw.size()
              << " elapsed_ms=" << elapsed_ms << "\n";
}

const std::vector<TestCase>& all_tests() {
    static const std::vector<TestCase> tests = {
        {"sms_text_codec_decodes_multiline_html", test_decodes_multiline_html},
        {"sms_text_codec_real_fixture_qml_payload_roundtrip", test_real_fixture_qml_payload_roundtrip},
        {"sms_text_codec_real_fixture_generated_html_contains_expected_markup", test_real_fixture_generated_html_contains_expected_markup},
        {"sms_text_codec_decodes_unicode_escape", test_decodes_unicode_escape},
        {"sms_text_codec_invalid_escape_falls_back", test_invalid_escape_falls_back},
        {"sms_text_codec_non_string_falls_back", test_non_string_falls_back},
        {"sms_text_codec_perf_large_payload", test_perf_large_payload},
    };
    return tests;
}

} // namespace

int main(int argc, char** argv) {
    try {
        const auto& tests = all_tests();
        if (argc == 2) {
            const std::string requested = argv[1];
            for (const auto& test : tests) {
                if (requested == test.name) {
                    test.fn();
                    std::cout << "forge_runner_native_sms_text_codec_tests: "
                              << test.name << " passed\n";
                    return 0;
                }
            }
            throw std::runtime_error("unknown test case: " + requested);
        }

        for (const auto& test : tests) {
            test.fn();
        }
        std::cout << "forge_runner_native_sms_text_codec_tests: all tests passed ("
                  << tests.size() << ")\n";
        return 0;
    } catch (const std::exception& ex) {
        std::cerr << "forge_runner_native_sms_text_codec_tests failed: " << ex.what() << "\n";
        return 1;
    }
}
