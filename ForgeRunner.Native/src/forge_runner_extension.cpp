#include "forge_runner_main.h"
#include "forge_markdown.h"

#include <cstdio>
#include <map>
#include <godot_cpp/classes/button.hpp>
#include <godot_cpp/classes/code_edit.hpp>
#include <godot_cpp/classes/color_rect.hpp>
#include <godot_cpp/classes/file_access.hpp>
#include <godot_cpp/classes/rich_text_label.hpp>
#include <godot_cpp/classes/text_server.hpp>
#include <godot_cpp/classes/texture_rect.hpp>
#include <godot_cpp/classes/container.hpp>
#include <godot_cpp/classes/control.hpp>
#include <godot_cpp/classes/h_box_container.hpp>
#include <godot_cpp/classes/h_separator.hpp>
#include <godot_cpp/classes/input_event_mouse_button.hpp>
#include <godot_cpp/classes/input_event_mouse_motion.hpp>
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
        popup_->call_deferred("hide");  // deferred: avoids swallowing the next click
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

        // Make target visible BEFORE add_child so TabContainer lays out immediately
        target->set_visible(true);

        remove_child(content);
        // TabContainer hides non-current tabs on remove — restore before re-adding
        content->set_visible(true);
        target->add_child(content);
        target->set_tab_title(target->get_tab_count() - 1, title);
        target->set_current_tab(target->get_tab_count() - 1);
        content->set_anchors_and_offsets_preset(Control::PRESET_FULL_RECT);

        // Collapse source if now empty
        if (get_tab_count() == 0) set_visible(false);
        // Immediate host reflow so the new column gets its correct size right away
        auto* host = Object::cast_to<Container>(get_parent());
        if (host) host->notification(Container::NOTIFICATION_SORT_CHILDREN);
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
        content->set_anchors_and_offsets_preset(Control::PRESET_FULL_RECT);
        win->add_child(content);

        SceneTree* tree = get_tree();
        if (!tree) { win->queue_free(); add_child(content); return; }

        Window* root = tree->get_root();
        if (root->is_embedding_subwindows())
            root->set_embedding_subwindows(false);

        // get_window() returns the Window node this control lives in — that is
        // _mainAppWindow, the real visible app window.  Making our float window
        // transient to it establishes the correct macOS parent/child relationship.
        Window* owner = get_window();
        if (!owner) owner = root;

        float_entries_.push_back({win, content, title});
        win->connect("close_requested",
            callable_mp(this, &ForgeDockingContainerControl::_return_from_float)
                .bind(Variant(static_cast<int64_t>(win->get_instance_id()))));

        win->set_transient(true);
        owner->add_child(win);
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
        if (get_tab_count() == 0) {
            set_visible(false);
            auto* host = Object::cast_to<Container>(get_parent());
            if (host) host->notification(Container::NOTIFICATION_SORT_CHILDREN);
        }
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

    struct DragState { bool active = false; float origin = 0.f; float initial = 0.f; };

    static constexpr int   MAX_H     = 4;     // horizontal gap handles (up to 4 column gaps)
    static constexpr int   MAX_V     = 4;     // vertical split handles (farleft,left,right,farright)
    static constexpr float MIN_COL_W = 60.f;

protected:
    static void _bind_methods() {
        ClassDB::bind_method(D_METHOD("set_gap", "value"), &ForgeDockingHostControl::set_gap);
        ClassDB::bind_method(D_METHOD("get_gap"),          &ForgeDockingHostControl::get_gap);
        ClassDB::bind_method(D_METHOD("_on_h_handle_input", "event", "idx"),
                             &ForgeDockingHostControl::_on_h_handle_input);
        ClassDB::bind_method(D_METHOD("_on_v_handle_input", "event", "idx"),
                             &ForgeDockingHostControl::_on_v_handle_input);
    }

