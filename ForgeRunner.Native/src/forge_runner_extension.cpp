#include "forge_runner_main.h"

#include <cstdio>
#include <map>
#include <godot_cpp/classes/button.hpp>
#include <godot_cpp/classes/container.hpp>
#include <godot_cpp/classes/control.hpp>
#include <godot_cpp/classes/h_box_container.hpp>
#include <godot_cpp/classes/h_separator.hpp>
#include <godot_cpp/classes/input_event_mouse_button.hpp>
#include <godot_cpp/classes/label.hpp>
#include <godot_cpp/classes/menu_button.hpp>
#include <godot_cpp/classes/panel_container.hpp>
#include <godot_cpp/classes/popup_menu.hpp>
#include <godot_cpp/classes/scene_tree.hpp>
#include <godot_cpp/classes/style_box_flat.hpp>
#include <godot_cpp/classes/tab_bar.hpp>
#include <godot_cpp/classes/tab_container.hpp>
#include <godot_cpp/classes/display_server.hpp>
#include <godot_cpp/classes/v_box_container.hpp>
#include <godot_cpp/classes/viewport.hpp>
#include <godot_cpp/classes/window.hpp>
#include <godot_cpp/core/class_db.hpp>
#include <godot_cpp/core/defs.hpp>
#include <godot_cpp/godot.hpp>
#include <godot_cpp/variant/rect2.hpp>

#include <vector>

using namespace godot;

// ---------------------------------------------------------------------------
// ForgeDockingContainerControl
// ---------------------------------------------------------------------------

class ForgeDockingContainerControl : public TabContainer {
    GDCLASS(ForgeDockingContainerControl, TabContainer);

    struct FloatEntry {
        Window*  window  = nullptr;
        Control* content = nullptr;
        String   title;
    };

    // Dock grid columns: [top_side, bottom_side] × 4 + center
    static constexpr const char* GRID_COLS[4][2] = {
        {"farleft",  "farleftbottom"},
        {"left",     "leftbottom"},
        {"right",    "rightbottom"},
        {"farright", "farrightbottom"},
    };
    // All 8 side positions + center — always shown, disabled when no target
    static constexpr const char* ALL_POSITIONS[9] = {
        "farleft", "farleftbottom", "left", "leftbottom",
        "center",
        "right", "rightbottom", "farright", "farrightbottom"
    };

protected:
    static void _bind_methods() {
        ClassDB::bind_method(D_METHOD("set_dock_side",      "side"),  &ForgeDockingContainerControl::set_dock_side);
        ClassDB::bind_method(D_METHOD("get_dock_side"),               &ForgeDockingContainerControl::get_dock_side);
        ClassDB::bind_method(D_METHOD("set_fixed_width",    "value"), &ForgeDockingContainerControl::set_fixed_width);
        ClassDB::bind_method(D_METHOD("get_fixed_width"),             &ForgeDockingContainerControl::get_fixed_width);
        ClassDB::bind_method(D_METHOD("set_fixed_height",   "value"), &ForgeDockingContainerControl::set_fixed_height);
        ClassDB::bind_method(D_METHOD("get_fixed_height"),            &ForgeDockingContainerControl::get_fixed_height);
        ClassDB::bind_method(D_METHOD("set_height_percent", "value"), &ForgeDockingContainerControl::set_height_percent);
        ClassDB::bind_method(D_METHOD("get_height_percent"),          &ForgeDockingContainerControl::get_height_percent);
        ClassDB::bind_method(D_METHOD("set_flex",           "value"), &ForgeDockingContainerControl::set_flex);
        ClassDB::bind_method(D_METHOD("is_flex"),                     &ForgeDockingContainerControl::is_flex);
        ClassDB::bind_method(D_METHOD("_on_about_to_popup"),          &ForgeDockingContainerControl::_on_about_to_popup);
        ClassDB::bind_method(D_METHOD("_return_from_float", "win_id"),&ForgeDockingContainerControl::_return_from_float);
        ClassDB::bind_method(D_METHOD("_on_dock_btn_pressed", "pos"), &ForgeDockingContainerControl::_on_dock_btn_pressed);
        ClassDB::bind_method(D_METHOD("_on_float_pressed"),           &ForgeDockingContainerControl::_on_float_pressed);
        ClassDB::bind_method(D_METHOD("_on_close_pressed"),           &ForgeDockingContainerControl::_on_close_pressed);
        ClassDB::bind_method(D_METHOD("_on_overlay_input", "event"),  &ForgeDockingContainerControl::_on_overlay_input);
    }

public:
    // -----------------------------------------------------------------------
    // Lifecycle
    // -----------------------------------------------------------------------
    void _ready() override {
        // Use set_popup() to get the built-in ⋮ button in the tab bar.
        // We suppress the actual popup and show our custom dialog instead.
        popup_ = memnew(PopupMenu);
        add_child(popup_);
        set_popup(popup_);
        popup_->connect("about_to_popup",
            callable_mp(this, &ForgeDockingContainerControl::_on_about_to_popup));
    }

