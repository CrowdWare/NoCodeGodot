#pragma once

#include "forge_sms_bridge.h"

#include <cstdint>
#include <string>

#include <godot_cpp/classes/node.hpp>
#include <godot_cpp/variant/string.hpp>

// Forward declarations — full headers included in forge_runner_main.cpp
namespace godot {
class Control;
class Timer;
}

class ForgeRunnerNativeMain : public godot::Node {
    GDCLASS(ForgeRunnerNativeMain, godot::Node);

protected:
    static void _bind_methods();

public:
    void _ready() override;

    // SMS event callbacks — called via Godot signal connections.
    // Signal args come first; bound args (object_id, event_name) are appended.
    void on_sms_event(godot::String object_id, godot::String event_name);
    void on_sms_bool_event(bool value, godot::String object_id, godot::String event_name);
    void on_sms_text_event(godot::String text, godot::String object_id, godot::String event_name);
    void on_sms_value_event(double value, godot::String object_id, godot::String event_name);
    void on_sms_item_event(int index, godot::String object_id, godot::String event_name);

private:
    godot::Control* content_root_    = nullptr;
    godot::Timer*   splash_timer_    = nullptr;
    std::string     splash_next_path_;

    forge::SmsBridge sms_bridge_;
    std::int64_t     sms_session_ = -1;

    /// Convert a file:// URL to an absolute filesystem path.
    static std::string file_url_to_path(const std::string& url);

    /// Load the text content of a local file. Returns "" on failure.
    static std::string load_file(const std::string& path);

    /// Parse and display an SML file at @p path.
    void show_sml(const std::string& path);

    /// Remove the current content Control from the scene.
    void clear_content();

    /// Show an error label when loading fails.
    void show_error(const std::string& msg);

    /// Called when the splash timer fires.
    void on_splash_timeout();

    /// Load the companion .sms script and start an SMS session.
    /// Binds Godot signals of all id-registered controls to dispatch events.
    void start_sms(const std::string& sml_path, const std::string& root_name);

    /// Dispose the active SMS session.
    void stop_sms();
};