public:
    void   set_gap(double v) { gap_ = (float)(v < 0.0 ? 0.0 : v); queue_sort(); }
    double get_gap()   const { return gap_; }

    void _ready() override {
        _ensure_auto_dock_containers();
        queue_sort();
    }

    void _notification(int what) {
        if (what == NOTIFICATION_SORT_CHILDREN) arrange_children();
    }

    // --- Horizontal resize handle input (resizes left-neighbour column width) ---
    void _on_h_handle_input(Ref<InputEvent> event, int idx) {
        if (idx < 0 || idx >= MAX_H) return;
        Ref<InputEventMouseButton> mb = event;
        if (mb.is_valid() && mb->get_button_index() == MouseButton::MOUSE_BUTTON_LEFT) {
            if (mb->is_pressed()) {
                float init_w = 0.f;
                if (h_left_[idx]) {
                    double fw = h_left_[idx]->get_fixed_width();
                    init_w = (fw > 0.0) ? (float)fw : (float)h_left_[idx]->get_size().x;
                }
                h_drag_[idx] = { true, mb->get_global_position().x, init_w };
                if (h_handles_[idx]) h_handles_[idx]->set_color(Color(0.30f, 0.55f, 0.90f, 0.50f));
            } else {
                h_drag_[idx].active = false;
                if (h_handles_[idx]) h_handles_[idx]->set_color(Color(0.45f, 0.45f, 0.55f, 0.4f));
            }
        }
        Ref<InputEventMouseMotion> mm = event;
        if (mm.is_valid() && h_drag_[idx].active && h_left_[idx]) {
            float delta = mm->get_global_position().x - h_drag_[idx].origin;
            h_left_[idx]->set_fixed_width((double)maxf(MIN_COL_W, h_drag_[idx].initial + delta));
            queue_sort();
        }
    }

    // --- Vertical resize handle input (resizes top/bottom split ratio) ---
    void _on_v_handle_input(Ref<InputEvent> event, int idx) {
        if (idx < 0 || idx >= MAX_V) return;
        Ref<InputEventMouseButton> mb = event;
        if (mb.is_valid() && mb->get_button_index() == MouseButton::MOUSE_BUTTON_LEFT) {
            if (mb->is_pressed()) {
                float init_pct = 50.f;
                if (v_bot_[idx]) {
                    float hp = (float)v_bot_[idx]->get_height_percent();
                    if (hp > 0.f) init_pct = 100.f - hp;
                }
                v_drag_[idx] = { true, mb->get_global_position().y, init_pct };
                if (v_handles_[idx]) v_handles_[idx]->set_color(Color(0.30f, 0.55f, 0.90f, 0.50f));
            } else {
                v_drag_[idx].active = false;
                if (v_handles_[idx]) v_handles_[idx]->set_color(Color(0.45f, 0.45f, 0.55f, 0.4f));
            }
        }
        Ref<InputEventMouseMotion> mm = event;
        if (mm.is_valid() && v_drag_[idx].active && v_bot_[idx]) {
            float total_h = get_size().y;
            if (total_h < 1.f) return;
            float delta     = mm->get_global_position().y - v_drag_[idx].origin;
            float delta_pct = (delta / total_h) * 100.f;
            // bot height_percent = 100 - top_percent
            float new_top_pct = clampf(v_drag_[idx].initial + delta_pct, 10.f, 90.f);
            v_bot_[idx]->set_height_percent((double)(100.f - new_top_pct));
            queue_sort();
        }
    }