    void _on_about_to_popup() {
        popup_->hide();
        _ensure_dialog();
        _update_dock_buttons();

        Window* root = get_tree() ? get_tree()->get_root() : nullptr;
        if (!root) return;

        if (!overlay_->get_parent()) root->add_child(overlay_);
        if (!dialog_->get_parent())  root->add_child(dialog_);

        overlay_->set_z_index(1999);
        overlay_->set_visible(true);
        dialog_->set_z_index(2000);
        dialog_->set_visible(true);

        // Position just below the tab bar, aligned to its right edge
        TabBar* tb  = get_tab_bar();
        Rect2   tbr = tb ? tb->get_global_rect() : get_global_rect();
        float   dw  = 210.f;
        float   dh  = 220.f;
        float   dx  = tbr.position.x + tbr.size.x - dw;
        float   dy  = tbr.position.y + tbr.size.y + 4.f;

        Rect2 vp = get_viewport()->get_visible_rect();
        if (dx < vp.position.x)                          dx = vp.position.x;
        if (dx + dw > vp.position.x + vp.size.x)        dx = vp.position.x + vp.size.x - dw;
        if (dy + dh > vp.position.y + vp.size.y)        dy = vp.position.y + vp.size.y - dh;

        dialog_->set_global_position(Vector2(dx, dy));
    }

    void _hide_dialog() {
        if (overlay_) overlay_->set_visible(false);
        if (dialog_)  dialog_->set_visible(false);
    }

    // -----------------------------------------------------------------------
    // Dialog button handlers
    // -----------------------------------------------------------------------
    void _on_dock_btn_pressed(String pos) {
        _hide_dialog();
        ForgeDockingContainerControl* target = _find_sibling(pos);
        if (target) _move_tab_to(target);
    }

    void _on_float_pressed() {
        _hide_dialog();
        _make_floating();
    }

    void _on_close_pressed() {
        _hide_dialog();
        _close_panel();
    }

    void _on_overlay_input(Ref<InputEvent> event) {
        Ref<InputEventMouseButton> mb = event;
        if (mb.is_valid() && mb->is_pressed())
            _hide_dialog();
    }

    // -----------------------------------------------------------------------
    // Move current tab to another DockingContainer
    // -----------------------------------------------------------------------
    void _move_tab_to(ForgeDockingContainerControl* target) {
        if (!target) return;
        int idx = get_current_tab();
        if (idx < 0 || idx >= get_tab_count()) return;

        String   title   = get_tab_title(idx);
        Control* content = get_tab_control(idx);
        if (!content) return;

        remove_child(content);
        target->add_child(content);
        target->set_tab_title(target->get_tab_count() - 1, title);
        target->set_current_tab(target->get_tab_count() - 1);
        content->set_anchors_and_offsets_preset(Control::PRESET_FULL_RECT);
    }

