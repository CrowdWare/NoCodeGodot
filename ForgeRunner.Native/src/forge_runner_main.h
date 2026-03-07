#pragma once

#include <string>

#include <godot_cpp/classes/node.hpp>

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

private:
    godot::Control* content_root_    = nullptr;
    godot::Timer*   splash_timer_    = nullptr;
    std::string     splash_next_path_;

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
};