private:
    static float clampf(float v, float lo, float hi) { return v < lo ? lo : (v > hi ? hi : v); }
    static float maxf(float a, float b) { return a > b ? a : b; }

    static float resolve_column_width(ForgeDockingContainerControl* top,
                                      ForgeDockingContainerControl* bot, float fallback) {
        float w = fallback;
        if (top && top->get_fixed_width() > 0.0) w = (float)top->get_fixed_width();
        if (bot && bot->get_fixed_width() > 0.0) w = (float)bot->get_fixed_width();
        return maxf(1.f, w);
    }

    static float resolve_bottom_height(ForgeDockingContainerControl* bot, float total_h) {
        if (!bot) return 0.f;
        const float fixed_h = (float)bot->get_fixed_height();
        if (fixed_h > 0.f) return clampf(fixed_h, 0.f, total_h);
        const float pct = (float)bot->get_height_percent();
        if (pct > 0.f) return clampf(total_h * (pct / 100.f), 0.f, total_h);
        return total_h * 0.5f;
    }

    // Layout top+bottom into rect; returns gap top-y (for v-handle placement), -1 if no split
    float layout_column(ForgeDockingContainerControl* top, ForgeDockingContainerControl* bot,
                        const Rect2& rect, float gap_px) {
        if (top && bot) {
            const float total_h = maxf(0.f, rect.size.y);
            const float bot_max = maxf(0.f, total_h - gap_px);
            const float bot_h   = Math::floor(clampf(resolve_bottom_height(bot, total_h), 0.f, bot_max));
            const float bot_y   = rect.position.y + total_h - bot_h;
            const float top_h   = maxf(0.f, bot_y - rect.position.y - gap_px);
            fit_child_in_rect(top, Rect2(rect.position, Vector2(rect.size.x, top_h)));
            fit_child_in_rect(bot, Rect2(Vector2(rect.position.x, bot_y), Vector2(rect.size.x, bot_h)));
            return bot_y - gap_px;  // gap top-y (where v-handle goes)
        }
        if (top) { fit_child_in_rect(top, rect); }
        if (bot) { fit_child_in_rect(bot, rect); }
        return -1.f;
    }


    ColorRect* _ensure_h_handle(int idx) {
        if (h_handles_[idx]) return h_handles_[idx];
        ColorRect* cr = memnew(ColorRect);
        cr->set_color(Color(0.45f, 0.45f, 0.55f, 0.4f));
        cr->set_mouse_filter(Control::MOUSE_FILTER_STOP);
        cr->set_default_cursor_shape(Control::CURSOR_HSIZE);
        cr->set_z_index(900);
        cr->connect("gui_input",
            callable_mp(this, &ForgeDockingHostControl::_on_h_handle_input).bind(Variant(idx)));
        add_child(cr);
        h_handles_[idx] = cr;
        return cr;
    }

    ColorRect* _ensure_v_handle(int idx) {
        if (v_handles_[idx]) return v_handles_[idx];
        ColorRect* cr = memnew(ColorRect);
        cr->set_color(Color(0.45f, 0.45f, 0.55f, 0.4f));
        cr->set_mouse_filter(Control::MOUSE_FILTER_STOP);
        cr->set_default_cursor_shape(Control::CURSOR_VSIZE);
        cr->set_z_index(900);
        cr->connect("gui_input",
            callable_mp(this, &ForgeDockingHostControl::_on_v_handle_input).bind(Variant(idx)));
        add_child(cr);
        v_handles_[idx] = cr;
        return cr;
    }

    void arrange_children() {
        // 5 columns: 0=farleft, 1=left, 2=center, 3=right, 4=farright
        // Bottom rows:          0=farleftbottom, 1=leftbottom, -, 3=rightbottom, 4=farrightbottom
        ForgeDockingContainerControl* col_top[5] = {};
        ForgeDockingContainerControl* col_bot[5] = {};
        std::vector<ForgeDockingContainerControl*> centers;

        const int n = get_child_count();
        for (int i = 0; i < n; ++i) {
            auto* dock = Object::cast_to<ForgeDockingContainerControl>(get_child(i));
            if (!dock || !dock->is_visible()) continue;
            const String side = dock->get_dock_side().to_lower();
            if      (side == "farleft")        col_top[0] = dock;
            else if (side == "farleftbottom")  col_bot[0] = dock;
            else if (side == "left")           col_top[1] = dock;
            else if (side == "leftbottom")     col_bot[1] = dock;
            else if (side == "right")          col_top[3] = dock;
            else if (side == "rightbottom")    col_bot[3] = dock;
            else if (side == "farright")       col_top[4] = dock;
            else if (side == "farrightbottom") col_bot[4] = dock;
            else                               centers.push_back(dock);
        }

        // Center uses first item for sizing; rest stacked invisibly
        bool has[5];
        for (int i = 0; i < 5; ++i) has[i] = (col_top[i] || col_bot[i]);
        if (!centers.empty()) has[2] = true;

        const float total_w = get_size().x;
        const float total_h = get_size().y;
        const float gap_px  = Math::floor(maxf(0.f, gap_));
        const float h_thick = maxf(gap_px, 4.f);  // handle click target width

        // Fixed widths for side columns; center is whatever remains
        float cw[5] = {};
        cw[0] = has[0] ? resolve_column_width(col_top[0], col_bot[0], 220.f) : 0.f;
        cw[1] = has[1] ? resolve_column_width(col_top[1], col_bot[1], 240.f) : 0.f;
        cw[3] = has[3] ? resolve_column_width(col_top[3], col_bot[3], 240.f) : 0.f;
        cw[4] = has[4] ? resolve_column_width(col_top[4], col_bot[4], 220.f) : 0.f;

        int gap_count = 0;
        int prev_vis  = -1;
        for (int i = 0; i < 5; ++i) {
            if (has[i]) { if (prev_vis >= 0) ++gap_count; prev_vis = i; }
        }
        cw[2] = maxf(0.f, total_w - cw[0] - cw[1] - cw[3] - cw[4] - (float)gap_count * gap_px);

        // Hide all handles before re-placing
        for (int i = 0; i < MAX_H; ++i) {
            h_left_[i] = nullptr;
            if (h_handles_[i]) {
                if (!h_drag_[i].active) h_handles_[i]->set_color(Color(0.45f, 0.45f, 0.55f, 0.4f));
                h_handles_[i]->set_visible(false);
            }
        }
        for (int i = 0; i < MAX_V; ++i) {
            v_bot_[i] = nullptr;
            if (v_handles_[i]) {
                if (!v_drag_[i].active) v_handles_[i]->set_color(Color(0.45f, 0.45f, 0.55f, 0.4f));
                v_handles_[i]->set_visible(false);
            }
        }

        // V-handle index by column: 0=farleft, 1=left, -=center, 2=right, 3=farright
        static const int V_IDX[5] = { 0, 1, -1, 2, 3 };

        float x     = 0.f;
        int   h_idx = 0;
        prev_vis    = -1;

        for (int ci = 0; ci < 5; ++ci) {
            if (!has[ci]) continue;

            // Horizontal handle between columns — always placed, min 4px, overlaid if no gap
            if (prev_vis >= 0) {
                const float hw  = maxf(gap_px, 4.f);
                const float hx  = (gap_px > 0.f) ? x : (x - hw * 0.5f);
                ColorRect* hh   = _ensure_h_handle(h_idx);
                fit_child_in_rect(hh, Rect2(hx, 0.f, hw, total_h));
                hh->set_visible(true);
                // Left neighbour (skip flex center — can't resize it directly)
                if (prev_vis != 2)
                    h_left_[h_idx] = col_top[prev_vis] ? col_top[prev_vis] : col_bot[prev_vis];
                ++h_idx;
                x += gap_px;
            }

            Rect2 col_rect(x, 0.f, cw[ci], total_h);

            if (ci == 2) {
                // Center: all extras stacked at zero size
                if (!centers.empty()) {
                    fit_child_in_rect(centers.front(), col_rect);
                    for (std::size_t k = 1; k < centers.size(); ++k)
                        fit_child_in_rect(centers[k], Rect2(col_rect.position, Vector2(0.f, 0.f)));
                }
            } else {
                float gap_y = layout_column(col_top[ci], col_bot[ci], col_rect, gap_px);
                // Vertical handle for split columns — always placed, min 4px, overlaid if no gap
                if (col_top[ci] && col_bot[ci] && gap_y >= 0.f) {
                    int vi = V_IDX[ci];
                    if (vi >= 0) {
                        const float vh_h = maxf(gap_px, 4.f);
                        const float vh_y = (gap_px > 0.f) ? gap_y : (gap_y - vh_h * 0.5f);
                        ColorRect* vh = _ensure_v_handle(vi);
                        fit_child_in_rect(vh, Rect2(x, vh_y, cw[ci], vh_h));
                        vh->set_visible(true);
                        v_bot_[vi] = col_bot[ci];
                    }
                }
            }

            x += cw[ci];
            prev_vis = ci;
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

        std::vector<ForgeDockingContainerControl*> existing;
        int n = get_child_count();
        for (int i = 0; i < n; ++i) {
            auto* d = Object::cast_to<ForgeDockingContainerControl>(get_child(i));
            if (d) existing.push_back(d);
        }

        if (existing.empty()) return;

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

    float gap_ = 0.f;

    ColorRect*                    h_handles_[MAX_H] = {};
    ColorRect*                    v_handles_[MAX_V] = {};
    ForgeDockingContainerControl* h_left_[MAX_H]    = {};  // left neighbour for each h-handle
    ForgeDockingContainerControl* v_bot_[MAX_V]     = {};  // bottom container for each v-handle
    DragState                     h_drag_[MAX_H]    = {};
    DragState                     v_drag_[MAX_V]    = {};
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
// ForgeMarkdownContainer — VBoxContainer that renders a markdown string
// ---------------------------------------------------------------------------

class ForgeMarkdownContainer : public VBoxContainer {
    GDCLASS(ForgeMarkdownContainer, VBoxContainer);

    Ref<StyleBoxFlat> bg_style_;
    float             base_font_size_ = 16.f;

    // Heading font-size multipliers for levels 1–6
    static float heading_scale(int level) {
        static const float S[7] = {1.f, 2.0f, 1.6f, 1.3f, 1.1f, 1.0f, 0.9f};
        return S[(level >= 1 && level <= 6) ? level : 1];
    }

protected:
    static void _bind_methods() {
        ClassDB::bind_method(D_METHOD("set_bg_style", "style"),       &ForgeMarkdownContainer::set_bg_style);
        ClassDB::bind_method(D_METHOD("set_markdown",  "text"),       &ForgeMarkdownContainer::set_markdown);
        ClassDB::bind_method(D_METHOD("set_src",       "path"),       &ForgeMarkdownContainer::set_src);
        ClassDB::bind_method(D_METHOD("set_base_font_size", "size"),  &ForgeMarkdownContainer::set_base_font_size);
    }

public:
    void set_bg_style(Ref<StyleBoxFlat> style) { bg_style_ = style; queue_redraw(); }

    void set_base_font_size(double size) {
        base_font_size_ = (float)size;
        // No lazy rebuild here — caller must call set_markdown again if needed.
    }

    void set_markdown(const String& text) {
        _clear_children();
        if (text.is_empty()) return;
        const std::vector<forge::MarkdownBlock> blocks =
            forge::parse_markdown(text.utf8().get_data());
        for (const auto& b : blocks) _add_block(b);
    }

    void set_src(const String& path) {
        Ref<FileAccess> fa = FileAccess::open(path, FileAccess::READ);
        if (fa.is_valid())
            set_markdown(fa->get_as_text());
        else
            set_markdown(String("[i]Could not load: ") + path + "[/i]");
    }

    void _draw() override {
        if (bg_style_.is_valid())
            draw_style_box(bg_style_, Rect2(Vector2(), get_size()));
    }

private:
    void _clear_children() {
        for (int i = get_child_count() - 1; i >= 0; --i) {
            Node* c = get_child(i);
            remove_child(c);
            c->queue_free();
        }
    }

    void _add_block(const forge::MarkdownBlock& b) {
        switch (b.kind) {
            case forge::BlockKind::Heading:   _add_heading(b);    break;
            case forge::BlockKind::Paragraph: _add_paragraph(b);  break;
            case forge::BlockKind::CodeBlock: _add_code_block(b); break;
            case forge::BlockKind::ListItem:  _add_list_item(b);  break;
            case forge::BlockKind::Image:     _add_image(b);      break;
            case forge::BlockKind::HRule: {
                HSeparator* sep = memnew(HSeparator);
                sep->set_h_size_flags(Control::SIZE_EXPAND_FILL);
                add_child(sep);
                break;
            }
        }
    }

    void _add_heading(const forge::MarkdownBlock& b) {
        Label* lbl = memnew(Label);
        lbl->set_text(String(b.text.c_str()));
        lbl->add_theme_font_size_override("font_size",
            (int)(base_font_size_ * heading_scale(b.level)));
        lbl->set_h_size_flags(Control::SIZE_EXPAND_FILL);
        lbl->set_autowrap_mode(TextServer::AUTOWRAP_WORD_SMART);
        add_child(lbl);
    }

    void _add_paragraph(const forge::MarkdownBlock& b) {
        RichTextLabel* rtl = memnew(RichTextLabel);
        rtl->set_use_bbcode(true);
        rtl->set_bbcode(String(forge::inline_to_bbcode(b.text).c_str()));
        rtl->set_fit_content(true);
        rtl->set_h_size_flags(Control::SIZE_EXPAND_FILL);
        rtl->add_theme_font_size_override("normal_font_size", (int)base_font_size_);
        add_child(rtl);
    }

    void _add_code_block(const forge::MarkdownBlock& b) {
        PanelContainer* panel = memnew(PanelContainer);
        Ref<StyleBoxFlat> style; style.instantiate();
        style->set_bg_color(Color(0.10f, 0.10f, 0.12f, 1.f));
        style->set_corner_radius_all(4);
        style->set_content_margin_all(8.f);
        panel->add_theme_stylebox_override("panel", style);
        panel->set_h_size_flags(Control::SIZE_EXPAND_FILL);

        CodeEdit* edit = memnew(CodeEdit);
        edit->set_text(String(b.text.c_str()));
        edit->set_editable(false);
        edit->set_h_size_flags(Control::SIZE_EXPAND_FILL);
        edit->add_theme_font_size_override("font_size",
            (int)(base_font_size_ * 0.9f));
        panel->add_child(edit);
        add_child(panel);
    }

    void _add_list_item(const forge::MarkdownBlock& b) {
        HBoxContainer* hbox = memnew(HBoxContainer);
        hbox->set_h_size_flags(Control::SIZE_EXPAND_FILL);

        Label* bullet = memnew(Label);
        bullet->set_text("•");
        bullet->add_theme_font_size_override("font_size", (int)base_font_size_);
        bullet->set_v_size_flags(Control::SIZE_SHRINK_BEGIN);
        hbox->add_child(bullet);

        RichTextLabel* rtl = memnew(RichTextLabel);
        rtl->set_use_bbcode(true);
        rtl->set_bbcode(String(forge::inline_to_bbcode(b.text).c_str()));
        rtl->set_fit_content(true);
        rtl->set_h_size_flags(Control::SIZE_EXPAND_FILL);
        rtl->add_theme_font_size_override("normal_font_size", (int)base_font_size_);
        hbox->add_child(rtl);

        add_child(hbox);
    }

    void _add_image(const forge::MarkdownBlock& b) {
        TextureRect* img = memnew(TextureRect);
        img->set_h_size_flags(Control::SIZE_EXPAND_FILL);
        img->set_stretch_mode(TextureRect::STRETCH_KEEP_ASPECT_CENTERED);
        // Texture loading is handled by the UI builder (same as src: on TextureRect)
        img->set_meta("pending_src", String(b.src.c_str()));
        add_child(img);
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
    ClassDB::register_class<ForgeMarkdownContainer>();
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