    // -----------------------------------------------------------------------
    // Make Floating — pops current tab into a native OS window
    // -----------------------------------------------------------------------
    void _make_floating() {
        int idx = get_current_tab();
        if (idx < 0 || idx >= get_tab_count()) return;

        String   title      = get_tab_title(idx);
        Control* content    = get_tab_control(idx);
        if (!content) return;

        // Capture size + screen position BEFORE removing the tab (layout changes after)
        Rect2   global_rect = get_global_rect();
        Vector2 screen_pos  = get_screen_position();

        remove_child(content);

        Window* win = memnew(Window);
        win->set_title(title);
        win->set_initial_position(Window::WINDOW_INITIAL_POSITION_ABSOLUTE);
        win->set_position(Vector2i((int)screen_pos.x, (int)screen_pos.y));
        win->set_size(Vector2i(
            (int)global_rect.size.x > 220 ? (int)global_rect.size.x : 220,
            (int)global_rect.size.y > 140 ? (int)global_rect.size.y : 140));
        win->set_transient(false);

        content->set_anchors_and_offsets_preset(Control::PRESET_FULL_RECT);
        win->add_child(content);

        SceneTree* tree = get_tree();
        if (!tree) { win->queue_free(); add_child(content); return; }

        // Ensure subwindows are not embedded so the window can move to other monitors
        Window* root = tree->get_root();
        if (root->is_embedding_subwindows())
            root->set_embedding_subwindows(false);

        float_entries_.push_back({win, content, title});
        win->connect("close_requested",
            callable_mp(this, &ForgeDockingContainerControl::_return_from_float)
                .bind(Variant(static_cast<int64_t>(win->get_instance_id()))));

        root->add_child(win);
        win->show();
    }

    // -----------------------------------------------------------------------
    // Return floating tab back to this container
    // -----------------------------------------------------------------------
    void _return_from_float(int64_t win_id) {
        for (int i = 0; i < (int)float_entries_.size(); ++i) {
            FloatEntry& fe = float_entries_[i];
            if (!fe.window) continue;
            if (static_cast<int64_t>(fe.window->get_instance_id()) != win_id) continue;

            Window*  win     = fe.window;
            Control* content = fe.content;
            String   title   = fe.title;

            if (content->get_parent() == win) win->remove_child(content);

            add_child(content);
            set_tab_title(get_tab_count() - 1, title);
            set_current_tab(get_tab_count() - 1);
            content->set_anchors_and_offsets_preset(Control::PRESET_FULL_RECT);

            float_entries_.erase(float_entries_.begin() + i);
            win->queue_free();
            return;
        }
    }

    // -----------------------------------------------------------------------
    // Close current tab
    // -----------------------------------------------------------------------
    void _close_panel() {
        int idx = get_current_tab();
        if (idx < 0 || idx >= get_tab_count()) return;
        Control* content = get_tab_control(idx);
        if (!content) return;
        remove_child(content);
        content->set_visible(false);
        if (get_tab_count() == 0) set_visible(false);
    }

    // -----------------------------------------------------------------------
    // Properties
    // -----------------------------------------------------------------------
    void   set_dock_side(const String& side) { dock_side_ = side.to_lower(); }
    String get_dock_side()    const { return dock_side_; }

    void   set_fixed_width(double v)  { fixed_width_  = static_cast<float>(v); }
    double get_fixed_width()    const { return fixed_width_; }

    void   set_fixed_height(double v) { fixed_height_ = static_cast<float>(v); }
    double get_fixed_height()   const { return fixed_height_; }

    void   set_height_percent(double v) { height_percent_ = static_cast<float>(v); }
    double get_height_percent()   const { return height_percent_; }

