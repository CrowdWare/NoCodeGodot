#pragma once

#include <filesystem>
#include <string>

namespace forgecli {
namespace fs = std::filesystem;

struct SandboxRoots {
    fs::path res_root;
    fs::path appres_root;
    fs::path user_root;
};

bool initialize_sandbox_roots(const fs::path& project_root, SandboxRoots& roots, std::string& error);

int sandbox_allow_path(
    const SandboxRoots& roots,
    const char* owner,
    const char* uri_path,
    char* error,
    int error_capacity);

} // namespace forgecli
