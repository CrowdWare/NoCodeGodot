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

void expect_parse_ok_nodes(const std::string& name, const std::string& source, std::int64_t expected_nodes) {
    const auto result = parse(source);
    if (result.rc != 0) {
        throw std::runtime_error(name + " failed unexpectedly: " + result.error);
    }
    if (result.node_count != expected_nodes) {
        throw std::runtime_error(name + " node_count=" + std::to_string(result.node_count)
            + " expected=" + std::to_string(expected_nodes));
    }
}

void expect_parse_error_contains(const std::string& name, const std::string& source, const std::string& expected_substring) {
    const auto result = parse(source);
    if (result.rc == 0) {
        throw std::runtime_error(name + " unexpectedly succeeded.");
    }
    if (result.error.find(expected_substring) == std::string::npos) {
        throw std::runtime_error(name + " returned unexpected error: " + result.error);
    }
}

void expect_ast_json_ok(const std::string& name, const std::string& source) {
    char out_json[2048] = {0};
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
        throw std::runtime_error(name + " failed unexpectedly: " + error);
    }
    if (out_len <= 0) {
        throw std::runtime_error(name + " produced empty AST json.");
    }
    if (std::string(out_json).find("\"roots\"") == std::string::npos) {
        throw std::runtime_error(name + " AST json missing roots.");
    }
}

using TestFn = void(*)();

struct TestCase {
    const char* name;
    TestFn fn;
};

void test_single_node() {
    expect_parse_ok_nodes("single_node", "Window { title: \"Hello\" }", 1);
}

void test_nested_nodes() {
    expect_parse_ok_nodes("nested_nodes", "Window { VBox { Label { text: \"A\" } } }", 3);
}

void test_ast_json_roundtrip() {
    expect_ast_json_ok("ast_json_roundtrip", "Window { title: \"X\" }");
}

void test_unterminated_string() {
    expect_parse_error_contains("unterminated_string", "Window { title: \"Hello }", "Unterminated string");
}

const std::vector<TestCase>& all_tests() {
    static const std::vector<TestCase> tests = {
        {"single_node", test_single_node},
        {"nested_nodes", test_nested_nodes},
        {"ast_json_roundtrip", test_ast_json_roundtrip},
        {"unterminated_string", test_unterminated_string},
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
                    std::cout << "smlcore_native_spec_tests: " << test.name << " passed\n";
                    return 0;
                }
            }
            throw std::runtime_error("unknown test case: " + requested);
        }

        for (const auto& test : tests) {
            test.fn();
        }
        std::cout << "smlcore_native_spec_tests: all tests passed (" << tests.size() << ")\n";
        return 0;
    } catch (const std::exception& ex) {
        std::cerr << "smlcore_native_spec_tests failed: " << ex.what() << "\n";
        return 1;
    }
}
