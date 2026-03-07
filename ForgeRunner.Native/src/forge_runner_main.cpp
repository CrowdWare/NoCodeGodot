#include "forge_runner_main.h"
#include "forge_ui_builder.h"

#include <cstdlib>
#include <filesystem>
#include <fstream>
#include <sstream>
#include <string>

#include <godot_cpp/classes/button.hpp>
#include <godot_cpp/classes/control.hpp>
#include <godot_cpp/classes/item_list.hpp>
#include <godot_cpp/classes/label.hpp>
#include <godot_cpp/classes/line_edit.hpp>
#include <godot_cpp/classes/option_button.hpp>
#include <godot_cpp/classes/project_settings.hpp>
#include <godot_cpp/classes/slider.hpp>
#include <godot_cpp/classes/spin_box.hpp>
#include <godot_cpp/classes/text_edit.hpp>
#include <godot_cpp/classes/timer.hpp>
#include <godot_cpp/classes/window.hpp>
#include <godot_cpp/core/class_db.hpp>
#include <godot_cpp/variant/callable.hpp>
#include <godot_cpp/variant/utility_functions.hpp>

namespace fs = std::filesystem;
using namespace godot;

// ---------------------------------------------------------------------------
// GDClass boilerplate
// ---------------------------------------------------------------------------

void ForgeRunnerNativeMain::_bind_methods() {
    ClassDB::bind_method(D_METHOD("on_splash_timeout"),
                         &ForgeRunnerNativeMain::on_splash_timeout);
    ClassDB::bind_method(D_METHOD("on_sms_event", "object_id", "event_name"),
                         &ForgeRunnerNativeMain::on_sms_event);
    ClassDB::bind_method(D_METHOD("on_sms_bool_event", "value", "object_id", "event_name"),
                         &ForgeRunnerNativeMain::on_sms_bool_event);
    ClassDB::bind_method(D_METHOD("on_sms_text_event", "text", "object_id", "event_name"),
                         &ForgeRunnerNativeMain::on_sms_text_event);
    ClassDB::bind_method(D_METHOD("on_sms_value_event", "value", "object_id", "event_name"),
                         &ForgeRunnerNativeMain::on_sms_value_event);
    ClassDB::bind_method(D_METHOD("on_sms_item_event", "index", "object_id", "event_name"),
                         &ForgeRunnerNativeMain::on_sms_item_event);
}

// ---------------------------------------------------------------------------
// _ready
// ---------------------------------------------------------------------------

void ForgeRunnerNativeMain::_ready() {
    UtilityFunctions::print("[ForgeRunner.Native] _ready");

    const char* env_url = std::getenv("FORGE_RUNNER_URL");
    if (!env_url || env_url[0] == '\0') {
        show_error("FORGE_RUNNER_URL is not set.");
        return;
    }

    const std::string url(env_url);
    const std::string path = file_url_to_path(url);

    if (path.empty()) {
        show_error("Unsupported URL scheme (only file:// is supported locally): " + url);
        return;
    }

    show_sml(path);
}

// ---------------------------------------------------------------------------
// URL resolution
// ---------------------------------------------------------------------------

std::string ForgeRunnerNativeMain::file_url_to_path(const std::string& url) {
    if (url.size() < 7 || url.substr(0, 7) != "file://") return {};

    std::string path = url.substr(7); // strip "file://"

    // file://localhost/... → /...
    if (path.size() >= 9 && path.substr(0, 9) == "localhost") path = path.substr(9);

    // Ensure leading slash
    if (!path.empty() && path[0] != '/') path = "/" + path;

    return path;
}

// ---------------------------------------------------------------------------
// File loading
// ---------------------------------------------------------------------------

std::string ForgeRunnerNativeMain::load_file(const std::string& path) {
    std::ifstream f(path);
    if (!f.is_open()) return {};
    std::ostringstream ss;
    ss << f.rdbuf();
    return ss.str();
}

// ---------------------------------------------------------------------------
// SML loading + UI build
// ---------------------------------------------------------------------------

