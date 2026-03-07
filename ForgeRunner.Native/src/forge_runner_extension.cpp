#include <godot_cpp/classes/container.hpp>
#include <godot_cpp/classes/node.hpp>
#include <godot_cpp/classes/tab_container.hpp>
#include <godot_cpp/core/class_db.hpp>
#include <godot_cpp/core/defs.hpp>
#include <godot_cpp/godot.hpp>
#include <godot_cpp/variant/rect2.hpp>
#include <godot_cpp/variant/utility_functions.hpp>

#include <cstdlib>
#include <vector>

using namespace godot;

class ForgeDockingContainerControl : public TabContainer {
    GDCLASS(ForgeDockingContainerControl, TabContainer);

protected:
    static void _bind_methods() {
        ClassDB::bind_method(D_METHOD("set_dock_side", "side"), &ForgeDockingContainerControl::set_dock_side);
        ClassDB::bind_method(D_METHOD("get_dock_side"), &ForgeDockingContainerControl::get_dock_side);
        ClassDB::bind_method(D_METHOD("set_fixed_width", "value"), &ForgeDockingContainerControl::set_fixed_width);
        ClassDB::bind_method(D_METHOD("get_fixed_width"), &ForgeDockingContainerControl::get_fixed_width);
        ClassDB::bind_method(D_METHOD("set_fixed_height", "value"), &ForgeDockingContainerControl::set_fixed_height);
        ClassDB::bind_method(D_METHOD("get_fixed_height"), &ForgeDockingContainerControl::get_fixed_height);
        ClassDB::bind_method(D_METHOD("set_height_percent", "value"), &ForgeDockingContainerControl::set_height_percent);
        ClassDB::bind_method(D_METHOD("get_height_percent"), &ForgeDockingContainerControl::get_height_percent);
        ClassDB::bind_method(D_METHOD("set_flex", "value"), &ForgeDockingContainerControl::set_flex);
        ClassDB::bind_method(D_METHOD("is_flex"), &ForgeDockingContainerControl::is_flex);
    }

public:
    void set_dock_side(const String &side) { dock_side_ = side.to_lower(); }
    String get_dock_side() const { return dock_side_; }

    void set_fixed_width(double value) { fixed_width_ = static_cast<float>(value); }
    double get_fixed_width() const { return fixed_width_; }

    void set_fixed_height(double value) { fixed_height_ = static_cast<float>(value); }
    double get_fixed_height() const { return fixed_height_; }

    void set_height_percent(double value) { height_percent_ = static_cast<float>(value); }
    double get_height_percent() const { return height_percent_; }

    void set_flex(bool value) { flex_ = value; }
    bool is_flex() const { return flex_; }

private:
    String dock_side_ = "center";
    float fixed_width_ = -1.0f;
    float fixed_height_ = -1.0f;
    float height_percent_ = -1.0f;
    bool flex_ = false;
};

class ForgeDockingHostControl : public Container {
    GDCLASS(ForgeDockingHostControl, Container);

protected:
    static void _bind_methods() {
        ClassDB::bind_method(D_METHOD("set_gap", "value"), &ForgeDockingHostControl::set_gap);
        ClassDB::bind_method(D_METHOD("get_gap"), &ForgeDockingHostControl::get_gap);
    }

public:
    void set_gap(double value) {
        gap_ = static_cast<float>(value < 0.0 ? 0.0 : value);
        queue_sort();
    }

    double get_gap() const {
        return gap_;
    }

    void _ready() override {
        queue_sort();
    }

    void _notification(int what) {
        if (what == NOTIFICATION_SORT_CHILDREN) {
            arrange_children();
        }
    }

private:
    static float clampf(float v, float lo, float hi) {
        if (v < lo) {
            return lo;
        }
        if (v > hi) {
            return hi;
        }
        return v;
    }

    static float maxf(float a, float b) {
        return a > b ? a : b;
    }

    static float resolve_column_width(ForgeDockingContainerControl *top, ForgeDockingContainerControl *bottom, float fallback) {
        float width = fallback;
        if (top != nullptr && top->get_fixed_width() > 0.0) {
            width = static_cast<float>(top->get_fixed_width());
        }
        if (bottom != nullptr && bottom->get_fixed_width() > 0.0) {
            width = static_cast<float>(bottom->get_fixed_width());
        }
        return maxf(1.0f, width);
    }

    static float resolve_bottom_height(ForgeDockingContainerControl *bottom, float total_h) {
        if (bottom == nullptr) {
            return 0.0f;
        }
        const float fixed_h = static_cast<float>(bottom->get_fixed_height());
        if (fixed_h > 0.0f) {
            return clampf(fixed_h, 0.0f, total_h);
        }
        const String side = bottom->get_dock_side().to_lower();
        if (side == "rightbottom") {
            return total_h * 0.5f;
        }
        const float pct = static_cast<float>(bottom->get_height_percent());
        if (pct > 0.0f) {
            return clampf(total_h * (pct / 100.0f), 0.0f, total_h);
        }
        return total_h * 0.42f;
    }

