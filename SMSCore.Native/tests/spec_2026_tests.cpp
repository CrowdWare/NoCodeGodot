#include "sms_native.h"

#include <cstdint>
#include <cstring>
#include <iostream>
#include <stdexcept>
#include <string>
#include <vector>

namespace {

struct ExecResult {
    int rc = 0;
    std::int64_t value = 0;
    std::string error;
};

ExecResult execute(const std::string& source) {
    char error[1024] = {0};
    std::int64_t value = 0;
    const auto rc = sms_native_execute(source.c_str(), &value, error, static_cast<int>(sizeof(error)));
    return {rc, value, error};
}

void expect_ok_value(const std::string& name, const std::string& source, std::int64_t expected) {
    const auto result = execute(source);
    if (result.rc != 0) {
        throw std::runtime_error(name + " failed unexpectedly: " + result.error);
    }
    if (result.value != expected) {
        throw std::runtime_error(name + " returned " + std::to_string(result.value)
            + " (expected " + std::to_string(expected) + ").");
    }
}

void expect_error_contains(const std::string& name, const std::string& source, const std::string& expected_substring) {
    const auto result = execute(source);
    if (result.rc == 0) {
        throw std::runtime_error(name + " unexpectedly succeeded.");
    }
    if (result.error.find(expected_substring) == std::string::npos) {
        throw std::runtime_error(name + " returned unexpected error: " + result.error);
    }
}

using TestFn = void(*)();

struct TestCase {
    const char* name;
    TestFn fn;
};

void test_import_res_scheme() {
    expect_ok_value("import_res_scheme", "import \"res:/libs/math.sms\"; 1;", 1);
}

void test_import_appres_scheme() {
    expect_ok_value("import_appres_scheme", "import \"appRes:/modules/ui.sms\" as ui; 2;", 2);
}

void test_import_user_scheme() {
    expect_ok_value("import_user_scheme", "import \"user:/local/tools.sms\"; 3;", 3);
}

void test_import_forbid_double_slash() {
    expect_error_contains("import_forbid_double_slash", "import \"res://libs/math.sms\"; 1;", "must not contain '//'");
}

void test_import_forbid_relative() {
    expect_error_contains("import_forbid_relative", "import \"./math.sms\"; 1;", "Relative import paths are not allowed");
}

void test_export_function_named_args_and_defaults() {
    expect_ok_value(
        "export_function_named_args_and_defaults",
        "export fun add(a: Int32, b: Int32 = 5): Int32 { return a + b; } add(a = 7);",
        12);
}

void test_data_class_named_args_and_defaults() {
    expect_ok_value(
        "data_class_named_args_and_defaults",
        "data class Vec3(x: Int32 = 1, y: Int32 = 2, z: Int32 = 3);"
        "var v = Vec3(y = 10);"
        "v.x + v.y + v.z;",
        14);
}

void test_for_in_typed_variable() {
    expect_ok_value(
        "for_in_typed_variable",
        "var list = [1, 2, 3, 4];"
        "var sum = 0;"
        "for (item: Int32 in list) { sum = sum + item; }"
        "sum;",
        10);
}

void test_named_then_positional_rejected() {
    expect_error_contains(
        "named_then_positional_rejected",
        "fun add(a, b) { return a + b; } add(a = 1, 2);",
        "Positional argument is not allowed after named argument");
}

void test_unknown_named_function_argument() {
    expect_error_contains(
        "unknown_named_function_argument",
        "fun add(a, b) { return a + b; } add(a = 1, c = 2);",
        "Unknown named argument 'c'");
}

void test_duplicate_named_function_argument() {
    expect_error_contains(
        "duplicate_named_function_argument",
        "fun add(a, b) { return a + b; } add(a = 1, a = 2, b = 3);",
        "Duplicate named argument 'a'");
}

void test_missing_required_function_argument() {
    expect_error_contains(
        "missing_required_function_argument",
        "fun add(a, b) { return a + b; } add(a = 1);",
        "Missing required argument 'b'");
}

void test_unknown_named_dataclass_argument() {
    expect_error_contains(
        "unknown_named_dataclass_argument",
        "data class Vec3(x, y, z); Vec3(y = 2, w = 9);",
        "Unknown named argument 'w'");
}

void test_duplicate_named_dataclass_argument() {
    expect_error_contains(
        "duplicate_named_dataclass_argument",
        "data class Vec3(x, y, z); Vec3(x = 1, x = 2, y = 3, z = 4);",
        "Duplicate named argument 'x'");
}

void test_missing_required_dataclass_argument() {
    expect_error_contains(
        "missing_required_dataclass_argument",
        "data class Vec3(x, y, z); Vec3(y = 2, z = 3);",
        "Missing required argument 'x'");
}

void test_invalid_import_scheme() {
    expect_error_contains(
        "invalid_import_scheme",
        "import \"file:/tmp/mod.sms\"; 1;",
        "Import path must start with");
}

const std::vector<TestCase>& all_tests() {
    static const std::vector<TestCase> tests = {
        {"import_res_scheme", test_import_res_scheme},
        {"import_appres_scheme", test_import_appres_scheme},
        {"import_user_scheme", test_import_user_scheme},
        {"import_forbid_double_slash", test_import_forbid_double_slash},
        {"import_forbid_relative", test_import_forbid_relative},
        {"export_function_named_args_and_defaults", test_export_function_named_args_and_defaults},
        {"data_class_named_args_and_defaults", test_data_class_named_args_and_defaults},
        {"for_in_typed_variable", test_for_in_typed_variable},
        {"named_then_positional_rejected", test_named_then_positional_rejected},
        {"unknown_named_function_argument", test_unknown_named_function_argument},
        {"duplicate_named_function_argument", test_duplicate_named_function_argument},
        {"missing_required_function_argument", test_missing_required_function_argument},
        {"unknown_named_dataclass_argument", test_unknown_named_dataclass_argument},
        {"duplicate_named_dataclass_argument", test_duplicate_named_dataclass_argument},
        {"missing_required_dataclass_argument", test_missing_required_dataclass_argument},
        {"invalid_import_scheme", test_invalid_import_scheme},
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
                    std::cout << "sms_native_spec_tests: " << test.name << " passed\n";
                    return 0;
                }
            }
            throw std::runtime_error("unknown test case: " + requested);
        }

        for (const auto& test : tests) {
            test.fn();
        }
        std::cout << "sms_native_spec_tests: all tests passed (" << tests.size() << ")\n";
        return 0;
    } catch (const std::exception& ex) {
        std::cerr << "sms_native_spec_tests failed: " << ex.what() << "\n";
        return 1;
    }
}