void ForgeRunnerNativeMain::show_sml(const std::string& path) {
    clear_content();

    const auto src = load_file(path);
    if (src.empty()) {
        show_error("Could not read file: " + path);
        return;
    }

    smlcore::Document doc;
    try {
        doc = smlcore::parse_document(src);
    } catch (const std::exception& ex) {
        show_error(std::string("SML parse error: ") + ex.what());
        return;
    }

    if (doc.roots.empty()) {
        show_error("SML document has no root node: " + path);
        return;
    }

    // Determine base directory and appres root
    const auto last_sep = path.rfind('/');
    const std::string base_dir = (last_sep != std::string::npos)
                                 ? path.substr(0, last_sep) : ".";

    const char* env_appres = std::getenv("FORGE_RUNNER_APPRES_ROOT");
    std::string appres_root;
    if (env_appres && env_appres[0] != '\0') {
        appres_root = env_appres;
    } else {
        // Default: Godot project root (ForgeRunner.Native/) so that appRes:/
        // paths resolve relative to the native app, not the SML file location.
        String proj_root = ProjectSettings::get_singleton()->globalize_path("res://");
        appres_root = std::string(proj_root.utf8().get_data());
        if (!appres_root.empty() && appres_root.back() == '/')
            appres_root.pop_back();
    }

    // Build UI
    forge::UiBuilder builder(base_dir, appres_root);
    forge::WindowConfig win_cfg;
    Control* ui = builder.build(doc, win_cfg);

    // Apply window settings
    Window* win = get_window();
    if (win) {
        if (win_cfg.is_splash) {
            // Splash: project.godot sets extend_to_title, always_on_top,
            // transparent, resizable=false. Only hide minimize+maximize so
            // just the red close button remains visible.
            win->set_flag(Window::FLAG_MINIMIZE_DISABLED, true);
            win->set_flag(Window::FLAG_MAXIMIZE_DISABLED, true);
            if (win_cfg.width > 0 && win_cfg.height > 0)
                win->set_size(Vector2i(win_cfg.width, win_cfg.height));
        } else {
            // Window: reset splash-mode flags, then apply SML-specified values.
            win->set_flag(Window::FLAG_MINIMIZE_DISABLED, false);
            win->set_flag(Window::FLAG_MAXIMIZE_DISABLED, false);
            win->set_flag(Window::FLAG_ALWAYS_ON_TOP,   false);
            win->set_flag(Window::FLAG_RESIZE_DISABLED, false);
            win->set_flag(Window::FLAG_EXTEND_TO_TITLE, win_cfg.extend_to_title);
            win->set_flag(Window::FLAG_BORDERLESS,      win_cfg.borderless);
            if (!win_cfg.title.empty())
                win->set_title(String(win_cfg.title.c_str()));
            if (win_cfg.width > 0 && win_cfg.height > 0)
                win->set_size(Vector2i(win_cfg.width, win_cfg.height));
            if (win_cfg.min_width > 0 && win_cfg.min_height > 0)
                win->set_min_size(Vector2i(win_cfg.min_width, win_cfg.min_height));
            if (win_cfg.center_on_screen)
                win->move_to_center();
        }
    }

    add_child(ui);
    content_root_ = ui;

    // Start SMS if a companion .sms script exists
    if (!win_cfg.is_splash) {
        start_sms(path, doc.roots[0].name);
    }

    // Splash: schedule next load
    if (win_cfg.is_splash && !win_cfg.splash_load_on_ready.empty()) {
        splash_next_path_ = base_dir + "/" + win_cfg.splash_load_on_ready;

        if (splash_timer_) {
            splash_timer_->stop();
            splash_timer_->queue_free();
        }
        splash_timer_ = memnew(Timer);
        splash_timer_->set_one_shot(true);
        splash_timer_->set_wait_time(
            static_cast<double>(std::max(1, win_cfg.splash_duration_ms)) / 1000.0);
        splash_timer_->connect("timeout",
            Callable(this, "on_splash_timeout"));
        add_child(splash_timer_);
        splash_timer_->start();
    }

    UtilityFunctions::print(String("[ForgeRunner.Native] loaded: ") +
                            String(path.c_str()));
}

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

void ForgeRunnerNativeMain::clear_content() {
    stop_sms();
    if (content_root_) {
        content_root_->queue_free();
        content_root_ = nullptr;
    }
    if (splash_timer_) {
        splash_timer_->stop();
        splash_timer_->queue_free();
        splash_timer_ = nullptr;
    }
    splash_next_path_.clear();
}

void ForgeRunnerNativeMain::show_error(const std::string& msg) {
    UtilityFunctions::push_error(String(("[ForgeRunner.Native] " + msg).c_str()));

    auto* lbl = memnew(Label);
    lbl->set_text(String(msg.c_str()));
    lbl->set_anchor_and_offset(SIDE_LEFT,   0.0f, 16.0f);
    lbl->set_anchor_and_offset(SIDE_TOP,    0.0f, 16.0f);
    lbl->set_anchor_and_offset(SIDE_RIGHT,  1.0f, -16.0f);
    lbl->set_anchor_and_offset(SIDE_BOTTOM, 1.0f, -16.0f);
    lbl->set_autowrap_mode(TextServer::AUTOWRAP_WORD_SMART);
    add_child(lbl);
    content_root_ = lbl;
}

void ForgeRunnerNativeMain::on_splash_timeout() {
    if (splash_next_path_.empty()) return;
    const auto next = splash_next_path_;
    splash_next_path_.clear();
    show_sml(next);
}

// ---------------------------------------------------------------------------
// SMS integration
// ---------------------------------------------------------------------------

