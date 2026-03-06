#include <algorithm>
#include <chrono>
#include <cctype>
#include <cstdio>
#include <cstdint>
#include <cstdlib>
#include <ctime>
#include <filesystem>
#include <fstream>
#include <iostream>
#include <optional>
#include <string>
#include <vector>

#if defined(_WIN32)
#include <windows.h>
#else
#include <dlfcn.h>
#endif

namespace fs = std::filesystem;

using NativeSessionCreateFn = int (*)(std::int64_t*, char*, int);
using NativeSessionLoadFn = int (*)(std::int64_t, const char*, char*, int);
using NativeSessionInvokeFn = int (*)(std::int64_t, const char*, const char*, const char*, std::int64_t*, char*, int);
using NativeSessionDisposeFn = int (*)(std::int64_t, char*, int);
using NativeSetUiCallbacksFn = int (*)(
    int (*)(const char*, const char*, char*, int, char*, int),
    int (*)(const char*, const char*, const char*, char*, int),
    int (*)(const char*, const char*, const char*, char*, int, char*, int),
    char*,
    int);

namespace {

std::string current_timestamp_utc() {
    using clock = std::chrono::system_clock;
    const auto now = clock::now();
    const std::time_t tt = clock::to_time_t(now);
    std::tm tm{};
#if defined(_WIN32)
    gmtime_s(&tm, &tt);
#else
    gmtime_r(&tt, &tm);
#endif

    char buffer[32]{};
    std::strftime(buffer, sizeof(buffer), "%Y-%m-%dT%H:%M:%SZ", &tm);
    return std::string(buffer);
}

std::string trim(std::string text) {
    auto is_ws = [](unsigned char c) { return std::isspace(c) != 0; };
    while (!text.empty() && is_ws(static_cast<unsigned char>(text.front()))) {
        text.erase(text.begin());
    }
    while (!text.empty() && is_ws(static_cast<unsigned char>(text.back()))) {
        text.pop_back();
    }
    return text;
}

std::string to_lower(std::string text) {
    std::transform(text.begin(), text.end(), text.begin(), [](unsigned char c) {
        return static_cast<char>(std::tolower(c));
    });
    return text;
}

void print_usage() {
    std::cout << "ForgeRunner.Native\n"
              << "Usage:\n"
              << "  forge-runner-native [--url <value>] [--verbose] [--help]\n";
}

std::optional<std::string> read_text_file(const fs::path& path) {
    std::ifstream input(path, std::ios::binary);
    if (!input) {
        return std::nullopt;
    }
    return std::string((std::istreambuf_iterator<char>(input)), std::istreambuf_iterator<char>());
}

bool write_text_file(const fs::path& path, const std::string& value) {
    std::error_code ec;
    fs::create_directories(path.parent_path(), ec);
    std::ofstream output(path, std::ios::binary | std::ios::trunc);
    if (!output) {
        return false;
    }
    output << value;
    return static_cast<bool>(output);
}

std::string shared_library_extension() {
#if defined(_WIN32)
    return ".dll";
#elif defined(__APPLE__)
    return ".dylib";
#else
    return ".so";
#endif
}

fs::path last_url_state_file() {
    const char* home = std::getenv("HOME");
#if defined(_WIN32)
    if (home == nullptr) {
        home = std::getenv("USERPROFILE");
    }
#endif
    if (home == nullptr || std::string(home).empty()) {
        return fs::current_path() / ".forge_runner_native_last_url";
    }
    return fs::path(home) / ".forge_runner_native_last_url";
}

std::optional<std::string> restore_last_url() {
    const auto path = last_url_state_file();
    const auto content = read_text_file(path);
    if (!content.has_value()) {
        return std::nullopt;
    }
    const auto value = trim(*content);
    if (value.empty()) {
        return std::nullopt;
    }
    return value;
}

void persist_last_url(const std::string& url) {
    if (url.empty()) {
        return;
    }
    (void)write_text_file(last_url_state_file(), url + "\n");
}

std::optional<fs::path> resolve_file_url(const std::string& url) {
    constexpr const char* kPrefix = "file://";
    if (url.rfind(kPrefix, 0) != 0) {
        return std::nullopt;
    }
    std::string raw = url.substr(std::char_traits<char>::length(kPrefix));
    if (raw.empty()) {
        return std::nullopt;
    }
#if defined(_WIN32)
    if (!raw.empty() && raw[0] == '/') {
        raw.erase(raw.begin());
    }
#endif
    return fs::path(raw);
}

std::string first_string_argument(const std::string& args_json) {
    const auto first_quote = args_json.find('"');
    if (first_quote == std::string::npos) {
        return args_json;
    }
    const auto second_quote = args_json.find('"', first_quote + 1);
    if (second_quote == std::string::npos || second_quote <= first_quote + 1) {
        return args_json;
    }
    return args_json.substr(first_quote + 1, second_quote - first_quote - 1);
}

int ui_get_stub(const char*, const char*, char* out_json, int out_json_capacity, char*, int) {
    if (out_json != nullptr && out_json_capacity > 0) {
        out_json[0] = '\0';
    }
    return 1;
}

int ui_set_stub(const char*, const char*, const char*, char* error, int error_capacity) {
    if (error != nullptr && error_capacity > 0) {
        std::snprintf(error, static_cast<std::size_t>(error_capacity), "ui set is unsupported in native executable mode");
    }
    return 1;
}

int ui_invoke_bridge(
    const char* object_id,
    const char* method,
    const char* args_json,
    char* out_json,
    int out_json_capacity,
    char* error,
    int error_capacity) {
    const std::string object = object_id != nullptr ? object_id : "";
    const std::string method_name = method != nullptr ? method : "";
    const std::string args = args_json != nullptr ? args_json : "[]";
    const std::string message = first_string_argument(args);

    if (object == "__log__") {
        const std::string prefix = "[SMS][" + method_name + "] ";
        std::cout << prefix << message << "\n";
        if (out_json != nullptr && out_json_capacity > 0) {
            std::snprintf(out_json, static_cast<std::size_t>(out_json_capacity), "null");
        }
        return 0;
    }

    if (error != nullptr && error_capacity > 0) {
        std::snprintf(error, static_cast<std::size_t>(error_capacity), "unsupported native ui invoke: %s.%s", object.c_str(), method_name.c_str());
    }
    return 1;
}

void* load_library(const fs::path& file) {
#if defined(_WIN32)
    return reinterpret_cast<void*>(LoadLibraryA(file.string().c_str()));
#else
    return dlopen(file.string().c_str(), RTLD_NOW);
#endif
}

void* load_symbol(void* lib, const char* name) {
#if defined(_WIN32)
    return reinterpret_cast<void*>(GetProcAddress(reinterpret_cast<HMODULE>(lib), name));
#else
    return dlsym(lib, name);
#endif
}

std::optional<std::string> find_root_element_name(const std::string& sml_source) {
    std::string token;
    for (char ch : sml_source) {
        if (std::isalnum(static_cast<unsigned char>(ch)) || ch == '_') {
            token.push_back(ch);
            continue;
        }
        if (!token.empty()) {
            return token;
        }
    }
    return std::nullopt;
}

std::optional<std::string> find_root_id(const std::string& sml_source) {
    const auto id_pos = sml_source.find("id:");
    if (id_pos == std::string::npos) {
        return std::nullopt;
    }
    std::size_t pos = id_pos + 3;
    while (pos < sml_source.size() && std::isspace(static_cast<unsigned char>(sml_source[pos])) != 0) {
        pos++;
    }
    std::string id;
    while (pos < sml_source.size()) {
        const char ch = sml_source[pos];
        if (std::isalnum(static_cast<unsigned char>(ch)) || ch == '_' || ch == '-') {
            id.push_back(ch);
            pos++;
            continue;
        }
        break;
    }
    if (id.empty()) {
        return std::nullopt;
    }
    return id;
}

bool invoke_event_or_ignore_missing(
    NativeSessionInvokeFn invoke_fn,
    std::int64_t session,
    const std::string& target,
    const std::string& event_name) {
    char error[1024] = {0};
    std::int64_t out = 0;
    const int rc = invoke_fn(session, target.c_str(), event_name.c_str(), "[]", &out, error, static_cast<int>(sizeof(error)));
    if (rc == 0) {
        return true;
    }
    const std::string err = error;
    if (err.find("No SMS event handler found") != std::string::npos) {
        return false;
    }
    std::cerr << "[ForgeRunner.Native] event dispatch failed for '" << target << "." << event_name << "': " << err << "\n";
    return false;
}

}  // namespace

