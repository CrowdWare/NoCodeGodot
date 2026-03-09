#include "forge_sms_bridge.h"
#include "forge_sms_error_policy.h"
#include "forge_path_resolver.h"

#include <algorithm>
#include <cstdio>
#include <cstdlib>
#include <cstring>
#include <filesystem>
#include <fstream>
#include <limits>
#include <sstream>
#include <unordered_map>
#include <unordered_set>

#if defined(_WIN32)
#include <windows.h>
#else
#include <dlfcn.h>
#endif

#include <godot_cpp/classes/button.hpp>
#include <godot_cpp/classes/base_button.hpp>
#include <godot_cpp/classes/check_box.hpp>
#include <godot_cpp/classes/check_button.hpp>
#include <godot_cpp/classes/control.hpp>
#include <godot_cpp/classes/engine.hpp>
#include <godot_cpp/classes/item_list.hpp>
#include <godot_cpp/classes/json.hpp>
#include <godot_cpp/classes/label.hpp>
#include <godot_cpp/classes/line_edit.hpp>
#include <godot_cpp/classes/main_loop.hpp>
#include <godot_cpp/classes/option_button.hpp>
#include <godot_cpp/classes/progress_bar.hpp>
#include <godot_cpp/classes/rich_text_label.hpp>
#include <godot_cpp/classes/scene_tree.hpp>
#include <godot_cpp/classes/scroll_container.hpp>
#include <godot_cpp/classes/slider.hpp>
#include <godot_cpp/classes/spin_box.hpp>
#include <godot_cpp/classes/text_edit.hpp>
#include <godot_cpp/classes/texture2d.hpp>
#include <godot_cpp/classes/image.hpp>
#include <godot_cpp/classes/image_texture.hpp>
#include <godot_cpp/classes/tree.hpp>
#include <godot_cpp/classes/tree_item.hpp>
#include <godot_cpp/classes/resource.hpp>
#include <godot_cpp/classes/file_access.hpp>
#include <godot_cpp/variant/utility_functions.hpp>

namespace fs = std::filesystem;
using namespace godot;

