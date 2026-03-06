#include "sandbox_policy.h"

#include <cstdio>

namespace forgecli {

static void write_error(char* error, int error_capacity, const std::string& message) {
    if (error == nullptr || error_capacity <= 0) {
        return;
    }
    std::snprintf(error, static_cast<std::size_t>(error_capacity), "%s", message.c_str());
}

static bool path_has_prefix(const fs::path& value, const fs::path& prefix) {
    const fs::path normalized_value = value.lexically_normal();
    const fs::path normalized_prefix = prefix.lexically_normal();
    auto prefix_it = normalized_prefix.begin();
    auto value_it = normalized_value.begin();
    for (; prefix_it != normalized_prefix.end(); ++prefix_it, ++value_it) {
        if (value_it == normalized_value.end() || *value_it != *prefix_it) {
            return false;
        }
    }
    return true;
}

static bool contains_disallowed_component(const fs::path& relative) {
    for (const auto& part : relative) {
        if (part == "." || part == "..") {
            return true;
        }
    }
    return false;
}

bool initialize_sandbox_roots(const fs::path& project_root, SandboxRoots& roots, std::string& error) {
    std::error_code ec;
    const fs::path canonical_project = fs::weakly_canonical(project_root, ec);
    if (ec) {
        error = "Failed to resolve project root.";
        return false;
    }

    const fs::path user_root = canonical_project / ".forge_user";
    fs::create_directories(user_root, ec);
    if (ec) {
        error = "Failed to prepare user sandbox root.";
        return false;
    }

    roots.res_root = canonical_project;
    roots.appres_root = canonical_project;
    roots.user_root = fs::weakly_canonical(user_root, ec);
    if (ec) {
        error = "Failed to resolve user sandbox root.";
        return false;
    }

    return true;
}

int sandbox_allow_path(
    const SandboxRoots& roots,
    const char* owner,
    const char* uri_path,
    char* error,
    int error_capacity) {
    const std::string owner_name = owner != nullptr ? owner : "sms";
    const std::string uri = uri_path != nullptr ? uri_path : "";

    if (uri.empty()) {
        write_error(error, error_capacity, owner_name + " path rejected: empty URI.");
        return 1;
    }

    fs::path root;
    std::string relative_text;
    if (uri.rfind("res:/", 0) == 0) {
        root = roots.res_root;
        relative_text = uri.substr(5);
    } else if (uri.rfind("appRes:/", 0) == 0) {
        root = roots.appres_root;
        relative_text = uri.substr(8);
    } else if (uri.rfind("user:/", 0) == 0) {
        root = roots.user_root;
        relative_text = uri.substr(6);
    } else {
        write_error(error, error_capacity, owner_name + " path rejected: unsupported URI scheme.");
        return 1;
    }

    fs::path relative = fs::path(relative_text).lexically_normal();
    if (relative.is_absolute() || contains_disallowed_component(relative)) {
        write_error(error, error_capacity, owner_name + " path rejected: traversal not allowed.");
        return 1;
    }

    std::error_code ec;
    const fs::path canonical_root = fs::weakly_canonical(root, ec);
    if (ec) {
        write_error(error, error_capacity, owner_name + " path rejected: root resolution failed.");
        return 1;
    }

    const fs::path candidate = (canonical_root / relative).lexically_normal();
    if (!path_has_prefix(candidate, canonical_root)) {
        write_error(error, error_capacity, owner_name + " path rejected: outside sandbox root.");
        return 1;
    }

    fs::path cursor = canonical_root;
    for (const auto& part : relative) {
        if (part.empty()) {
            continue;
        }
        cursor /= part;
        const auto status = fs::symlink_status(cursor, ec);
        if (!ec && fs::exists(status) && fs::is_symlink(status)) {
            write_error(error, error_capacity, owner_name + " path rejected: symlink component is not allowed.");
            return 1;
        }
        ec.clear();
    }

    fs::path probe = candidate;
    while (!probe.empty()) {
        const auto status = fs::symlink_status(probe, ec);
        if (!ec && fs::exists(status)) {
            const fs::path canonical_probe = fs::weakly_canonical(probe, ec);
            if (ec || !path_has_prefix(canonical_probe, canonical_root)) {
                write_error(error, error_capacity, owner_name + " path rejected: canonical path escapes sandbox root.");
                return 1;
            }
            break;
        }
        ec.clear();
        if (probe == canonical_root) {
            break;
        }
        const fs::path parent = probe.parent_path();
        if (parent == probe) {
            break;
        }
        probe = parent;
    }

    return 0;
}

} // namespace forgecli