int main(int argc, char** argv) {
    std::vector<std::string> args;
    args.reserve(argc > 0 ? static_cast<std::size_t>(argc) : 0U);
    for (int i = 0; i < argc; ++i) {
        args.emplace_back(argv[i] != nullptr ? argv[i] : "");
    }

    bool verbose = false;
    std::string url;
    bool url_from_arg = false;

    for (int i = 1; i < argc; ++i) {
        const std::string arg = args[static_cast<std::size_t>(i)];
        if (arg == "--help" || arg == "-h") {
            print_usage();
            return 0;
        }
        if (arg == "--verbose") {
            verbose = true;
            continue;
        }
        if (arg == "--url" && i + 1 < argc) {
            url = args[static_cast<std::size_t>(++i)];
            url_from_arg = true;
            continue;
        }
        if (arg.rfind("--url=", 0) == 0) {
            url = arg.substr(6);
            url_from_arg = true;
            continue;
        }
    }

    std::cout << "[ForgeRunner.Native] bootstrap started at " << current_timestamp_utc() << "\n";
    std::cout << "[ForgeRunner.Native] executable main is active.\n";

    if (url.empty()) {
        const auto restored = restore_last_url();
        if (restored.has_value()) {
            url = *restored;
            std::cout << "[ForgeRunner.Native] restored last url=" << url << "\n";
        }
    }

    if (!url.empty()) {
        std::cout << "[ForgeRunner.Native] url=" << url << "\n";
        if (url_from_arg) {
            persist_last_url(url);
        }
    } else {
        std::cout << "[ForgeRunner.Native] url=<none>\n";
    }
    if (verbose) {
        std::cout << "[ForgeRunner.Native] verbose=true\n";
    }

    if (url.empty()) {
        return 0;
    }

    const auto app_path_opt = resolve_file_url(url);
    if (!app_path_opt.has_value()) {
        std::cerr << "[ForgeRunner.Native] unsupported url scheme (currently only file://): " << url << "\n";
        return 1;
    }

    const fs::path app_path = *app_path_opt;
    const auto app_sml = read_text_file(app_path);
    if (!app_sml.has_value()) {
        std::cerr << "[ForgeRunner.Native] failed to read app.sml: " << app_path.string() << "\n";
        return 1;
    }

    const fs::path app_sms_path = app_path.parent_path() / "app.sms";
    const auto app_sms = read_text_file(app_sms_path);
    if (!app_sms.has_value()) {
        std::cerr << "[ForgeRunner.Native] no app.sms found beside app.sml (" << app_sms_path.string() << "), nothing to execute.\n";
        return 0;
    }

    const fs::path repo_root = fs::current_path();
    const fs::path sms_lib_dir = std::getenv("SMS_NATIVE_LIB_DIR") != nullptr
        ? fs::path(std::getenv("SMS_NATIVE_LIB_DIR"))
        : (repo_root / "SMSCore.Native" / "build");
    const fs::path sms_lib_path = sms_lib_dir / ("libsms_native" + shared_library_extension());

    void* sms_lib = load_library(sms_lib_path);
    if (sms_lib == nullptr) {
        std::cerr << "[ForgeRunner.Native] failed to load sms native library: " << sms_lib_path.string() << "\n";
        return 1;
    }

    auto create_fn = reinterpret_cast<NativeSessionCreateFn>(load_symbol(sms_lib, "sms_native_session_create"));
    auto load_fn = reinterpret_cast<NativeSessionLoadFn>(load_symbol(sms_lib, "sms_native_session_load"));
    auto invoke_fn = reinterpret_cast<NativeSessionInvokeFn>(load_symbol(sms_lib, "sms_native_session_invoke"));
    auto dispose_fn = reinterpret_cast<NativeSessionDisposeFn>(load_symbol(sms_lib, "sms_native_session_dispose"));
    auto set_ui_callbacks_fn = reinterpret_cast<NativeSetUiCallbacksFn>(load_symbol(sms_lib, "sms_native_set_ui_callbacks"));
    if (create_fn == nullptr || load_fn == nullptr || invoke_fn == nullptr || dispose_fn == nullptr || set_ui_callbacks_fn == nullptr) {
        std::cerr << "[ForgeRunner.Native] missing required sms native symbol(s)\n";
        return 1;
    }

    char cb_error[1024] = {0};
    (void)set_ui_callbacks_fn(&ui_get_stub, &ui_set_stub, &ui_invoke_bridge, cb_error, static_cast<int>(sizeof(cb_error)));

    std::int64_t session = 0;
    char error[1024] = {0};
    if (create_fn(&session, error, static_cast<int>(sizeof(error))) != 0) {
        std::cerr << "[ForgeRunner.Native] session create failed: " << error << "\n";
        return 1;
    }

    const std::string sms_source = *app_sms;
    if (load_fn(session, sms_source.c_str(), error, static_cast<int>(sizeof(error))) != 0) {
        std::cerr << "[ForgeRunner.Native] session load failed for '" << app_sms_path.string() << "': " << error << "\n";
        (void)dispose_fn(session, nullptr, 0);
        return 1;
    }

    invoke_event_or_ignore_missing(invoke_fn, session, "__runtime__", "ready");

    const auto root_element = find_root_element_name(*app_sml);
    if (root_element.has_value()) {
        const auto root_target = to_lower(*root_element);
        invoke_event_or_ignore_missing(invoke_fn, session, root_target, "ready");
    }

    const auto root_id = find_root_id(*app_sml);
    if (root_id.has_value()) {
        invoke_event_or_ignore_missing(invoke_fn, session, *root_id, "ready");
    }

    (void)dispose_fn(session, nullptr, 0);
    return 0;
}