namespace forge {

namespace {
constexpr int kMaxSmsDispatchDepth = 256;

bool try_quit_on_fatal_sms_error(const std::string& message) {
    if (!sms_error_requires_exit(message)) {
        return false;
    }

    UtilityFunctions::printerr(String(("[ForgeRunner.Native] Fatal RuntimeError. Exiting: " + message).c_str()));
    auto* main_loop = Engine::get_singleton() ? Engine::get_singleton()->get_main_loop() : nullptr;
    auto* tree = Object::cast_to<SceneTree>(main_loop);
    if (tree != nullptr) {
        tree->quit(1);
    }
    return true;
}
} // namespace

// ---------------------------------------------------------------------------
// Global id map
// ---------------------------------------------------------------------------

IdMap& SmsBridge::id_map() {
    static IdMap s_map;
    return s_map;
}

static UiOpenDialogHook& ui_open_dialog_hook() {
    static UiOpenDialogHook hook = nullptr;
    return hook;
}

void set_ui_open_dialog_hook(UiOpenDialogHook hook) {
    ui_open_dialog_hook() = hook;
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

static std::string variant_to_json(const Variant& value) {
    if (value.get_type() == Variant::NIL) return "null";
    const String json = JSON::stringify(value);
    return json.utf8().get_data();
}

static Variant parse_json_variant(const std::string& json_value, bool* ok = nullptr) {
    const Variant parsed = JSON::parse_string(String(json_value.c_str()));
    const bool parsed_ok = !(parsed.get_type() == Variant::NIL && json_value != "null");
    if (ok) *ok = parsed_ok;
    return parsed_ok ? parsed : Variant();
}

static std::unordered_map<std::int64_t, TreeItem*>& tree_item_handles() {
    static std::unordered_map<std::int64_t, TreeItem*> handles;
    return handles;
}

static std::int64_t& next_tree_item_handle() {
    static std::int64_t next = 1;
    return next;
}

static std::int64_t register_tree_item_handle(TreeItem* item) {
    if (item == nullptr) return 0;
    const std::int64_t handle = next_tree_item_handle()++;
    tree_item_handles()[handle] = item;
    return handle;
}

static String app_url_base_dir() {
    const char* env = std::getenv("FORGE_RUNNER_URL");
    if (env == nullptr || env[0] == '\0') return String();
    std::string url(env);
    if (url.rfind("file://", 0) != 0) return String();
    std::string path = url.substr(7);
    return String(fs::path(path).parent_path().string().c_str());
}

static String appres_root_dir() {
    const char* appres = std::getenv("FORGE_RUNNER_APPRES_ROOT");
    if (appres == nullptr || appres[0] == '\0') return String();
    return String(appres);
}

static Ref<Texture2D> load_texture_best_effort(const String& raw_path) {
    if (raw_path.is_empty()) return Ref<Texture2D>();
    const String base_dir = app_url_base_dir();
    const String appres_root = appres_root_dir();
    const std::string resolved = forge::resolve_runtime_asset_path(
        raw_path.utf8().get_data(),
        base_dir.utf8().get_data(),
        appres_root.utf8().get_data());
    if (resolved.empty()) return Ref<Texture2D>();

    const String resolved_path(resolved.c_str());
    if (!FileAccess::file_exists(resolved_path)) {
        static std::unordered_set<std::string> warned_missing;
        if (warned_missing.insert(resolved).second) {
            UtilityFunctions::push_warning(String("[ForgeRunner.Native] Missing tree icon: ") + resolved_path);
        }
        return Ref<Texture2D>();
    }

    Ref<Image> img;
    img.instantiate();
    if (img.is_null() || img->load(resolved_path) != OK) {
        static std::unordered_set<std::string> warned_unreadable;
        if (warned_unreadable.insert(resolved).second) {
            UtilityFunctions::push_warning(String("[ForgeRunner.Native] Failed to read tree icon: ") + resolved_path);
        }
        return Ref<Texture2D>();
    }
    Ref<ImageTexture> tex;
    tex.instantiate();
    if (tex.is_null()) return Ref<Texture2D>();
    tex->set_image(img);
    Ref<Texture2D> out = tex;
    return out;
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

static LineEdit* resolve_line_edit_by_id(const std::string& id) {
    auto it = SmsBridge::id_map().find(id);
    if (it == SmsBridge::id_map().end() || it->second == nullptr) return nullptr;
    return Object::cast_to<LineEdit>(it->second);
}

static double parse_numeric_from_text(const String& text) {
    const std::string raw = text.utf8().get_data();
    std::string normalized;
    normalized.reserve(raw.size());
    for (unsigned char c : raw) {
        const bool ok =
            (c >= '0' && c <= '9') || c == '.' || c == ',' ||
            c == '-' || c == '+' || c == 'e' || c == 'E';
        normalized += ok ? static_cast<char>(c == ',' ? '.' : c) : ' ';
    }
    std::stringstream ss(normalized);
    double v = 0.0;
    ss >> v;
    return ss.fail() ? 0.0 : v;
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
        const auto it = SmsBridge::id_map().find(id);
        const bool exists = (it != SmsBridge::id_map().end() && it->second != nullptr);
        write_out(out_json, out_cap, exists ? "1" : "0");
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
        if (auto* btn = Object::cast_to<BaseButton>(ctrl)) pressed = btn->is_pressed();
        result = pressed ? "true" : "false";
    } else if (prop == "disabled") {
        bool dis = false;
        if (auto* btn = Object::cast_to<BaseButton>(ctrl)) dis = btn->is_disabled();
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
    } else {
        const Variant value = ctrl->get(StringName(prop.c_str()));
        result = variant_to_json(value);
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
        if (auto* btn = Object::cast_to<BaseButton>(ctrl))
            btn->set_pressed(value == "true" || value == "1");
    } else if (prop == "disabled") {
        if (auto* btn = Object::cast_to<BaseButton>(ctrl))
            btn->set_disabled(value == "true" || value == "1");
    } else if (prop == "selectedIndex") {
        int idx = 0;
        try { idx = std::stoi(value); } catch (...) {}
        if (auto* ob = Object::cast_to<OptionButton>(ctrl)) ob->select(idx);
    } else if (prop == "scrollV") {
        int v = 0;
        try { v = std::stoi(value); } catch (...) {}
        if (auto* sc = Object::cast_to<ScrollContainer>(ctrl)) sc->set_v_scroll(v);
    } else {
        bool parsed_ok = false;
        Variant parsed_value = parse_json_variant(value, &parsed_ok);
        if (!parsed_ok) {
            parsed_value = String(json_unquote(value).c_str());
        }
        ctrl->set(StringName(prop.c_str()), parsed_value);
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
        const std::string msg = first_string_arg(args);
        if (mname == "success") {
            UtilityFunctions::print(String(("\x1b[32m" + msg + "\x1b[0m").c_str()));
        } else {
            UtilityFunctions::print(String(msg.c_str()));
        }
        write_out(out_json, out_cap, "null");
        return 0;
    }

    if (id == "__ui__" || id == "ui") {
        bool parsed_ok = false;
        const Variant parsed = parse_json_variant(args, &parsed_ok);
        const Array arr = (parsed_ok && parsed.get_type() == Variant::ARRAY) ? static_cast<Array>(parsed) : Array();
        auto arg_string = [&](int idx) -> std::string {
            if (idx < 0 || idx >= arr.size()) return {};
            return static_cast<String>(arr[idx]).utf8().get_data();
        };
        static std::string s_last_project_path;

        if (mname == "getObject") {
            write_out(out_json, out_cap, json_string(arg_string(0)));
            return 0;
        }
        if (mname == "setLastProjectPath") {
            s_last_project_path = arg_string(0);
            write_out(out_json, out_cap, "null");
            return 0;
        }
        if (mname == "getLastProjectPath") {
            write_out(out_json, out_cap, json_string(s_last_project_path));
            return 0;
        }
        if (mname == "hasLastProject") {
            write_out(out_json, out_cap, s_last_project_path.empty() ? "false" : "true");
            return 0;
        }
        if (mname == "openFileDialog" || mname == "openSaveFileDialog") {
            const auto cb = arg_string(0);
            const auto filter = arg_string(1);
            if (auto hook = ui_open_dialog_hook()) {
                hook(cb, filter, mname == "openSaveFileDialog");
            } else {
                UtilityFunctions::push_warning("[ForgeRunner.Native] ui.open*FileDialog hook is not set.");
            }
            write_out(out_json, out_cap, "null");
            return 0;
        }
        if (mname == "copyTemplateFilesToProject") {
            write_out(out_json, out_cap, "true");
            return 0;
        }
        if (mname == "configureNumericLineEdit") {
            const std::string control_id = arg_string(0);
            if (LineEdit* le = resolve_line_edit_by_id(control_id)) {
                le->set_horizontal_alignment(HORIZONTAL_ALIGNMENT_RIGHT);
                le->set_select_all_on_focus(true);
            }
            write_out(out_json, out_cap, "true");
            return 0;
        }
        if (mname == "setNumericLineEditValue") {
            const std::string control_id = arg_string(0);
            if (LineEdit* le = resolve_line_edit_by_id(control_id)) {
                String value_text;
                if (arr.size() > 1) {
                    const Variant value = arr[1];
                    if (value.get_type() == Variant::FLOAT || value.get_type() == Variant::INT) {
                        value_text = String::num(static_cast<double>(value));
                    } else {
                        value_text = String(value);
                    }
                }
                le->set_text(value_text);
            }
            write_out(out_json, out_cap, "true");
            return 0;
        }
        if (mname == "getNumericLineEditValue") {
            const std::string control_id = arg_string(0);
            double value = 0.0;
            if (LineEdit* le = resolve_line_edit_by_id(control_id)) {
                value = parse_numeric_from_text(le->get_text());
            }
            write_out(out_json, out_cap, std::to_string(value));
            return 0;
        }
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
    } else if (auto* tree = Object::cast_to<Tree>(ctrl)) {
        if (mname == "Clear" || mname == "clear") {
            tree->clear();
            tree_item_handles().clear();
            write_out(out_json, out_cap, "null");
            return 0;
        }
        if (mname == "CreateRoot") {
            bool parsed_ok = false;
            const Variant parsed = parse_json_variant(args, &parsed_ok);
            if (!parsed_ok || parsed.get_type() != Variant::ARRAY) {
                write_out(out_json, out_cap, "0");
                return 0;
            }
            const Array arr = parsed;
            const String text = arr.size() > 0 ? static_cast<String>(arr[0]) : String();
            const String path = arr.size() > 1 ? static_cast<String>(arr[1]) : String();

            TreeItem* item = tree->create_item();
            if (item != nullptr) {
                item->set_text(0, text);
                item->set_collapsed(false);
                item->set_metadata(0, path);
            }
            const std::int64_t handle = register_tree_item_handle(item);
            write_out(out_json, out_cap, std::to_string(static_cast<long long>(handle)));
            return 0;
        }
        if (mname == "CreateChild") {
            bool parsed_ok = false;
            const Variant parsed = parse_json_variant(args, &parsed_ok);
            if (!parsed_ok || parsed.get_type() != Variant::ARRAY) {
                write_out(out_json, out_cap, "0");
                return 0;
            }
            const Array arr = parsed;
            if (arr.size() < 4) {
                write_out(out_json, out_cap, "0");
                return 0;
            }

            const std::int64_t parent_handle = static_cast<std::int64_t>(arr[0]);
            auto parent_it = tree_item_handles().find(parent_handle);
            if (parent_it == tree_item_handles().end() || parent_it->second == nullptr) {
                write_out(out_json, out_cap, "0");
                return 0;
            }

            const String text = static_cast<String>(arr[1]);
            const String path = static_cast<String>(arr[2]);
            const bool is_directory = static_cast<bool>(arr[3]);

            TreeItem* item = tree->create_item(parent_it->second);
            if (item != nullptr) {
                item->set_text(0, is_directory ? (text + String("/")) : text);
                item->set_metadata(0, path);
                item->set_collapsed(true);
            }
            const std::int64_t handle = register_tree_item_handle(item);
            write_out(out_json, out_cap, std::to_string(static_cast<long long>(handle)));
            return 0;
        }
        if (mname == "AddButton") {
            bool parsed_ok = false;
            const Variant parsed = parse_json_variant(args, &parsed_ok);
            if (!parsed_ok || parsed.get_type() != Variant::ARRAY) {
                write_out(out_json, out_cap, "false");
                return 0;
            }
            const Array arr = parsed;
            if (arr.size() < 3) {
                write_out(out_json, out_cap, "false");
                return 0;
            }

            const std::int64_t item_handle = static_cast<std::int64_t>(arr[0]);
            auto item_it = tree_item_handles().find(item_handle);
            if (item_it == tree_item_handles().end() || item_it->second == nullptr) {
                write_out(out_json, out_cap, "false");
                return 0;
            }

            const String icon_path = static_cast<String>(arr[1]);
            const int button_id = static_cast<int>(arr[2]);
            const String tooltip = arr.size() > 3 ? static_cast<String>(arr[3]) : String();

            Ref<Texture2D> icon = load_texture_best_effort(icon_path);
            if (icon.is_null()) {
                write_out(out_json, out_cap, "false");
                return 0;
            }

            item_it->second->add_button(0, icon, button_id, false, tooltip);
            write_out(out_json, out_cap, "true");
            return 0;
        }
        if (mname == "GetSelectedPath") {
            TreeItem* selected = tree->get_selected();
            const String path = selected != nullptr ? static_cast<String>(selected->get_metadata(0)) : String();
            write_out(out_json, out_cap, variant_to_json(path));
            return 0;
        }
        if (mname == "BindEvents") {
            // Events are wired by the native runner bootstrap where applicable.
            write_out(out_json, out_cap, "null");
            return 0;
        }
    } else if (mname == "clearItems") {
        if      (auto* ob = Object::cast_to<OptionButton>(ctrl)) ob->clear();
        else if (auto* il = Object::cast_to<ItemList>(ctrl))     il->clear();
    } else if (mname == "addItem") {
        const String item(first_string_arg(args).c_str());
        if      (auto* ob = Object::cast_to<OptionButton>(ctrl)) ob->add_item(item);
        else if (auto* il = Object::cast_to<ItemList>(ctrl))     il->add_item(item);
    } else if (ctrl->has_method(StringName(mname.c_str()))) {
        Array call_args;
        bool parsed_ok = false;
        const Variant parsed = parse_json_variant(args, &parsed_ok);
        if (parsed_ok && parsed.get_type() == Variant::ARRAY) {
            call_args = parsed;
        } else if (parsed_ok && parsed.get_type() != Variant::NIL) {
            call_args.push_back(parsed);
        }
        const Variant ret = ctrl->callv(StringName(mname.c_str()), call_args);
        write_out(out_json, out_cap, variant_to_json(ret));
        return 0;
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
    struct DispatchDepthGuard {
        int& depth_ref;
        bool entered = false;
        explicit DispatchDepthGuard(int& depth) : depth_ref(depth) {
            depth_ref++;
            entered = true;
        }
        ~DispatchDepthGuard() {
            if (entered && depth_ref > 0) {
                depth_ref--;
            }
        }
    };
    static thread_local int dispatch_depth = 0;
    if (dispatch_depth >= kMaxSmsDispatchDepth) {
        const std::string msg = "RuntimeError: SMS dispatch recursion limit exceeded for '"
            + object_id + "." + event_name + "' (possible stack overflow).";
        UtilityFunctions::push_warning(String(("[ForgeRunner.Native] SMS dispatch failed for '" + object_id + "." + event_name + "': " + msg).c_str()));
        (void)try_quit_on_fatal_sms_error(msg);
        return;
    }
    DispatchDepthGuard depth_guard(dispatch_depth);

    const char* args_json = payload_json.empty() ? "[]" : payload_json.c_str();
    std::int64_t result_session = -1;
    char err[512] = {};
    const int rc = invoke_fn_(session, object_id.c_str(), event_name.c_str(), args_json,
                              &result_session, err, static_cast<int>(sizeof(err)));
    if (rc != 0) {
        const std::string msg = err[0] != '\0' ? std::string(err) : std::string("unknown invoke error");
        if (sms_error_is_missing_handler(msg)) {
            return;
        }
        UtilityFunctions::push_warning(String(("[ForgeRunner.Native] SMS dispatch failed for '" + object_id + "." + event_name + "': " + msg).c_str()));
        (void)try_quit_on_fatal_sms_error(msg);
    }
}

void SmsBridge::dispose_session(std::int64_t session) {
    if (!loaded_ || session < 0) return;
    dispose_fn_(session, nullptr, 0);
}

} // namespace forge
