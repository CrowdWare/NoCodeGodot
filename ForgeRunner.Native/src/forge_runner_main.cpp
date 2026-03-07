#include "forge_runner_main.h"
#include "forge_ui_builder.h"

#include <cstdlib>
#include <fstream>
#include <sstream>
#include <stdexcept>
#include <string>

#include <godot_cpp/classes/control.hpp>
#include <godot_cpp/classes/label.hpp>
#include <godot_cpp/classes/project_settings.hpp>
#include <godot_cpp/classes/timer.hpp>
#include <godot_cpp/classes/window.hpp>
#include <godot_cpp/core/class_db.hpp>
#include <godot_cpp/variant/utility_functions.hpp>

using namespace godot;

// ---------------------------------------------------------------------------
// GDClass boilerplate
// ---------------------------------------------------------------------------

void ForgeRunnerNativeMain::_bind_methods() {
    ClassDB::bind_method(D_METHOD("on_splash_timeout"),
                         &ForgeRunnerNativeMain::on_splash_timeout);
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
        if (!win_cfg.title.empty())
            win->set_title(String(win_cfg.title.c_str()));
        if (win_cfg.width > 0 && win_cfg.height > 0)
            win->set_size(Vector2i(win_cfg.width, win_cfg.height));
        if (win_cfg.min_width > 0 && win_cfg.min_height > 0)
            win->set_min_size(Vector2i(win_cfg.min_width, win_cfg.min_height));
        if (win_cfg.extend_to_title)
            win->set_flag(Window::FLAG_EXTEND_TO_TITLE, true);
    }

    add_child(ui);
    content_root_ = ui;

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