    void set_flex(bool v) { flex_ = v; }
    bool is_flex()  const { return flex_; }

private:
    // -----------------------------------------------------------------------
    // Lazy dialog construction
    // -----------------------------------------------------------------------
    void _ensure_dialog() {
        if (dialog_) return;

        // --- Fullscreen overlay (click-outside-to-close) ---
        overlay_ = memnew(Control);
        overlay_->set_name("DockOverlay");
        overlay_->set_anchors_preset(Control::PRESET_FULL_RECT);
        overlay_->set_mouse_filter(Control::MOUSE_FILTER_STOP);
        overlay_->set_visible(false);
        overlay_->connect("gui_input",
            callable_mp(this, &ForgeDockingContainerControl::_on_overlay_input));

        // --- Dialog panel ---
        PanelContainer* dlg = memnew(PanelContainer);
        dlg->set_name("DockDialog");
        dlg->set_custom_minimum_size(Vector2(210, 0));
        dlg->set_visible(false);
        dlg->set_mouse_filter(Control::MOUSE_FILTER_STOP);

        Ref<StyleBoxFlat> dlg_style; dlg_style.instantiate();
        dlg_style->set_bg_color(Color(0.14f, 0.15f, 0.18f, 0.98f));
        dlg_style->set_border_width_all(1);
        dlg_style->set_border_color(Color(0.26f, 0.30f, 0.36f, 1.f));
        dlg_style->set_corner_radius_all(6);
        dlg_style->set_shadow_color(Color(0.f, 0.f, 0.f, 0.40f));
        dlg_style->set_shadow_size(8);
        dlg_style->set_shadow_offset(Vector2(0, 2));
        dlg_style->set_content_margin_all(8.f);
        dlg->add_theme_stylebox_override("panel", dlg_style);

        // Root VBox
        VBoxContainer* vbox = memnew(VBoxContainer);
        vbox->add_theme_constant_override("separation", 6);
        vbox->set_h_size_flags(Control::SIZE_EXPAND_FILL);

        // Header label
        Label* title_label = memnew(Label);
        title_label->set_text("Dock Position");
        title_label->set_horizontal_alignment(HORIZONTAL_ALIGNMENT_CENTER);
        title_label->set_h_size_flags(Control::SIZE_EXPAND_FILL);
        title_label->add_theme_color_override("font_color", Color(0.87f, 0.91f, 0.98f, 1.f));
        vbox->add_child(title_label);

        // Dock grid: columns + center
        HBoxContainer* grid = memnew(HBoxContainer);
        grid->set_h_size_flags(Control::SIZE_SHRINK_CENTER);
        grid->set_v_size_flags(Control::SIZE_SHRINK_CENTER);
        grid->add_theme_constant_override("separation", 3);

        // Left two columns
        grid->add_child(_make_dock_column(GRID_COLS[0][0], GRID_COLS[0][1]));
        grid->add_child(_make_dock_column(GRID_COLS[1][0], GRID_COLS[1][1]));
        // Center slot
        grid->add_child(_make_slot_btn("center", true));
        // Right two columns
        grid->add_child(_make_dock_column(GRID_COLS[2][0], GRID_COLS[2][1]));
        grid->add_child(_make_dock_column(GRID_COLS[3][0], GRID_COLS[3][1]));

        vbox->add_child(grid);

        // Separator
        HSeparator* sep = memnew(HSeparator);
        sep->set_h_size_flags(Control::SIZE_EXPAND_FILL);
        vbox->add_child(sep);

        // Action buttons
        Button* float_btn = memnew(Button);
        float_btn->set_text("  Make Floating");
        float_btn->set_flat(false);
        float_btn->set_text_alignment(HORIZONTAL_ALIGNMENT_LEFT);
        float_btn->set_h_size_flags(Control::SIZE_EXPAND_FILL);
        float_btn->connect("pressed",
            callable_mp(this, &ForgeDockingContainerControl::_on_float_pressed));
        vbox->add_child(float_btn);

        Button* close_btn = memnew(Button);
        close_btn->set_text("  Close Panel");
        close_btn->set_flat(false);
        close_btn->set_text_alignment(HORIZONTAL_ALIGNMENT_LEFT);
        close_btn->set_h_size_flags(Control::SIZE_EXPAND_FILL);
        close_btn->connect("pressed",
            callable_mp(this, &ForgeDockingContainerControl::_on_close_pressed));
        vbox->add_child(close_btn);

        dlg->add_child(vbox);
        dialog_ = dlg;
    }

    VBoxContainer* _make_dock_column(const char* top_pos, const char* bot_pos) {
        VBoxContainer* col = memnew(VBoxContainer);
        col->set_h_size_flags(Control::SIZE_SHRINK_CENTER);
        col->set_v_size_flags(Control::SIZE_EXPAND_FILL);
        col->set_custom_minimum_size(Vector2(36, 0));
        col->add_theme_constant_override("separation", 3);
        col->add_child(_make_slot_btn(top_pos, false));
        col->add_child(_make_slot_btn(bot_pos, false));
        return col;
    }