void ForgeRunnerNativeMain::start_sms(const std::string& sml_path, const std::string& root_name) {
    // Companion script: same base path with .sms extension
    fs::path script = fs::path(sml_path);
    script.replace_extension(".sms");
    if (!fs::exists(script)) return;

    // Resolve repo root (two levels up from the Godot project root)
    String proj_root = ProjectSettings::get_singleton()->globalize_path("res://");
    const std::string repo_root = fs::path(proj_root.utf8().get_data()).parent_path().parent_path().string();

    if (!sms_bridge_.load(repo_root)) return;

    sms_session_ = sms_bridge_.start_session(script.string());
    if (sms_session_ < 0) return;

    // Connect signals for all id-registered controls
    const auto& id_map = forge::SmsBridge::id_map();
    for (const auto& [id, ctrl] : id_map) {
        if (!ctrl) continue;
        const String gid(id.c_str());

        // 0-arg signals
        auto cb0 = [&](Control* c, const char* signal, const char* event) {
            c->connect(signal,
                callable_mp(this, &ForgeRunnerNativeMain::on_sms_event)
                    .bind(gid, String(event)));
        };
        // bool signals
        auto cbBool = [&](Control* c, const char* signal, const char* event) {
            c->connect(signal,
                callable_mp(this, &ForgeRunnerNativeMain::on_sms_bool_event)
                    .bind(gid, String(event)));
        };
        // text signals
        auto cbText = [&](Control* c, const char* signal, const char* event) {
            c->connect(signal,
                callable_mp(this, &ForgeRunnerNativeMain::on_sms_text_event)
                    .bind(gid, String(event)));
        };
        // numeric value signals
        auto cbVal = [&](Control* c, const char* signal, const char* event) {
            c->connect(signal,
                callable_mp(this, &ForgeRunnerNativeMain::on_sms_value_event)
                    .bind(gid, String(event)));
        };
        // int item signals
        auto cbItem = [&](Control* c, const char* signal, const char* event) {
            c->connect(signal,
                callable_mp(this, &ForgeRunnerNativeMain::on_sms_item_event)
                    .bind(gid, String(event)));
        };

        if (auto* btn = Object::cast_to<Button>(ctrl)) {
            cb0(btn,   "pressed",  "pressed");
            cbBool(btn, "toggled", "toggled");
        }
        if (auto* le = Object::cast_to<LineEdit>(ctrl)) {
            cbText(le, "text_changed",    "textChanged");
            cb0(le,    "text_submitted",  "textSubmitted");
        }
        if (auto* te = Object::cast_to<TextEdit>(ctrl)) {
            cb0(te, "text_changed", "textChanged");
        }
        if (auto* sb = Object::cast_to<SpinBox>(ctrl)) {
            cbVal(sb, "value_changed", "valueChanged");
        }
        if (auto* sl = Object::cast_to<Slider>(ctrl)) {
            cbVal(sl, "value_changed", "valueChanged");
        }
        if (auto* ob = Object::cast_to<OptionButton>(ctrl)) {
            cbItem(ob, "item_selected", "itemSelected");
        }
        if (auto* il = Object::cast_to<ItemList>(ctrl)) {
            cbItem(il, "item_selected", "itemSelected");
        }
    }

    // Dispatch ready event
    const std::string root_lower = [&] {
        std::string s = root_name;
        std::transform(s.begin(), s.end(), s.begin(), [](unsigned char c){ return std::tolower(c); });
        return s;
    }();
    sms_bridge_.dispatch_event(sms_session_, root_lower, "ready");
    UtilityFunctions::print(String(("[ForgeRunner.Native] SMS session started: " + script.string()).c_str()));
}

void ForgeRunnerNativeMain::stop_sms() {
    if (sms_session_ >= 0) {
        sms_bridge_.dispose_session(sms_session_);
        sms_session_ = -1;
    }
    forge::SmsBridge::id_map().clear();
}

// ---------------------------------------------------------------------------
// SMS event handlers
// ---------------------------------------------------------------------------

void ForgeRunnerNativeMain::on_sms_event(String object_id, String event_name) {
    sms_bridge_.dispatch_event(sms_session_,
        object_id.utf8().get_data(), event_name.utf8().get_data());
}

void ForgeRunnerNativeMain::on_sms_bool_event(bool value, String object_id, String event_name) {
    sms_bridge_.dispatch_event(sms_session_,
        object_id.utf8().get_data(), event_name.utf8().get_data(),
        value ? "true" : "false");
}

void ForgeRunnerNativeMain::on_sms_text_event(String text, String object_id, String event_name) {
    std::string json = "\"";
    const std::string raw = text.utf8().get_data();
    for (unsigned char c : raw) {
        if      (c == '"')  json += "\\\"";
        else if (c == '\\') json += "\\\\";
        else if (c == '\n') json += "\\n";
        else                json += static_cast<char>(c);
    }
    json += "\"";
    sms_bridge_.dispatch_event(sms_session_,
        object_id.utf8().get_data(), event_name.utf8().get_data(), json);
}

void ForgeRunnerNativeMain::on_sms_value_event(double value, String object_id, String event_name) {
    char buf[64];
    std::snprintf(buf, sizeof(buf), "%g", value);
    sms_bridge_.dispatch_event(sms_session_,
        object_id.utf8().get_data(), event_name.utf8().get_data(), buf);
}

void ForgeRunnerNativeMain::on_sms_item_event(int index, String object_id, String event_name) {
    char buf[32];
    std::snprintf(buf, sizeof(buf), "%d", index);
    sms_bridge_.dispatch_event(sms_session_,
        object_id.utf8().get_data(), event_name.utf8().get_data(), buf);
}
