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

#include "../src/forge_sms_error_policy.h"

#include <iostream>
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

void assert_false(bool condition, const std::string& message) {
    if (condition) {
        throw std::runtime_error(message);
    }
}

void test_missing_handler_does_not_require_exit() {
    const std::string msg = "No SMS event handler found for 'editor.objectMoved'.";
    assert_true(forge::sms_error_is_missing_handler(msg), "missing handler should be detected");
    assert_false(forge::sms_error_requires_exit(msg), "missing handler must not require process exit");
}

void test_runtime_error_requires_exit() {
    const std::string msg = "RuntimeError: interpreter recursion limit exceeded while invoking 'editor.objectSelected' (possible stack overflow).";
    assert_true(forge::sms_error_requires_exit(msg), "runtime errors must require process exit");
}

void test_non_runtime_error_does_not_require_exit() {
    const std::string msg = "Unknown ui method: setBoneTree";
    assert_false(forge::sms_error_requires_exit(msg), "non-runtime errors must not force exit");
}

void test_plain_stack_overflow_requires_exit() {
    const std::string msg = "Stack overflow.";
    assert_true(forge::sms_error_requires_exit(msg), "plain stack overflow must require process exit");
}

const std::vector<TestCase>& all_tests() {
    static const std::vector<TestCase> tests = {
        {"sms_error_policy_missing_handler_does_not_require_exit", test_missing_handler_does_not_require_exit},
        {"sms_error_policy_runtime_error_requires_exit", test_runtime_error_requires_exit},
        {"sms_error_policy_non_runtime_error_does_not_require_exit", test_non_runtime_error_does_not_require_exit},
        {"sms_error_policy_plain_stack_overflow_requires_exit", test_plain_stack_overflow_requires_exit},
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
                    std::cout << "forge_runner_native_sms_error_policy_tests: " << test.name << " passed\n";
                    return 0;
                }
            }
            throw std::runtime_error("unknown test case: " + requested);
        }

        for (const auto& test : tests) {
            test.fn();
        }
        std::cout << "forge_runner_native_sms_error_policy_tests: all tests passed (" << tests.size() << ")\n";
        return 0;
    } catch (const std::exception& ex) {
        std::cerr << "forge_runner_native_sms_error_policy_tests failed: " << ex.what() << "\n";
        return 1;
    }
}