    Button* _make_slot_btn(const char* pos, bool center) {
        Button* btn = memnew(Button);
        btn->set_text("");
        btn->set_flat(false);
        btn->set_tooltip_text(String(pos));
        if (center) {
            btn->set_custom_minimum_size(Vector2(44, 0));
            btn->set_h_size_flags(Control::SIZE_EXPAND_FILL);
            btn->set_v_size_flags(Control::SIZE_EXPAND_FILL);
        } else {
            btn->set_custom_minimum_size(Vector2(34, 26));
            btn->set_h_size_flags(Control::SIZE_SHRINK_CENTER);
            btn->set_v_size_flags(Control::SIZE_EXPAND_FILL);
        }

        auto mk = [center](float r, float g, float b, float a = 1.f) {
            Ref<StyleBoxFlat> s; s.instantiate();
            s->set_bg_color(Color(r, g, b, a));
            s->set_corner_radius_all(5);
            s->set_border_width_all(0);
            return s;
        };
        if (center) {
            btn->add_theme_stylebox_override("normal",   mk(0.34f, 0.36f, 0.42f));
            btn->add_theme_stylebox_override("hover",    mk(0.40f, 0.42f, 0.50f));
            btn->add_theme_stylebox_override("pressed",  mk(0.30f, 0.44f, 0.68f));
            btn->add_theme_stylebox_override("disabled", mk(0.26f, 0.28f, 0.34f));
        } else {
            btn->add_theme_stylebox_override("normal",   mk(0.24f, 0.24f, 0.28f));
            btn->add_theme_stylebox_override("hover",    mk(0.30f, 0.32f, 0.38f));
            btn->add_theme_stylebox_override("pressed",  mk(0.20f, 0.34f, 0.56f));
            btn->add_theme_stylebox_override("disabled", mk(0.28f, 0.30f, 0.34f, 0.95f));
        }
        btn->add_theme_color_override("font_color", Color(0.96f, 0.96f, 0.98f, 1.f));

        dock_btns_[pos] = btn;
        btn->connect("pressed",
            callable_mp(this, &ForgeDockingContainerControl::_on_dock_btn_pressed)
                .bind(Variant(String(pos))));
        return btn;
    }

    void _update_dock_buttons() {
        bool  can_move = get_drag_to_rearrange_enabled();
        int   rg       = get_tabs_rearrange_group();
        Node* parent   = get_parent();

        for (auto& [pos, btn] : dock_btns_) {
            bool available = false;
            if (can_move && parent) {
                int n = parent->get_child_count();
                for (int i = 0; i < n; ++i) {
                    auto* s = Object::cast_to<ForgeDockingContainerControl>(parent->get_child(i));
                    if (!s || s == this) continue;
                    if (!s->get_drag_to_rearrange_enabled()) continue;
                    if (s->get_tabs_rearrange_group() != rg) continue;
                    if (s->get_dock_side() == String(pos.c_str())) { available = true; break; }
                }
            }
            // All buttons always visible; disabled when no target exists
            btn->set_visible(true);
            btn->set_disabled(!available);
        }
    }

    ForgeDockingContainerControl* _find_sibling(const String& side) {
        Node* parent = get_parent();
        if (!parent) return nullptr;
        int n = parent->get_child_count();
        for (int i = 0; i < n; ++i) {
            auto* s = Object::cast_to<ForgeDockingContainerControl>(parent->get_child(i));
            if (!s || s == this) continue;
            if (s->get_dock_side() == side.to_lower()) return s;
        }
        return nullptr;
    }

    // -----------------------------------------------------------------------
    // Members
    // -----------------------------------------------------------------------
    PopupMenu*                              popup_         = nullptr;
    PanelContainer*                         dialog_        = nullptr;
    Control*                                overlay_       = nullptr;
    std::map<std::string, Button*>          dock_btns_;
    std::vector<FloatEntry>                 float_entries_;
    String                                  dock_side_     = "center";
    float                                   fixed_width_   = -1.0f;
    float                                   fixed_height_  = -1.0f;
    float                                   height_percent_= -1.0f;
    bool                                    flex_          = false;
};

// ---------------------------------------------------------------------------
// ForgeDockingHostControl
// ---------------------------------------------------------------------------

class ForgeDockingHostControl : public Container {
    GDCLASS(ForgeDockingHostControl, Container);

protected:
    static void _bind_methods() {
        ClassDB::bind_method(D_METHOD("set_gap", "value"), &ForgeDockingHostControl::set_gap);
        ClassDB::bind_method(D_METHOD("get_gap"),          &ForgeDockingHostControl::get_gap);
    }

public:
    void   set_gap(double v) { gap_ = static_cast<float>(v < 0.0 ? 0.0 : v); queue_sort(); }
    double get_gap()   const { return gap_; }

