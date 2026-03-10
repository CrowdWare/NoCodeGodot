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

#include "smlcore_native.h"

#include <cstdint>
#include <iostream>
#include <stdexcept>
#include <string>
#include <vector>

namespace {

struct ParseResult {
    int rc = 0;
    std::int64_t node_count = 0;
    std::string error;
};

ParseResult parse(const std::string& source) {
    char error[1024] = {0};
    std::int64_t count = 0;
    const auto rc = smlcore_native_parse(source.c_str(), &count, error, static_cast<int>(sizeof(error)));
    return {rc, count, error};
}

std::string parse_ast_json(const std::string& source) {
    char out_json[8192] = {0};
    char error[1024] = {0};
    std::int64_t out_len = 0;
    const auto rc = smlcore_native_parse_ast_json(
        source.c_str(),
        out_json,
        static_cast<int>(sizeof(out_json)),
        &out_len,
        error,
        static_cast<int>(sizeof(error)));
    if (rc != 0) {
        throw std::runtime_error(std::string("parse_ast_json failed: ") + error);
    }
    return std::string(out_json, static_cast<std::size_t>(out_len));
}

void expect_ok_nodes(const std::string& name, const std::string& source, std::int64_t expected) {
    const auto result = parse(source);
    if (result.rc != 0) {
        throw std::runtime_error(name + " failed unexpectedly: " + result.error);
    }
    if (result.node_count != expected) {
        throw std::runtime_error(name + " node_count=" + std::to_string(result.node_count)
            + " expected=" + std::to_string(expected));
    }
}

void expect_error_contains(const std::string& name, const std::string& source, const std::string& needle) {
    const auto result = parse(source);
    if (result.rc == 0) {
        throw std::runtime_error(name + " unexpectedly succeeded.");
    }
    if (result.error.find(needle) == std::string::npos) {
        throw std::runtime_error(name + " unexpected error: " + result.error);
    }
}

void expect_ast_contains(const std::string& name, const std::string& source, const std::string& needle) {
    const auto json = parse_ast_json(source);
    if (json.find(needle) == std::string::npos) {
        throw std::runtime_error(name + " missing AST fragment: " + needle);
    }
}

using TestFn = void(*)();
struct TestCase { const char* name; TestFn fn; };

void test_nested_nodes_parse_structure() {
    expect_ok_nodes(
        "nested_nodes_parse_structure",
        "Window { title: \"Hello\" Row { Label { text: \"A\" } Label { text: \"B\" } } }",
        4);
}

void test_identifier_property_parses() {
    expect_ast_contains(
        "identifier_property_parses",
        "Window { id: main }",
        "\"kind\":\"identifier\"");
    expect_ast_contains(
        "identifier_property_parses",
        "Window { id: main }",
        "\"value\":\"main\"");
}

void test_unknown_node_kept_in_ast() {
    expect_ok_nodes("unknown_node_kept_in_ast", "UnknownWidget { text: \"x\" }", 1);
    expect_ast_contains("unknown_node_kept_in_ast", "UnknownWidget { text: \"x\" }", "\"name\":\"UnknownWidget\"");
}

void test_float_values_parse_successfully() {
    const auto source =
        "Control { anchorLeft: 0.0 anchorRight: 1.0 width: 300 opacity: 0.75 anchorCenter: .5 }";
    expect_ok_nodes("float_values_parse_successfully", source, 1);
    expect_ast_contains("float_values_parse_successfully", source, "\"value\":\"0\"");
    expect_ast_contains("float_values_parse_successfully", source, "\"value\":\"1\"");
    expect_ast_contains("float_values_parse_successfully", source, "\"value\":\"0.75\"");
    expect_ast_contains("float_values_parse_successfully", source, "\"value\":\"0.5\"");
}

void test_invalid_float_format_rejected() {
    expect_error_contains(
        "invalid_float_format_rejected",
        "Control { anchorLeft: 12..3 }",
        "Expected property or child element name");
}

void test_unregistered_identifier_property_is_identifier() {
    expect_ast_contains(
        "unregistered_identifier_property_is_identifier",
        "Window { title: hello }",
        "\"kind\":\"identifier\"");
}

void test_tuple_values_parse_comma_and_pipe() {
    expect_ast_contains(
        "tuple_values_parse_comma_and_pipe",
        "Window { pos: 20,20 anchors: top | bottom | left }",
        "\"kind\":\"tuple\"");
    expect_ast_contains(
        "tuple_values_parse_comma_and_pipe",
        "Window { pos: 20,20 anchors: top | bottom | left }",
        "\"value\":\"20,20\"");
    expect_ast_contains(
        "tuple_values_parse_comma_and_pipe",
        "Window { pos: 20,20 anchors: top | bottom | left }",
        "\"value\":\"top|bottom|left\"");
}

void test_multiline_string_preserves_linebreaks() {
    expect_ast_contains(
        "multiline_string_preserves_linebreaks",
        "Window { text: \"\\nfirst line\\nsecond line\\n\" }",
        "first line\\nsecond line");
}

void test_resource_ref_parses_as_identifier_token() {
    expect_ast_contains(
        "resource_ref_parses_as_identifier_token",
        "Window { text: @Strings.greeting }",
        "\"value\":\"@Strings.greeting\"");
}

void test_comments_do_not_break_parse() {
    expect_ok_nodes(
        "comments_do_not_break_parse",
        "// comment\nWindow { title: \"x\" }\n// next",
        1);
}

const std::vector<TestCase>& all_tests() {
    static const std::vector<TestCase> tests = {
        {"sml_parser_nested_nodes_parse_structure", test_nested_nodes_parse_structure},
        {"sml_parser_identifier_property_parses", test_identifier_property_parses},
        {"sml_parser_unknown_node_kept_in_ast", test_unknown_node_kept_in_ast},
        {"sml_parser_float_values_parse_successfully", test_float_values_parse_successfully},
        {"sml_parser_invalid_float_format_rejected", test_invalid_float_format_rejected},
        {"sml_parser_unregistered_identifier_property_is_identifier", test_unregistered_identifier_property_is_identifier},
        {"sml_parser_tuple_values_parse_comma_and_pipe", test_tuple_values_parse_comma_and_pipe},
        {"sml_parser_multiline_string_preserves_linebreaks", test_multiline_string_preserves_linebreaks},
        {"sml_parser_resource_ref_parses_as_identifier_token", test_resource_ref_parses_as_identifier_token},
        {"sml_parser_comments_do_not_break_parse", test_comments_do_not_break_parse},
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
                    std::cout << "smlcore_native_parser_tests: " << test.name << " passed\n";
                    return 0;
                }
            }
            throw std::runtime_error("unknown test case: " + requested);
        }

        for (const auto& test : tests) {
            test.fn();
        }
        std::cout << "smlcore_native_parser_tests: all tests passed (" << tests.size() << ")\n";
        return 0;
    } catch (const std::exception& ex) {
        std::cerr << "smlcore_native_parser_tests failed: " << ex.what() << "\n";
        return 1;
    }
}
