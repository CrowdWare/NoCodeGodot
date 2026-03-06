#include "sandbox_policy.h"

#include <cstdlib>
#include <fstream>
#include <iostream>
#include <stdexcept>
#include <string>
#include <vector>

namespace {
namespace fs = std::filesystem;

struct TempDir {
    fs::path path;

    explicit TempDir(const std::string& prefix) {
        const auto base = fs::temp_directory_path();
        const auto unique = prefix + "_" + std::to_string(static_cast<long long>(std::rand()));
        path = base / unique;
        std::error_code ec;
        fs::create_directories(path, ec);
        if (ec) {
            throw std::runtime_error("failed to create temp dir");
        }
    }

    ~TempDir() {
        std::error_code ec;
        fs::remove_all(path, ec);
    }
};

void expect_allowed(const forgecli::SandboxRoots& roots, const std::string& uri) {
    char error[1024] = {0};
    const int rc = forgecli::sandbox_allow_path(roots, "fs.readText", uri.c_str(), error, static_cast<int>(sizeof(error)));
    if (rc != 0) {
        throw std::runtime_error("expected allow for '" + uri + "' but got: " + std::string(error));
    }
}

void expect_rejected_contains(const forgecli::SandboxRoots& roots, const std::string& uri, const std::string& expected) {
    char error[1024] = {0};
    const int rc = forgecli::sandbox_allow_path(roots, "fs.readText", uri.c_str(), error, static_cast<int>(sizeof(error)));
    if (rc == 0) {
        throw std::runtime_error("expected reject for '" + uri + "'");
    }
    const std::string msg = error;
    if (msg.find(expected) == std::string::npos) {
        throw std::runtime_error("reject message mismatch for '" + uri + "': " + msg);
    }
}

void test_traversal_rejected() {
    TempDir project("forge_sandbox_project");
    forgecli::SandboxRoots roots;
    std::string err;
    if (!forgecli::initialize_sandbox_roots(project.path, roots, err)) {
        throw std::runtime_error(err);
    }
    expect_rejected_contains(roots, "res:/safe/../../escape.txt", "traversal");
}

void test_symlink_component_rejected() {
    TempDir project("forge_sandbox_project");
    TempDir outside("forge_sandbox_outside");

    std::error_code ec;
    fs::create_directories(project.path / "safe", ec);
    if (ec) {
        throw std::runtime_error("failed to create safe dir");
    }

    const fs::path link = project.path / "safe" / "out";
    fs::create_directory_symlink(outside.path, link, ec);
    if (ec) {
#if defined(_WIN32)
        std::cout << "sandbox_policy_tests: symlink test skipped on this Windows environment\n";
        return;
#else
        throw std::runtime_error("failed to create symlink for test");
#endif
    }

    forgecli::SandboxRoots roots;
    std::string err;
    if (!forgecli::initialize_sandbox_roots(project.path, roots, err)) {
        throw std::runtime_error(err);
    }

    expect_rejected_contains(roots, "res:/safe/out/secret.txt", "symlink component");
}

void test_safe_path_allowed() {
    TempDir project("forge_sandbox_project");
    std::error_code ec;
    fs::create_directories(project.path / "safe", ec);
    if (ec) {
        throw std::runtime_error("failed to create safe dir");
    }
    std::ofstream(project.path / "safe" / "ok.txt") << "ok";

    forgecli::SandboxRoots roots;
    std::string err;
    if (!forgecli::initialize_sandbox_roots(project.path, roots, err)) {
        throw std::runtime_error(err);
    }

    expect_allowed(roots, "res:/safe/ok.txt");
}

using TestFn = void(*)();

struct TestCase {
    const char* name;
    TestFn fn;
};

const std::vector<TestCase>& all_tests() {
    static const std::vector<TestCase> tests = {
        {"traversal_rejected", test_traversal_rejected},
        {"symlink_component_rejected", test_symlink_component_rejected},
        {"safe_path_allowed", test_safe_path_allowed},
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
                    std::cout << "forgecli_sandbox_policy_tests: " << test.name << " passed\n";
                    return 0;
                }
            }
            throw std::runtime_error("unknown test case: " + requested);
        }

        for (const auto& test : tests) {
            test.fn();
        }
        std::cout << "forgecli_sandbox_policy_tests: all tests passed (" << tests.size() << ")\n";
        return 0;
    } catch (const std::exception& ex) {
        std::cerr << "forgecli_sandbox_policy_tests failed: " << ex.what() << "\n";
        return 1;
    }
}