    void _ready() override {
        _ensure_auto_dock_containers();
        queue_sort();
    }

    void _notification(int what) {
        if (what == NOTIFICATION_SORT_CHILDREN) arrange_children();
    }

private:
    static float clampf(float v, float lo, float hi) {
        return v < lo ? lo : (v > hi ? hi : v);
    }
    static float maxf(float a, float b) { return a > b ? a : b; }

    static float resolve_column_width(ForgeDockingContainerControl* top,
                                      ForgeDockingContainerControl* bot,
                                      float fallback) {
        float w = fallback;
        if (top && top->get_fixed_width() > 0.0) w = static_cast<float>(top->get_fixed_width());
        if (bot && bot->get_fixed_width() > 0.0) w = static_cast<float>(bot->get_fixed_width());
        return maxf(1.0f, w);
    }

    static float resolve_bottom_height(ForgeDockingContainerControl* bot, float total_h) {
        if (!bot) return 0.0f;
        const float fixed_h = static_cast<float>(bot->get_fixed_height());
        if (fixed_h > 0.0f) return clampf(fixed_h, 0.0f, total_h);
        const String side = bot->get_dock_side().to_lower();
        if (side == "rightbottom") return total_h * 0.5f;
        const float pct = static_cast<float>(bot->get_height_percent());
        if (pct > 0.0f) return clampf(total_h * (pct / 100.0f), 0.0f, total_h);
        return total_h * 0.42f;
    }

    void layout_column(ForgeDockingContainerControl* top, ForgeDockingContainerControl* bot,
                       const Rect2& rect, float gap_px) {
        if (top && bot) {
            const float total_h = maxf(0.0f, rect.size.y);
            const float bot_max = maxf(0.0f, total_h - gap_px);
            const float bot_h   = Math::floor(clampf(resolve_bottom_height(bot, total_h), 0.0f, bot_max));
            const float bot_y   = rect.position.y + total_h - bot_h;
            const float top_h   = maxf(0.0f, bot_y - rect.position.y - gap_px);
            fit_child_in_rect(top, Rect2(rect.position, Vector2(rect.size.x, top_h)));
            fit_child_in_rect(bot, Rect2(Vector2(rect.position.x, bot_y), Vector2(rect.size.x, bot_h)));
            return;
        }
        if (top) { fit_child_in_rect(top, rect); return; }
        if (bot) { fit_child_in_rect(bot, rect); }
    }

    void arrange_children() {
        ForgeDockingContainerControl* left_top    = nullptr;
        ForgeDockingContainerControl* left_bottom = nullptr;
        std::vector<ForgeDockingContainerControl*> centers;
        ForgeDockingContainerControl* right_top   = nullptr;
        ForgeDockingContainerControl* right_bot   = nullptr;

        const int n = get_child_count();
        for (int i = 0; i < n; ++i) {
            auto* dock = Object::cast_to<ForgeDockingContainerControl>(get_child(i));
            if (!dock || !dock->is_visible()) continue;
            const String side = dock->get_dock_side().to_lower();
            if      (side == "left")        left_top    = dock;
            else if (side == "leftbottom")  left_bottom = dock;
            else if (side == "right")       right_top   = dock;
            else if (side == "rightbottom") right_bot   = dock;
            else                            centers.push_back(dock);
        }

        const float total_w   = get_size().x;
        const float total_h   = get_size().y;
        const bool  has_left  = (left_top || left_bottom);
        const bool  has_right = (right_top || right_bot);
        const float gap_px    = Math::floor(maxf(0.0f, gap_));

        float left_w  = has_left  ? resolve_column_width(left_top,  left_bottom, 240.0f) : 0.0f;
        float right_w = has_right ? resolve_column_width(right_top, right_bot,   240.0f) : 0.0f;

        float used_gap = 0.0f;
        if (has_left)  used_gap += gap_px;
        if (has_right) used_gap += gap_px;
        const float center_w = maxf(0.0f, total_w - left_w - right_w - used_gap);

        float x = 0.0f;
        if (has_left) {
            layout_column(left_top, left_bottom, Rect2(x, 0.0f, left_w, total_h), gap_px);
            x += left_w + gap_px;
        }
        if (!centers.empty()) {
            Rect2 cr(x, 0.0f, center_w, total_h);
            fit_child_in_rect(centers.front(), cr);
            for (std::size_t idx = 1; idx < centers.size(); ++idx)
                fit_child_in_rect(centers[idx], Rect2(cr.position, Vector2(0.0f, 0.0f)));
            x += center_w + (has_right ? gap_px : 0.0f);
        }
        if (has_right) {
            layout_column(right_top, right_bot, Rect2(x, 0.0f, right_w, total_h), gap_px);
        }
    }

