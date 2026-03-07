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
#include <godot_cpp/classes/h_split_container.hpp>
#include <godot_cpp/classes/v_split_container.hpp>
#include <godot_cpp/classes/input.hpp>
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

    void   set_fixed_width(double v)  { fixed_width_ = static_cast<float>(v); }
    double get_fixed_width()    const { return fixed_width_; }

    // Override minimum size so ForgeDockingHostControl can freely resize columns.
    // Godot's set_size() clamps to get_combined_minimum_size(), which includes
    // TabContainer's content minimum. Returning zero lets the host set any width.
    virtual Vector2 _get_minimum_size() const override { return Vector2(0.f, 0.f); }

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

    // Walk up the tree to the docking host root (the container that owns all docks).
    // Since docks are now nested inside HSplitContainer/VSplitContainer, we need to
    // walk past those intermediate nodes to find siblings in the whole docking tree.
    Node* _docking_root() const {
        Node* node = get_parent();
        while (node) {
            // Stop when we reach a node that is not a split container or dock container
            // (i.e. the ForgeDockingHostControl or scene root).
            if (!Object::cast_to<HSplitContainer>(node) &&
                !Object::cast_to<VSplitContainer>(node) &&
                !Object::cast_to<ForgeDockingContainerControl>(node))
                return node;
            Node* p = node->get_parent();
            if (!p) return node;
            node = p;
        }
        return nullptr;
    }

    // Collect all ForgeDockingContainerControl descendants of root into out.
    static void _collect_docks(Node* root,
                                std::vector<ForgeDockingContainerControl*>& out) {
        for (int i = 0; i < root->get_child_count(); ++i) {
            Node* child = root->get_child(i);
            auto* dock  = Object::cast_to<ForgeDockingContainerControl>(child);
            if (dock) out.push_back(dock);
            else      _collect_docks(child, out);
        }
    }

    void _update_dock_buttons() {
        bool  can_move = get_drag_to_rearrange_enabled();
        int   rg       = get_tabs_rearrange_group();
        Node* root     = _docking_root();

        std::vector<ForgeDockingContainerControl*> all_docks;
        if (root) _collect_docks(root, all_docks);

        for (auto& [pos, btn] : dock_btns_) {
            bool available = false;
            if (can_move) {
                for (auto* s : all_docks) {
                    if (s == this) continue;
                    if (!s->get_drag_to_rearrange_enabled()) continue;
                    if (s->get_tabs_rearrange_group() != rg) continue;
                    if (s->get_dock_side() == String(pos.c_str())) { available = true; break; }
                }
            }
            btn->set_visible(true);
            btn->set_disabled(!available);
        }
    }

    ForgeDockingContainerControl* _find_sibling(const String& side) {
        Node* root = _docking_root();
        if (!root) return nullptr;
        std::vector<ForgeDockingContainerControl*> all_docks;
        _collect_docks(root, all_docks);
        for (auto* s : all_docks) {
            if (s != this && s->get_dock_side().to_lower() == side.to_lower()) return s;
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
// Uses Godot's HSplitContainer / VSplitContainer for smooth native drag.
// ---------------------------------------------------------------------------

class ForgeDockingHostControl : public Container {
    GDCLASS(ForgeDockingHostControl, Container);

    // Column order: 0=farleft  1=left  2=center  3=right  4=farright
    static constexpr int         N_COLS   = 5;
    static constexpr const char* TOP_SIDE[N_COLS] = {
        "farleft", "left", "center", "right", "farright"
    };
    static constexpr const char* BOT_SIDE[N_COLS] = {
        "farleftbottom", "leftbottom", nullptr, "rightbottom", "farrightbottom"
    };
    // VSplitContainer index per column (-1 = center, no split)
    static int _vi(int ci) {
        static const int T[] = {0, 1, -1, 2, 3}; return T[ci];
    }

protected:
    static void _bind_methods() {
        ClassDB::bind_method(D_METHOD("set_gap", "value"), &ForgeDockingHostControl::set_gap);
        ClassDB::bind_method(D_METHOD("get_gap"),          &ForgeDockingHostControl::get_gap);
        ClassDB::bind_method(D_METHOD("_on_dock_vis_changed", "vi"),
                             &ForgeDockingHostControl::_on_dock_vis_changed);
    }

public:
    void   set_gap(double v) { gap_ = (float)(v < 0.0 ? 0.0 : v); _apply_gap(); }
    double get_gap() const   { return gap_; }

    void _ready() override {
        _ensure_auto_dock_containers();
        _collect_docks();
        _build_split_tree();
    }

    void _notification(int what) {
        // Just fit the root split container to fill us entirely.
        if (what == NOTIFICATION_SORT_CHILDREN && root_) {
            fit_child_in_rect(root_, Rect2(0.f, 0.f, get_size().x, get_size().y));
        }
    }

private:
    // -----------------------------------------------------------------------
    // Setup helpers
    // -----------------------------------------------------------------------

    void _ensure_auto_dock_containers() {
        static constexpr const char* SIDES[9] = {
            "farleft","farleftbottom","left","leftbottom","center",
            "right","rightbottom","farright","farrightbottom"
        };
        // Collect existing + determine drag group
        std::vector<ForgeDockingContainerControl*> existing;
        for (int i = 0; i < get_child_count(); ++i) {
            auto* d = Object::cast_to<ForgeDockingContainerControl>(get_child(i));
            if (d) existing.push_back(d);
        }
        int drag_group = 1;
        for (auto* d : existing) { int rg = d->get_tabs_rearrange_group(); if (rg > 0) { drag_group = rg; break; } }

        for (const char* side : SIDES) {
            bool found = false;
            for (auto* d : existing) if (d->get_dock_side() == String(side)) { found = true; break; }
            if (found) continue;
            auto* dock = memnew(ForgeDockingContainerControl);
            dock->set_name(String("Auto_") + side);
            dock->set_dock_side(String(side));
            dock->set_drag_to_rearrange_enabled(true);
            dock->set_tabs_rearrange_group(drag_group);
            dock->set_fixed_width(220.0);
            dock->set_visible(false);
            add_child(dock);
            existing.push_back(dock);
        }
    }

    void _collect_docks() {
        for (int ci = 0; ci < N_COLS; ++ci) { dock_top_[ci] = nullptr; dock_bot_[ci] = nullptr; }
        for (int i = 0; i < get_child_count(); ++i) {
            auto* d = Object::cast_to<ForgeDockingContainerControl>(get_child(i));
            if (!d) continue;
            String s = d->get_dock_side().to_lower();
            for (int ci = 0; ci < N_COLS; ++ci) {
                if (s == String(TOP_SIDE[ci])) { dock_top_[ci] = d; break; }
                if (BOT_SIDE[ci] && s == String(BOT_SIDE[ci])) { dock_bot_[ci] = d; break; }
            }
        }
    }

    // Move node to new_parent (handles both fresh nodes and already-parented ones).
    static void _move_to(Node* node, Node* new_parent) {
        if (!node || !new_parent || node->get_parent() == new_parent) return;
        if (node->get_parent()) node->reparent(new_parent, false);
        else                    new_parent->add_child(node);
    }

    VSplitContainer* _get_vsplit(int vi) {
        if (!v_splits_[vi]) {
            v_splits_[vi] = memnew(VSplitContainer);
            v_splits_[vi]->add_theme_constant_override("separation", (int)gap_);
        }
        return v_splits_[vi];
    }

    HSplitContainer* _get_hsplit(int hi) {
        if (!h_splits_[hi]) {
            h_splits_[hi] = memnew(HSplitContainer);
            h_splits_[hi]->add_theme_constant_override("separation", (int)gap_);
        }
        return h_splits_[hi];
    }

    // Called when a dock inside a VSplitContainer changes visibility.
    // Hides the VSplitContainer when both children are invisible so that
    // the parent HSplitContainer can collapse that column automatically.
    void _on_dock_vis_changed(int vi) {
        if (vi < 0 || vi >= 4 || !v_splits_[vi]) return;
        auto* vs = v_splits_[vi];
        bool any_vis = false;
        for (int i = 0; i < vs->get_child_count(); ++i) {
            auto* c = Object::cast_to<Control>(vs->get_child(i));
            if (c && c->is_visible()) { any_vis = true; break; }
        }
        vs->set_visible(any_vis);
    }

    void _build_split_tree() {
        // Build per-column content node.
        // ALL docks are included regardless of current visibility so that
        // HSplitContainer can collapse/expand them dynamically as visibility changes.
        Node* col_node[N_COLS] = {};
        for (int ci = 0; ci < N_COLS; ++ci) {
            auto* top = dock_top_[ci];
            if (!top) continue;
            auto* bot = dock_bot_[ci];
            int vi = _vi(ci);
            if (bot && vi >= 0) {
                auto* vs = _get_vsplit(vi);
                _move_to(top, vs);
                _move_to(bot, vs);
                vs->move_child(top, 0);
                vs->move_child(bot, 1);
                // Set initial VSplit visibility and track changes.
                vs->set_visible(top->is_visible() || bot->is_visible());
                top->connect("visibility_changed",
                    callable_mp(this, &ForgeDockingHostControl::_on_dock_vis_changed).bind(Variant(vi)));
                bot->connect("visibility_changed",
                    callable_mp(this, &ForgeDockingHostControl::_on_dock_vis_changed).bind(Variant(vi)));
                col_node[ci] = vs;
            } else {
                col_node[ci] = top;
            }
        }

        // Collect visible column indices.
        std::vector<int> vis;
        for (int ci = 0; ci < N_COLS; ++ci) if (col_node[ci]) vis.push_back(ci);
        if (vis.empty()) return;

        if (vis.size() == 1) {
            auto* ctrl = Object::cast_to<Control>(col_node[vis[0]]);
            _move_to(col_node[vis[0]], this);
            if (ctrl) {
                ctrl->set_h_size_flags(Control::SIZE_FILL | Control::SIZE_EXPAND);
                ctrl->set_v_size_flags(Control::SIZE_FILL | Control::SIZE_EXPAND);
            }
            root_ = ctrl;
            return;
        }

        // Multiple columns: build right-skewed HSplitContainer chain.
        // h[0]{ col[0] | h[1]{ col[1] | h[2]{ ... | col[N-1] }}}
        // Build from right to left so inner containers exist before outer ones reference them.
        int n = (int)vis.size();
        for (int i = n - 2; i >= 0; --i) {
            auto* hs = _get_hsplit(i);
            // Clear stale children.
            while (hs->get_child_count() > 0)
                hs->remove_child(hs->get_child(0));
            // Left child: col_node[vis[i]]
            _move_to(col_node[vis[i]], hs);
            // Right child: next h_split (or last col_node for the rightmost pair)
            if (i == n - 2)
                _move_to(col_node[vis[n - 1]], hs);
            else
                _move_to(h_splits_[i + 1], hs);
        }

        auto* root_hs = h_splits_[0];
        _move_to(root_hs, this);
        root_hs->set_h_size_flags(Control::SIZE_FILL | Control::SIZE_EXPAND);
        root_hs->set_v_size_flags(Control::SIZE_FILL | Control::SIZE_EXPAND);
        root_ = root_hs;
    }

    void _apply_gap() {
        for (auto* hs : h_splits_) if (hs) hs->add_theme_constant_override("separation", (int)gap_);
        for (auto* vs : v_splits_) if (vs) vs->add_theme_constant_override("separation", (int)gap_);
    }

    // -----------------------------------------------------------------------
    // Members
    // -----------------------------------------------------------------------
    float            gap_              = 0.f;
    HSplitContainer* h_splits_[4]      = {};
    VSplitContainer* v_splits_[4]      = {};
    ForgeDockingContainerControl* dock_top_[N_COLS] = {};
    ForgeDockingContainerControl* dock_bot_[N_COLS] = {};
    Control*         root_             = nullptr;
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
        lbl->set_text(String::utf8(b.text.c_str()));
        lbl->add_theme_font_size_override("font_size",
            (int)(base_font_size_ * heading_scale(b.level)));
        lbl->set_h_size_flags(Control::SIZE_EXPAND_FILL);
        lbl->set_autowrap_mode(TextServer::AUTOWRAP_WORD_SMART);
        add_child(lbl);
    }

    void _add_paragraph(const forge::MarkdownBlock& b) {
        RichTextLabel* rtl = memnew(RichTextLabel);
        rtl->set_use_bbcode(true);
        rtl->set_text(String::utf8(forge::inline_to_bbcode(b.text).c_str()));
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

        // Escape BBCode brackets in code text, then wrap in [code]
        std::string escaped;
        for (char c : b.text) {
            if      (c == '[') escaped += "\\[";
            else if (c == ']') escaped += "\\]";
            else               escaped += c;
        }

        RichTextLabel* rtl = memnew(RichTextLabel);
        rtl->set_use_bbcode(true);
        rtl->set_text(String::utf8(("[code]" + escaped + "[/code]").c_str()));
        rtl->set_fit_content(true);
        rtl->set_h_size_flags(Control::SIZE_EXPAND_FILL);
        rtl->add_theme_font_size_override("mono_font_size",   (int)(base_font_size_ * 0.9f));
        rtl->add_theme_font_size_override("normal_font_size", (int)(base_font_size_ * 0.9f));
        panel->add_child(rtl);
        add_child(panel);
    }

    void _add_list_item(const forge::MarkdownBlock& b) {
        HBoxContainer* hbox = memnew(HBoxContainer);
        hbox->set_h_size_flags(Control::SIZE_EXPAND_FILL);

        Label* bullet = memnew(Label);
        bullet->set_text(String::chr(0x2022));
        bullet->add_theme_font_size_override("font_size", (int)base_font_size_);
        bullet->set_v_size_flags(Control::SIZE_SHRINK_BEGIN);
        hbox->add_child(bullet);

        RichTextLabel* rtl = memnew(RichTextLabel);
        rtl->set_use_bbcode(true);
        rtl->set_text(String::utf8(forge::inline_to_bbcode(b.text).c_str()));
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