    void layout_column(ForgeDockingContainerControl *top, ForgeDockingContainerControl *bottom, const Rect2 &rect, float gap_px) {
        if (top != nullptr && bottom != nullptr) {
            const float total_h = maxf(0.0f, rect.size.y);
            const float bottom_max = maxf(0.0f, total_h - gap_px);
            const float bottom_h = Math::floor(clampf(resolve_bottom_height(bottom, total_h), 0.0f, bottom_max));
            const float bottom_y = rect.position.y + total_h - bottom_h;
            const float top_h = maxf(0.0f, bottom_y - rect.position.y - gap_px);
            fit_child_in_rect(top, Rect2(rect.position, Vector2(rect.size.x, top_h)));
            fit_child_in_rect(bottom, Rect2(Vector2(rect.position.x, bottom_y), Vector2(rect.size.x, bottom_h)));
            return;
        }
        if (top != nullptr) {
            fit_child_in_rect(top, rect);
            return;
        }
        if (bottom != nullptr) {
            fit_child_in_rect(bottom, rect);
        }
    }

    void arrange_children() {
        ForgeDockingContainerControl *left_top = nullptr;
        ForgeDockingContainerControl *left_bottom = nullptr;
        std::vector<ForgeDockingContainerControl *> centers;
        ForgeDockingContainerControl *right_top = nullptr;
        ForgeDockingContainerControl *right_bottom = nullptr;

        const int child_count = get_child_count();
        for (int i = 0; i < child_count; ++i) {
            auto *dock = Object::cast_to<ForgeDockingContainerControl>(get_child(i));
            if (dock == nullptr || !dock->is_visible()) {
                continue;
            }
            const String side = dock->get_dock_side().to_lower();
            if (side == "left") {
                left_top = dock;
            } else if (side == "leftbottom") {
                left_bottom = dock;
            } else if (side == "right") {
                right_top = dock;
            } else if (side == "rightbottom") {
                right_bottom = dock;
            } else {
                centers.push_back(dock);
            }
        }

        const float total_w = get_size().x;
        const float total_h = get_size().y;
        const bool has_left = (left_top != nullptr || left_bottom != nullptr);
        const bool has_right = (right_top != nullptr || right_bottom != nullptr);
        const float gap_px = Math::floor(maxf(0.0f, gap_));

        float left_w = 0.0f;
        if (has_left) {
            left_w = resolve_column_width(left_top, left_bottom, 240.0f);
        }
        float right_w = 0.0f;
        if (has_right) {
            right_w = resolve_column_width(right_top, right_bottom, 240.0f);
        }

        float used_gap = 0.0f;
        if (has_left) {
            used_gap += gap_px;
        }
        if (has_right) {
            used_gap += gap_px;
        }
        const float center_w = maxf(0.0f, total_w - left_w - right_w - used_gap);

        float x = 0.0f;
        if (has_left) {
            layout_column(left_top, left_bottom, Rect2(x, 0.0f, left_w, total_h), gap_px);
            x += left_w + gap_px;
        }
        if (!centers.empty()) {
            Rect2 center_rect(x, 0.0f, center_w, total_h);
            fit_child_in_rect(centers.front(), center_rect);
            for (size_t idx = 1; idx < centers.size(); ++idx) {
                fit_child_in_rect(centers[idx], Rect2(center_rect.position, Vector2(0.0f, 0.0f)));
            }
            x += center_w + (has_right ? gap_px : 0.0f);
        }
        if (has_right) {
            layout_column(right_top, right_bottom, Rect2(x, 0.0f, right_w, total_h), gap_px);
        }
    }

    float gap_ = 0.0f;
};

class ForgeRunnerNativeMain : public Node {
    GDCLASS(ForgeRunnerNativeMain, Node);

protected:
    static void _bind_methods() {
        ClassDB::bind_method(D_METHOD("set_url", "url"), &ForgeRunnerNativeMain::set_url);
        ClassDB::bind_method(D_METHOD("get_url"), &ForgeRunnerNativeMain::get_url);
    }

public:
    void set_url(const String& url) {
        url_ = url;
    }

    String get_url() const {
        return url_;
    }

    void _ready() override {
        UtilityFunctions::print("[ForgeRunner.Native] native host bootstrap ready.");
        if (url_.is_empty()) {
            if (const char* env_url = std::getenv("FORGE_RUNNER_URL")) {
                url_ = String(env_url);
            }
        }
        if (!url_.is_empty()) {
            UtilityFunctions::print(String("[ForgeRunner.Native] host url=") + url_);
        }
    }

private:
    String url_;
};

void initialize_forge_runner_native(ModuleInitializationLevel p_level) {
    if (p_level != MODULE_INITIALIZATION_LEVEL_SCENE) {
        return;
    }

    ClassDB::register_class<ForgeDockingHostControl>();
    ClassDB::register_class<ForgeDockingContainerControl>();
    ClassDB::register_class<ForgeRunnerNativeMain>();
}

void uninitialize_forge_runner_native(ModuleInitializationLevel p_level) {
    if (p_level != MODULE_INITIALIZATION_LEVEL_SCENE) {
        return;
    }
}

extern "C" {
GDExtensionBool GDE_EXPORT forge_runner_native_library_init(
    GDExtensionInterfaceGetProcAddress p_get_proc_address,
    const GDExtensionClassLibraryPtr p_library,
    GDExtensionInitialization* r_initialization) {
    GDExtensionBinding::InitObject init_obj(p_get_proc_address, p_library, r_initialization);
    init_obj.register_initializer(initialize_forge_runner_native);
    init_obj.register_terminator(uninitialize_forge_runner_native);
    init_obj.set_minimum_library_initialization_level(MODULE_INITIALIZATION_LEVEL_SCENE);
    return init_obj.init();
}
}