    void _ensure_auto_dock_containers() {
        static constexpr const char* SIDES[9] = {
            "farleft", "farleftbottom",
            "left",    "leftbottom",
            "center",
            "right",   "rightbottom",
            "farright","farrightbottom"
        };

        // Collect existing ForgeDockingContainerControl children
        std::vector<ForgeDockingContainerControl*> existing;
        int n = get_child_count();
        for (int i = 0; i < n; ++i) {
            auto* d = Object::cast_to<ForgeDockingContainerControl>(get_child(i));
            if (d) existing.push_back(d);
        }

        // Only auto-create if at least one slot already exists (guards against bare hosts)
        if (existing.empty()) return;

        // Inherit rearrange group from first container that has one set
        int drag_group = 1;
        for (auto* d : existing) {
            int rg = d->get_tabs_rearrange_group();
            if (rg > 0) { drag_group = rg; break; }
        }

        for (const char* side : SIDES) {
            bool found = false;
            for (auto* d : existing) {
                if (d->get_dock_side() == String(side)) { found = true; break; }
            }
            if (found) continue;

            auto* auto_dock = memnew(ForgeDockingContainerControl);
            auto_dock->set_name(String("Auto_") + side);
            auto_dock->set_dock_side(String(side));
            auto_dock->set_drag_to_rearrange_enabled(true);
            auto_dock->set_tabs_rearrange_group(drag_group);
            auto_dock->set_fixed_width(220.0);
            auto_dock->set_visible(false);
            add_child(auto_dock);
            existing.push_back(auto_dock);
        }
    }

    float gap_ = 0.0f;
};

// ---------------------------------------------------------------------------
// ForgeWindowDragControl — transparent drag area that moves the OS window
// ---------------------------------------------------------------------------

class ForgeWindowDragControl : public Control {
    GDCLASS(ForgeWindowDragControl, Control);
protected:
    static void _bind_methods() {}
public:
    void _gui_input(const Ref<InputEvent>& p_event) override {
        Ref<InputEventMouseButton> mb = p_event;
        if (mb.is_valid() &&
            mb->get_button_index() == MouseButton::MOUSE_BUTTON_LEFT &&
            mb->is_pressed()) {
            Window* win = get_window();
            if (!win) return;
            if (mb->is_double_click()) {
                auto* ds = DisplayServer::get_singleton();
                bool maximized = ds->window_get_mode(win->get_window_id())
                                 == DisplayServer::WINDOW_MODE_MAXIMIZED;
                ds->window_set_mode(
                    maximized ? DisplayServer::WINDOW_MODE_WINDOWED
                              : DisplayServer::WINDOW_MODE_MAXIMIZED,
                    win->get_window_id());
            } else {
                DisplayServer::get_singleton()->window_start_drag(win->get_window_id());
            }
        }
    }
};

// ---------------------------------------------------------------------------
// ForgeBgVBoxContainer / ForgeBgHBoxContainer
// VBoxContainer / HBoxContainer with custom _draw() for bgColor support.
// StyleBoxFlat is set via set_bg_style(); it handles bg, border, radius, shadow.
// ---------------------------------------------------------------------------

