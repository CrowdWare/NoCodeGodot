#include "forge_sms_bridge.h"

#include <algorithm>
#include <cstdio>
#include <cstdlib>
#include <cstring>
#include <filesystem>
#include <fstream>
#include <limits>
#include <sstream>

#if defined(_WIN32)
#include <windows.h>
#else
#include <dlfcn.h>
#endif

#include <godot_cpp/classes/button.hpp>
#include <godot_cpp/classes/check_box.hpp>
#include <godot_cpp/classes/check_button.hpp>
#include <godot_cpp/classes/control.hpp>
#include <godot_cpp/classes/item_list.hpp>
#include <godot_cpp/classes/label.hpp>
#include <godot_cpp/classes/line_edit.hpp>
#include <godot_cpp/classes/option_button.hpp>
#include <godot_cpp/classes/progress_bar.hpp>
#include <godot_cpp/classes/rich_text_label.hpp>
#include <godot_cpp/classes/scroll_container.hpp>
#include <godot_cpp/classes/slider.hpp>
#include <godot_cpp/classes/spin_box.hpp>
#include <godot_cpp/classes/text_edit.hpp>
#include <godot_cpp/variant/utility_functions.hpp>

namespace fs = std::filesystem;
using namespace godot;

namespace forge {

// ---------------------------------------------------------------------------
// Global id map
// ---------------------------------------------------------------------------

IdMap& SmsBridge::id_map() {
    static IdMap s_map;
    return s_map;
}

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

static std::string json_string(const std::string& s) {
    std::string out = "\"";
    for (unsigned char c : s) {
        if      (c == '"')  out += "\\\"";
        else if (c == '\\') out += "\\\\";
        else if (c == '\n') out += "\\n";
        else if (c == '\r') out += "\\r";
        else if (c == '\t') out += "\\t";
        else                out += static_cast<char>(c);
    }
    out += "\"";
    return out;
}

static void write_out(char* buf, int cap, const std::string& s) {
    if (buf == nullptr || cap <= 0) return;
    std::snprintf(buf, static_cast<std::size_t>(cap), "%s", s.c_str());
}

// Strip surrounding double-quotes from a JSON string value.
static std::string json_unquote(const std::string& v) {
    if (v.size() >= 2 && v.front() == '"' && v.back() == '"')
        return v.substr(1, v.size() - 2);
    return v;
}

// Extract first string argument from a JSON array: ["arg1", ...]
static std::string first_string_arg(const std::string& args_json) {
    const auto start = args_json.find('"');
    if (start == std::string::npos) return {};
    std::size_t pos = start + 1;
    while (pos < args_json.size()) {
        if (args_json[pos] == '"' && args_json[pos - 1] != '\\')
            return args_json.substr(start + 1, pos - start - 1);
        ++pos;
    }
    return {};
}

// ---------------------------------------------------------------------------
// Platform library helpers
// ---------------------------------------------------------------------------

static void* platform_load_lib(const fs::path& p) {
#if defined(_WIN32)
    return LoadLibraryW(p.wstring().c_str());
#else
    return dlopen(p.c_str(), RTLD_NOW | RTLD_LOCAL);
#endif
}

static void* platform_load_sym(void* handle, const char* name) {
    if (!handle) return nullptr;
#if defined(_WIN32)
    return reinterpret_cast<void*>(GetProcAddress(static_cast<HMODULE>(handle), name));
#else
    return dlsym(handle, name);
#endif
}

static void platform_free_lib(void* handle) {
    if (!handle) return;
#if defined(_WIN32)
    FreeLibrary(static_cast<HMODULE>(handle));
#else
    dlclose(handle);
#endif
}

static std::string lib_extension() {
#if defined(_WIN32)
    return ".dll";
#elif defined(__APPLE__)
    return ".dylib";
#else
    return ".so";
#endif
}

// ---------------------------------------------------------------------------
// SMS UI callbacks (C-linkage wrappers called by the SMS interpreter)
// ---------------------------------------------------------------------------

static int sms_ui_get(
    const char* object_id, const char* property,
    char* out_json, int out_cap, char*, int)
{
    const std::string id   = object_id ? object_id : "";
    const std::string prop = property  ? property  : "";

    if (prop == "__exists") {
        write_out(out_json, out_cap, "1");
        return 0;
    }

    auto it = SmsBridge::id_map().find(id);
    if (it == SmsBridge::id_map().end() || it->second == nullptr) {
        write_out(out_json, out_cap, "null");
        return 0;
    }
    Control* ctrl = it->second;
    std::string result = "null";

    if (prop == "visible") {
        result = ctrl->is_visible() ? "true" : "false";
    } else if (prop == "text") {
        String t;
        if      (auto* l   = Object::cast_to<Label>(ctrl))         t = l->get_text();
        else if (auto* b   = Object::cast_to<Button>(ctrl))        t = b->get_text();
        else if (auto* le  = Object::cast_to<LineEdit>(ctrl))      t = le->get_text();
        else if (auto* te  = Object::cast_to<TextEdit>(ctrl))      t = te->get_text();
        else if (auto* rtl = Object::cast_to<RichTextLabel>(ctrl)) t = rtl->get_text();
        result = json_string(t.utf8().get_data());
    } else if (prop == "value") {
        double v = 0.0;
        if      (auto* sb = Object::cast_to<SpinBox>(ctrl))       v = sb->get_value();
        else if (auto* sl = Object::cast_to<Slider>(ctrl))        v = sl->get_value();
        else if (auto* pb = Object::cast_to<ProgressBar>(ctrl))   v = pb->get_value();
        char buf[64]; std::snprintf(buf, sizeof(buf), "%g", v);
        result = buf;
    } else if (prop == "checked" || prop == "buttonPressed") {
        bool pressed = false;
        if (auto* btn = Object::cast_to<Button>(ctrl)) pressed = btn->is_pressed();
        result = pressed ? "true" : "false";
    } else if (prop == "disabled") {
        bool dis = false;
        if (auto* btn = Object::cast_to<Button>(ctrl)) dis = btn->is_disabled();
        result = dis ? "true" : "false";
    } else if (prop == "selectedIndex") {
        int idx = -1;
        if (auto* ob = Object::cast_to<OptionButton>(ctrl)) idx = ob->get_selected();
        char buf[32]; std::snprintf(buf, sizeof(buf), "%d", idx);
        result = buf;
    } else if (prop == "selectedText") {
        std::string t;
        if (auto* ob = Object::cast_to<OptionButton>(ctrl)) {
            const int sel = ob->get_selected();
            if (sel >= 0) t = ob->get_item_text(sel).utf8().get_data();
        }
        result = json_string(t);
    } else if (prop == "caretColumn") {
        int col = 0;
        if (auto* le = Object::cast_to<LineEdit>(ctrl)) col = le->get_caret_column();
        char buf[32]; std::snprintf(buf, sizeof(buf), "%d", col);
        result = buf;
    } else if (prop == "scrollV") {
        int v = 0;
        if (auto* sc = Object::cast_to<ScrollContainer>(ctrl)) v = sc->get_v_scroll();
        char buf[32]; std::snprintf(buf, sizeof(buf), "%d", v);
        result = buf;
    }

    write_out(out_json, out_cap, result);
    return 0;
}

static int sms_ui_set(
    const char* object_id, const char* property, const char* value_json,
    char*, int)
{
    const std::string id    = object_id  ? object_id  : "";
    const std::string prop  = property   ? property   : "";
    const std::string value = value_json ? value_json : "null";

    auto it = SmsBridge::id_map().find(id);
    if (it == SmsBridge::id_map().end() || it->second == nullptr) return 0;
    Control* ctrl = it->second;

    if (prop == "visible") {
        ctrl->set_visible(value == "true" || value == "1");
    } else if (prop == "text") {
        const String t(json_unquote(value).c_str());
        if      (auto* l   = Object::cast_to<Label>(ctrl))         l->set_text(t);
        else if (auto* b   = Object::cast_to<Button>(ctrl))        b->set_text(t);
        else if (auto* le  = Object::cast_to<LineEdit>(ctrl))      le->set_text(t);
        else if (auto* te  = Object::cast_to<TextEdit>(ctrl))      te->set_text(t);
        else if (auto* rtl = Object::cast_to<RichTextLabel>(ctrl)) rtl->set_text(t);
    } else if (prop == "value") {
        double v = 0.0;
        try { v = std::stod(value); } catch (...) {}
        if      (auto* sb = Object::cast_to<SpinBox>(ctrl))       sb->set_value(v);
        else if (auto* sl = Object::cast_to<Slider>(ctrl))        sl->set_value(v);
        else if (auto* pb = Object::cast_to<ProgressBar>(ctrl))   pb->set_value(v);
    } else if (prop == "checked" || prop == "buttonPressed") {
        if (auto* btn = Object::cast_to<Button>(ctrl))
            btn->set_pressed(value == "true" || value == "1");
    } else if (prop == "disabled") {
        if (auto* btn = Object::cast_to<Button>(ctrl))
            btn->set_disabled(value == "true" || value == "1");
    } else if (prop == "selectedIndex") {
        int idx = 0;
        try { idx = std::stoi(value); } catch (...) {}
        if (auto* ob = Object::cast_to<OptionButton>(ctrl)) ob->select(idx);
    } else if (prop == "scrollV") {
        int v = 0;
        try { v = std::stoi(value); } catch (...) {}
        if (auto* sc = Object::cast_to<ScrollContainer>(ctrl)) sc->set_v_scroll(v);
    }
    return 0;
}

static int sms_ui_invoke(
    const char* object_id, const char* method, const char* args_json,
    char* out_json, int out_cap, char*, int)
{
    const std::string id    = object_id ? object_id : "";
    const std::string mname = method    ? method    : "";
    const std::string args  = args_json ? args_json : "[]";

    if (id == "__log__") {
        UtilityFunctions::print(String(("[SMS] " + first_string_arg(args)).c_str()));
        write_out(out_json, out_cap, "null");
        return 0;
    }

    auto it = SmsBridge::id_map().find(id);
    if (it == SmsBridge::id_map().end() || it->second == nullptr) {
        write_out(out_json, out_cap, "null");
        return 0;
    }
    Control* ctrl = it->second;

    if (mname == "focus") {
        ctrl->grab_focus();
    } else if (mname == "scrollToBottom") {
        if (auto* sc = Object::cast_to<ScrollContainer>(ctrl))
            sc->set_v_scroll(std::numeric_limits<int>::max());
    } else if (mname == "clearItems") {
        if      (auto* ob = Object::cast_to<OptionButton>(ctrl)) ob->clear();
        else if (auto* il = Object::cast_to<ItemList>(ctrl))     il->clear();
    } else if (mname == "addItem") {
        const String item(first_string_arg(args).c_str());
        if      (auto* ob = Object::cast_to<OptionButton>(ctrl)) ob->add_item(item);
        else if (auto* il = Object::cast_to<ItemList>(ctrl))     il->add_item(item);
    }

    write_out(out_json, out_cap, "null");
    return 0;
}

// ---------------------------------------------------------------------------
// SmsBridge implementation
// ---------------------------------------------------------------------------

SmsBridge::SmsBridge() = default;

SmsBridge::~SmsBridge() {
    unload();
}

bool SmsBridge::load(const std::string& repo_root) {
    if (loaded_) return true;

    const char* env_dir = std::getenv("SMS_NATIVE_LIB_DIR");
    const fs::path lib_dir = (env_dir && env_dir[0] != '\0')
        ? fs::path(env_dir)
        : fs::path(repo_root) / "SMSCore.Native" / "build";
    const fs::path lib_path = lib_dir / ("libsms_native" + lib_extension());

    lib_handle_ = platform_load_lib(lib_path);
    if (!lib_handle_) {
        UtilityFunctions::push_warning(String((
            "[ForgeRunner.Native] SMS library not found at " + lib_path.string() +
            " — SMS execution disabled.").c_str()));
        return false;
    }

    create_fn_    = reinterpret_cast<CreateFn> (platform_load_sym(lib_handle_, "sms_native_session_create"));
    load_fn_      = reinterpret_cast<LoadFn>   (platform_load_sym(lib_handle_, "sms_native_session_load"));
    invoke_fn_    = reinterpret_cast<InvokeFn> (platform_load_sym(lib_handle_, "sms_native_session_invoke"));
    dispose_fn_   = reinterpret_cast<DisposeFn>(platform_load_sym(lib_handle_, "sms_native_session_dispose"));
    set_ui_cb_fn_ = reinterpret_cast<SetUiCbFn>(platform_load_sym(lib_handle_, "sms_native_set_ui_callbacks"));

    if (!create_fn_ || !load_fn_ || !invoke_fn_ || !dispose_fn_ || !set_ui_cb_fn_) {
        UtilityFunctions::push_warning("[ForgeRunner.Native] SMS library is missing required symbols.");
        platform_free_lib(lib_handle_);
        lib_handle_ = nullptr;
        return false;
    }

    char err[512] = {};
    set_ui_cb_fn_(&sms_ui_get, &sms_ui_set, &sms_ui_invoke, err, static_cast<int>(sizeof(err)));
    if (err[0] != '\0')
        UtilityFunctions::push_warning(String((
            "[ForgeRunner.Native] SMS set_ui_callbacks warning: " + std::string(err)).c_str()));

    loaded_ = true;
    UtilityFunctions::print("[ForgeRunner.Native] SMS native bridge loaded.");
    return true;
}

void SmsBridge::unload() {
    if (!lib_handle_) return;
    platform_free_lib(lib_handle_);
    lib_handle_   = nullptr;
    create_fn_    = nullptr;
    load_fn_      = nullptr;
    invoke_fn_    = nullptr;
    dispose_fn_   = nullptr;
    set_ui_cb_fn_ = nullptr;
    loaded_       = false;
}

std::int64_t SmsBridge::start_session(const std::string& script_path) {
    if (!loaded_) return -1;

    // sms_native_session_load expects script SOURCE, not a file path.
    std::ifstream f(script_path);
    if (!f.is_open()) {
        UtilityFunctions::push_warning(String((
            "[ForgeRunner.Native] SMS cannot open script: " + script_path).c_str()));
        return -1;
    }
    std::ostringstream ss;
    ss << f.rdbuf();
    const std::string source = ss.str();

    std::int64_t session = -1;
    char err[512] = {};
    if (create_fn_(&session, err, static_cast<int>(sizeof(err))) != 0 || session < 0) {
        UtilityFunctions::push_warning(String((
            "[ForgeRunner.Native] SMS session create failed: " + std::string(err)).c_str()));
        return -1;
    }
    if (load_fn_(session, source.c_str(), err, static_cast<int>(sizeof(err))) != 0) {
        UtilityFunctions::push_warning(String((
            "[ForgeRunner.Native] SMS session load failed: " + std::string(err)).c_str()));
        dispose_fn_(session, nullptr, 0);
        return -1;
    }
    return session;
}

void SmsBridge::dispatch_event(std::int64_t session,
                               const std::string& object_id,
                               const std::string& event_name,
                               const std::string& payload_json) {
    if (!loaded_ || session < 0) return;
    std::int64_t result_session = -1;
    char out[64] = {};
    invoke_fn_(session, object_id.c_str(), event_name.c_str(), payload_json.c_str(),
               &result_session, out, static_cast<int>(sizeof(out)));
}

void SmsBridge::dispose_session(std::int64_t session) {
    if (!loaded_ || session < 0) return;
    dispose_fn_(session, nullptr, 0);
}

} // namespace forge