class ForgeBgVBoxContainer : public VBoxContainer {
    GDCLASS(ForgeBgVBoxContainer, VBoxContainer);
    Ref<StyleBoxFlat> bg_style_;
    Color             highlight_color_;
    bool              has_highlight_ = false;

protected:
    static void _bind_methods() {
        ClassDB::bind_method(D_METHOD("set_bg_style",       "style"), &ForgeBgVBoxContainer::set_bg_style);
        ClassDB::bind_method(D_METHOD("set_highlight_color","color"), &ForgeBgVBoxContainer::set_highlight_color);
    }

public:
    void set_bg_style(Ref<StyleBoxFlat> style) {
        bg_style_ = style;
        queue_redraw();
    }
    void set_highlight_color(Color color) {
        highlight_color_ = color;
        has_highlight_ = true;
        queue_redraw();
    }
    void _draw() override {
        if (bg_style_.is_valid())
            draw_style_box(bg_style_, Rect2(Vector2(), get_size()));
        if (has_highlight_) {
            float r = bg_style_.is_valid() ? (float)bg_style_->get_corner_radius(CORNER_TOP_LEFT) : 0.0f;
            Vector2 sz = get_size();
            draw_line(Vector2(r, 0.5f), Vector2(sz.x - r, 0.5f), highlight_color_);
            draw_line(Vector2(0.5f, r), Vector2(0.5f, sz.y - r), highlight_color_);
        }
    }
};

class ForgeBgHBoxContainer : public HBoxContainer {
    GDCLASS(ForgeBgHBoxContainer, HBoxContainer);
    Ref<StyleBoxFlat> bg_style_;
    Color             highlight_color_;
    bool              has_highlight_ = false;

protected:
    static void _bind_methods() {
        ClassDB::bind_method(D_METHOD("set_bg_style",       "style"), &ForgeBgHBoxContainer::set_bg_style);
        ClassDB::bind_method(D_METHOD("set_highlight_color","color"), &ForgeBgHBoxContainer::set_highlight_color);
    }

public:
    void set_bg_style(Ref<StyleBoxFlat> style) {
        bg_style_ = style;
        queue_redraw();
    }
    void set_highlight_color(Color color) {
        highlight_color_ = color;
        has_highlight_ = true;
        queue_redraw();
    }
    void _draw() override {
        if (bg_style_.is_valid())
            draw_style_box(bg_style_, Rect2(Vector2(), get_size()));
        if (has_highlight_) {
            float r = bg_style_.is_valid() ? (float)bg_style_->get_corner_radius(CORNER_TOP_LEFT) : 0.0f;
            Vector2 sz = get_size();
            draw_line(Vector2(r, 0.5f), Vector2(sz.x - r, 0.5f), highlight_color_);
            draw_line(Vector2(0.5f, r), Vector2(0.5f, sz.y - r), highlight_color_);
        }
    }
};

// ---------------------------------------------------------------------------
// GDExtension entry points
// ---------------------------------------------------------------------------

void initialize_forge_runner_native(ModuleInitializationLevel p_level) {
    fprintf(stderr, "[FRN] initialize_forge_runner_native level=%d\n", (int)p_level);
    if (p_level != MODULE_INITIALIZATION_LEVEL_SCENE) return;
    ClassDB::register_class<ForgeBgVBoxContainer>();
    ClassDB::register_class<ForgeBgHBoxContainer>();
    ClassDB::register_class<ForgeWindowDragControl>();
    ClassDB::register_class<ForgeDockingHostControl>();
    ClassDB::register_class<ForgeDockingContainerControl>();
    ClassDB::register_class<ForgeRunnerNativeMain>();
    fprintf(stderr, "[FRN] classes registered\n");
}

void uninitialize_forge_runner_native(ModuleInitializationLevel p_level) {
    (void)p_level;
}

extern "C" {
GDExtensionBool GDE_EXPORT forge_runner_native_library_init(
    GDExtensionInterfaceGetProcAddress p_get_proc_address,
    const GDExtensionClassLibraryPtr   p_library,
    GDExtensionInitialization*         r_initialization)
{
    fprintf(stderr, "[FRN] forge_runner_native_library_init called\n");
    GDExtensionBinding::InitObject init_obj(p_get_proc_address, p_library, r_initialization);
    init_obj.register_initializer(initialize_forge_runner_native);
    init_obj.register_terminator(uninitialize_forge_runner_native);
    init_obj.set_minimum_library_initialization_level(MODULE_INITIALIZATION_LEVEL_SCENE);
    return init_obj.init();
}
}
