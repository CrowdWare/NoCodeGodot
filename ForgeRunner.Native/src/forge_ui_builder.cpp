#include "forge_ui_builder.h"
#include "forge_sms_bridge.h"
#include "generated/schema_properties.h"

#include <algorithm>
#include <cctype>
#include <cstdlib>
#include <fstream>
#include <sstream>

// Godot-cpp headers
#include <godot_cpp/classes/aspect_ratio_container.hpp>
#include <godot_cpp/classes/button.hpp>
#include <godot_cpp/classes/center_container.hpp>
#include <godot_cpp/classes/check_box.hpp>
#include <godot_cpp/classes/check_button.hpp>
#include <godot_cpp/classes/code_edit.hpp>
#include <godot_cpp/classes/color_rect.hpp>
#include <godot_cpp/classes/control.hpp>
#include <godot_cpp/classes/grid_container.hpp>
#include <godot_cpp/classes/h_box_container.hpp>
#include <godot_cpp/classes/h_separator.hpp>
#include <godot_cpp/classes/h_slider.hpp>
#include <godot_cpp/classes/h_split_container.hpp>
#include <godot_cpp/classes/image.hpp>
#include <godot_cpp/classes/image_texture.hpp>
#include <godot_cpp/classes/item_list.hpp>
#include <godot_cpp/classes/label.hpp>
#include <godot_cpp/classes/line_edit.hpp>
#include <godot_cpp/classes/link_button.hpp>
#include <godot_cpp/classes/margin_container.hpp>
#include <godot_cpp/classes/menu_bar.hpp>
#include <godot_cpp/classes/menu_button.hpp>
#include <godot_cpp/classes/option_button.hpp>
#include <godot_cpp/classes/panel.hpp>
#include <godot_cpp/classes/panel_container.hpp>
#include <godot_cpp/classes/popup_menu.hpp>
#include <godot_cpp/classes/progress_bar.hpp>
#include <godot_cpp/classes/rich_text_label.hpp>
#include <godot_cpp/classes/scroll_container.hpp>
#include <godot_cpp/classes/spin_box.hpp>
#include <godot_cpp/classes/style_box_flat.hpp>
#include <godot_cpp/classes/system_font.hpp>
#include <godot_cpp/classes/tab_bar.hpp>
#include <godot_cpp/classes/tab_container.hpp>
#include <godot_cpp/classes/text_edit.hpp>
#include <godot_cpp/classes/texture_button.hpp>
#include <godot_cpp/classes/texture_rect.hpp>
#include <godot_cpp/classes/tree.hpp>
#include <godot_cpp/classes/v_box_container.hpp>
#include <godot_cpp/classes/v_separator.hpp>
#include <godot_cpp/classes/v_slider.hpp>
#include <godot_cpp/classes/v_split_container.hpp>
#include <godot_cpp/core/class_db.hpp>
#include <godot_cpp/variant/color.hpp>
#include <godot_cpp/variant/utility_functions.hpp>

using namespace godot;

// ---------------------------------------------------------------------------
// File-local helpers
// ---------------------------------------------------------------------------

static Ref<ImageTexture> load_texture(const std::string& path) {
    if (path.empty()) return {};
    Ref<Image> img;
    img.instantiate();
    if (img->load(String(path.c_str())) != Error::OK) return {};
    return ImageTexture::create_from_image(img);
}

namespace forge {

// ---------------------------------------------------------------------------
// Construction
// ---------------------------------------------------------------------------

UiBuilder::UiBuilder(const std::string& base_dir, const std::string& appres_root)
    : base_dir_(base_dir), appres_root_(appres_root) {
    load_strings();
}

// ---------------------------------------------------------------------------
// Public API
// ---------------------------------------------------------------------------

Control* UiBuilder::build(const smlcore::Document& doc, WindowConfig& out_window) {
    forge::SmsBridge::id_map().clear();

    if (doc.roots.empty()) return memnew(Control);

    const auto& root = doc.roots[0];
    apply_window_props(root, out_window);

    auto* ctrl = build_node(root);
    if (!ctrl) ctrl = memnew(Control);

    // Fill parent viewport
    ctrl->set_anchor_and_offset(SIDE_LEFT,   0.0f, 0.0f);
    ctrl->set_anchor_and_offset(SIDE_TOP,    0.0f, 0.0f);
    ctrl->set_anchor_and_offset(SIDE_RIGHT,  1.0f, 0.0f);
    ctrl->set_anchor_and_offset(SIDE_BOTTOM, 1.0f, 0.0f);

    return ctrl;
}

// ---------------------------------------------------------------------------
// strings.sml
// ---------------------------------------------------------------------------

void UiBuilder::load_strings() {
    const auto path = base_dir_ + "/strings.sml";
    std::ifstream f(path);
    if (!f.is_open()) return;

    std::ostringstream ss;
    ss << f.rdbuf();
    const auto src = ss.str();

    try {
        auto doc = smlcore::parse_document(src);
        for (const auto& root : doc.roots) {
            // Accept any root node name (Strings, strings, etc.)
            for (const auto& prop : root.properties) {
                strings_[prop.name] = prop.value;
            }
        }
    } catch (...) {
        UtilityFunctions::push_warning("[ForgeRunner] Failed to parse strings.sml");
    }
}

// ---------------------------------------------------------------------------
// Window / splash config
// ---------------------------------------------------------------------------

void UiBuilder::apply_window_props(const smlcore::Node& root, WindowConfig& out) {
    std::string nl = root.name;
    std::transform(nl.begin(), nl.end(), nl.begin(),
                   [](unsigned char c){ return std::tolower(c); });

    out.is_splash = (nl == "splashscreen");

    if (const auto* p = root.find_property("title")) {
        out.title = resolve_text(*p);
    }
    if (out.title.empty() && !out.is_splash) out.title = "ForgeRunner";

    const auto sz = root.get_value("size");
    if (!sz.empty()) {
        const auto comma = sz.find(',');
        if (comma != std::string::npos) {
            out.width  = parse_int(sz.substr(0, comma), 0);
            out.height = parse_int(sz.substr(comma + 1), 0);
        }
    }
    const auto msz = root.get_value("minSize");
    if (!msz.empty()) {
        const auto comma = msz.find(',');
        if (comma != std::string::npos) {
            out.min_width  = parse_int(msz.substr(0, comma), 0);
            out.min_height = parse_int(msz.substr(comma + 1), 0);
        }
    }
    if (!out.is_splash) {
        // Window node: read SML flags and centre on screen.
        // Splash flags (borderless, always_on_top, extend_to_title) come from
        // project.godot and are reset by forge_runner_main when leaving splash.
        out.extend_to_title  = parse_bool(root.get_value("extendToTitle", "false"), false);
        out.borderless       = parse_bool(root.get_value("borderless",     "false"), false);
        out.center_on_screen = true;
    }

    if (out.is_splash) {
        out.splash_duration_ms   = parse_int(root.get_value("duration", "3000"), 3000);
        out.splash_load_on_ready = root.get_value("loadOnReady");
    }
}

// ---------------------------------------------------------------------------
// Node building — recursive
// ---------------------------------------------------------------------------

// Parse padding tuple "a, b, c, d" or single "a" into left/top/right/bottom.
static void parse_padding(const std::string& v, int& pl, int& pt, int& pr, int& pb) {
    // Try comma-separated: left, top, right, bottom
    std::vector<int> parts;
    std::istringstream iss(v);
    std::string tok;
    while (std::getline(iss, tok, ',')) {
        tok.erase(tok.begin(), std::find_if(tok.begin(), tok.end(), [](unsigned char c){ return !std::isspace(c); }));
        tok.erase(std::find_if(tok.rbegin(), tok.rend(), [](unsigned char c){ return !std::isspace(c); }).base(), tok.end());
        try { parts.push_back(std::stoi(tok)); } catch (...) {}
    }
    if (parts.size() == 4) { pl = parts[0]; pt = parts[1]; pr = parts[2]; pb = parts[3]; }
    else if (parts.size() == 2) { pl = pr = parts[0]; pt = pb = parts[1]; }
    else if (parts.size() == 1) { pl = pt = pr = pb = parts[0]; }
    else { pl = pt = pr = pb = 0; }
}

Control* UiBuilder::build_node(const smlcore::Node& node) {
    std::string nl = node.name;
    std::transform(nl.begin(), nl.end(), nl.begin(),
                   [](unsigned char c){ return std::tolower(c); });

    // If a non-MarginContainer has padding, wrap it so margins work correctly.
    // The wrapper takes layout properties; the inner control handles content.
    const bool is_box = (nl == "vboxcontainer" || nl == "hboxcontainer" ||
                         nl == "panel" || nl == "panelcontainer");
    const bool has_padding = node.has_property("padding") || node.has_property("paddingLeft") ||
                             node.has_property("paddingTop") || node.has_property("paddingRight") ||
                             node.has_property("paddingBottom");

    if (is_box && has_padding) {
        // Create outer MarginContainer for padding + layout properties
        auto* wrapper = memnew(MarginContainer);

        // Parse and apply padding
        int pl = 0, pt = 0, pr = 0, pb = 0;
        if (node.has_property("padding"))
            parse_padding(node.get_value("padding"), pl, pt, pr, pb);
        if (node.has_property("paddingLeft"))   pl = std::stoi(node.get_value("paddingLeft"));
        if (node.has_property("paddingTop"))    pt = std::stoi(node.get_value("paddingTop"));
        if (node.has_property("paddingRight"))  pr = std::stoi(node.get_value("paddingRight"));
        if (node.has_property("paddingBottom")) pb = std::stoi(node.get_value("paddingBottom"));
        wrapper->add_theme_constant_override("margin_left",   pl);
        wrapper->add_theme_constant_override("margin_top",    pt);
        wrapper->add_theme_constant_override("margin_right",  pr);
        wrapper->add_theme_constant_override("margin_bottom", pb);

        // Apply layout/styling properties to the wrapper
        apply_props(wrapper, node);

        // Build the inner container (no children yet, will add below)
        auto* inner = create_control(nl);
        if (!inner) { memdelete(wrapper); return memnew(Control); }

        // Apply content properties (spacing) to inner
        if (node.has_property("spacing")) {
            if (auto* box = Object::cast_to<BoxContainer>(inner))
                box->add_theme_constant_override("separation", std::stoi(node.get_value("spacing")));
        }

        // Inner fills the wrapper
        inner->set_anchor(SIDE_LEFT, 0); inner->set_anchor(SIDE_RIGHT,  1);
        inner->set_anchor(SIDE_TOP,  0); inner->set_anchor(SIDE_BOTTOM, 1);
        inner->set_h_size_flags(Control::SIZE_EXPAND_FILL);
        inner->set_v_size_flags(Control::SIZE_EXPAND_FILL);

        // Build children into inner
        for (const auto& child : node.children) {
            std::string cnl = child.name;
            std::transform(cnl.begin(), cnl.end(), cnl.begin(), [](unsigned char c){ return std::tolower(c); });
            if (cnl == "item") continue;
            auto* cc = build_node(child);
            if (!cc) continue;
            inner->add_child(cc);
        }

        wrapper->add_child(inner);
        return wrapper;
    }

    auto* ctrl = create_control(nl);
    if (!ctrl) return nullptr;

    apply_props(ctrl, node);

    // Special child handling
    if (nl == "menubar") {
        build_menubar_children(ctrl, node);
        return ctrl;
    }

    // PopupMenu items — handled inside build_menubar_children, skip here
    if (nl == "popupmenu") return ctrl;

    // Recurse children
    for (const auto& child : node.children) {
        std::string cnl = child.name;
        std::transform(cnl.begin(), cnl.end(), cnl.begin(),
                       [](unsigned char c){ return std::tolower(c); });
        if (cnl == "item") continue; // menu items handled separately

        auto* child_ctrl = build_node(child);
        if (!child_ctrl) continue;

        // No auto size flags — explicit sizeFlagsHorizontal/Vertical in SML required

        ctrl->add_child(child_ctrl);

        // Set tab title from attached *.title property (DockingContainer / TabContainer)
        if (auto* tc = Object::cast_to<TabContainer>(ctrl)) {
            for (const auto& prop : child.properties) {
                if (prop.name.size() > 6 &&
                    prop.name.substr(prop.name.size() - 6) == ".title") {
                    int idx = tc->get_tab_count() - 1;
                    tc->set_tab_title(idx, String(prop.value.c_str()));
                    break;
                }
            }
        }
    }

    return ctrl;
}

// ---------------------------------------------------------------------------
// Control factory
// ---------------------------------------------------------------------------

Control* UiBuilder::create_control(const std::string& nl) {
    if (nl == "window" || nl == "splashscreen") return memnew(Panel);
    if (nl == "panel")             return memnew(Panel);
    if (nl == "panelcontainer")    return memnew(PanelContainer);
    if (nl == "vboxcontainer")     return memnew(VBoxContainer);
    if (nl == "hboxcontainer")     return memnew(HBoxContainer);
    if (nl == "centercontainer")   return memnew(CenterContainer);
    if (nl == "margincontainer")   return memnew(MarginContainer);
    if (nl == "scrollcontainer")   return memnew(ScrollContainer);
    if (nl == "gridcontainer")     return memnew(GridContainer);
    if (nl == "hsplitcontainer")   return memnew(HSplitContainer);
    if (nl == "vsplitcontainer")   return memnew(VSplitContainer);
    if (nl == "aspectratiocontainer") return memnew(AspectRatioContainer);
    if (nl == "label")             return memnew(Label);
    if (nl == "richtextlabel")     return memnew(RichTextLabel);
    if (nl == "button")            return memnew(Button);
    if (nl == "linkbutton")        return memnew(LinkButton);
    if (nl == "checkbox")          return memnew(CheckBox);
    if (nl == "checkbutton")       return memnew(CheckButton);
    if (nl == "lineedit")          return memnew(LineEdit);
    if (nl == "textedit")          return memnew(TextEdit);
    if (nl == "codeedit")          return memnew(CodeEdit);
    if (nl == "optionbutton")      return memnew(OptionButton);
    if (nl == "menubutton")        return memnew(MenuButton);
    if (nl == "texturebutton")     return memnew(TextureButton);
    if (nl == "texturerect")       return memnew(TextureRect);
    if (nl == "colorrect")         return memnew(ColorRect);
    if (nl == "hseparator")        return memnew(HSeparator);
    if (nl == "vseparator")        return memnew(VSeparator);
    if (nl == "progressbar")       return memnew(ProgressBar);
    if (nl == "hslider" || nl == "slider") return memnew(HSlider);
    if (nl == "vslider")           return memnew(VSlider);
    if (nl == "spinbox")           return memnew(SpinBox);
    if (nl == "tree")              return memnew(Tree);
    if (nl == "itemlist")          return memnew(ItemList);
    if (nl == "tabbar")            return memnew(TabBar);
    if (nl == "tabcontainer")      return memnew(TabContainer);
    if (nl == "menubar")           return memnew(MenuBar);
    if (nl == "popupmenu")         return memnew(Control); // placeholder
    if (nl == "windowdrag") {
        Variant v = ClassDB::instantiate("ForgeWindowDragControl");
        if (v.get_type() != Variant::NIL) {
            Object* obj = v;
            if (auto* c = Object::cast_to<Control>(obj)) return c;
        }
        return memnew(Control);
    }
    if (nl == "control")           return memnew(Control);

    // Native docking controls (from GDExtension)
    if (nl == "dockinghost") {
        Variant v = ClassDB::instantiate("ForgeDockingHostControl");
        if (v.get_type() != Variant::NIL) {
            Object* obj = v;
            if (auto* c = Object::cast_to<Control>(obj)) return c;
        }
        return memnew(HBoxContainer);
    }
    if (nl == "dockingcontainer") {
        Variant v = ClassDB::instantiate("ForgeDockingContainerControl");
        if (v.get_type() != Variant::NIL) {
            Object* obj = v;
            if (auto* c = Object::cast_to<Control>(obj)) return c;
        }
        return memnew(TabContainer);
    }

    // Dotted node names (components, ui.NavTab, etc.) → Button placeholder
    if (nl.find('.') != std::string::npos) return memnew(Button);

    // Unknown → generic Control
    return memnew(Control);
}

// ---------------------------------------------------------------------------
// Property mapping
// ---------------------------------------------------------------------------

void UiBuilder::apply_props(Control* ctrl, const smlcore::Node& node) {
    std::string nl = node.name;
    std::transform(nl.begin(), nl.end(), nl.begin(),
                   [](unsigned char c){ return std::tolower(c); });

    // --- id → node name + SMS id_map registration ---
    if (node.has_property("id")) {
        const auto id_val = node.get_value("id");
        ctrl->set_name(String(id_val.c_str()));
        forge::SmsBridge::id_map()[id_val] = ctrl;
    } else {
        for (const auto& prop : node.properties) {
            const auto& pn = prop.name;
            if (pn.size() > 6 && pn.substr(pn.size() - 6) == ".title") {
                ctrl->set_name(String(prop.value.c_str()));
                break;
            }
        }
    }

    // --- visible ---
    if (node.has_property("visible"))
        ctrl->set_visible(parse_bool(node.get_value("visible"), true));

    // --- size: fixedWidth / fixedHeight / width / height / customMinimumSize ---
    {
        float min_w = ctrl->get_custom_minimum_size().x;
        float min_h = ctrl->get_custom_minimum_size().y;
        bool changed = false;
        if (node.has_property("customMinimumSize")) {
            const auto v = node.get_value("customMinimumSize");
            const auto comma = v.find(',');
            if (comma != std::string::npos) {
                min_w = parse_float(v.substr(0, comma));
                min_h = parse_float(v.substr(comma + 1));
            } else {
                min_w = min_h = parse_float(v);
            }
            changed = true;
        }
        for (const char* key : {"fixedWidth", "width"}) {
            if (node.has_property(key)) { min_w = static_cast<float>(parse_int(node.get_value(key))); changed = true; break; }
        }
        for (const char* key : {"fixedHeight", "height"}) {
            if (node.has_property(key)) { min_h = static_cast<float>(parse_int(node.get_value(key))); changed = true; break; }
        }
        if (changed) ctrl->set_custom_minimum_size(Vector2(min_w, min_h));
    }

    // --- anchors (individual floats) ---
    if (node.has_property("anchorLeft"))
        ctrl->set_anchor(SIDE_LEFT,   parse_float(node.get_value("anchorLeft")));
    if (node.has_property("anchorRight"))
        ctrl->set_anchor(SIDE_RIGHT,  parse_float(node.get_value("anchorRight")));
    if (node.has_property("anchorTop"))
        ctrl->set_anchor(SIDE_TOP,    parse_float(node.get_value("anchorTop")));
    if (node.has_property("anchorBottom"))
        ctrl->set_anchor(SIDE_BOTTOM, parse_float(node.get_value("anchorBottom")));

    // --- anchors: left | top | right | bottom (pipe-separated keyword preset) ---
    if (node.has_property("anchors")) {
        const auto av = node.get_value("anchors");
        const bool al = av.find("left")   != std::string::npos;
        const bool ar = av.find("right")  != std::string::npos;
        const bool at = av.find("top")    != std::string::npos;
        const bool ab = av.find("bottom") != std::string::npos;
        if (al || ar || at || ab) {
            ctrl->set_anchor(SIDE_LEFT,   al ? 0.0f : ctrl->get_anchor(SIDE_LEFT));
            ctrl->set_anchor(SIDE_RIGHT,  ar ? 1.0f : ctrl->get_anchor(SIDE_RIGHT));
            ctrl->set_anchor(SIDE_TOP,    at ? 0.0f : ctrl->get_anchor(SIDE_TOP));
            ctrl->set_anchor(SIDE_BOTTOM, ab ? 1.0f : ctrl->get_anchor(SIDE_BOTTOM));
        }
    }

    // --- offsets (explicit sides) ---
    if (node.has_property("offsetLeft"))
        ctrl->set_offset(SIDE_LEFT,   parse_float(node.get_value("offsetLeft")));
    if (node.has_property("offsetTop"))
        ctrl->set_offset(SIDE_TOP,    parse_float(node.get_value("offsetTop")));
    if (node.has_property("offsetRight"))
        ctrl->set_offset(SIDE_RIGHT,  parse_float(node.get_value("offsetRight")));
    if (node.has_property("offsetBottom"))
        ctrl->set_offset(SIDE_BOTTOM, parse_float(node.get_value("offsetBottom")));

    // --- top / left / right / bottom as pixel offsets ---
    if (node.has_property("top"))    ctrl->set_offset(SIDE_TOP,    parse_float(node.get_value("top")));
    if (node.has_property("left"))   ctrl->set_offset(SIDE_LEFT,   parse_float(node.get_value("left")));
    if (node.has_property("right"))  ctrl->set_offset(SIDE_RIGHT,  parse_float(node.get_value("right")));
    if (node.has_property("bottom")) ctrl->set_offset(SIDE_BOTTOM, parse_float(node.get_value("bottom")));

    // --- size flags ---
    if (node.has_property("sizeFlagsHorizontal"))
        ctrl->set_h_size_flags(static_cast<Control::SizeFlags>(
            parse_size_flags(node.get_value("sizeFlagsHorizontal"), Control::SIZE_FILL)));
    if (node.has_property("sizeFlagsVertical"))
        ctrl->set_v_size_flags(static_cast<Control::SizeFlags>(
            parse_size_flags(node.get_value("sizeFlagsVertical"), Control::SIZE_FILL)));

    // --- windowdrag: transparent pass-through ---
    if (nl == "windowdrag")
        ctrl->set_mouse_filter(Control::MOUSE_FILTER_PASS);

    // --- mouse filter ---
    if (node.has_property("mouseFilter")) {
        const auto mf = node.get_value("mouseFilter");
        if (mf == "stop" || mf == "0")   ctrl->set_mouse_filter(Control::MOUSE_FILTER_STOP);
        else if (mf == "pass" || mf == "1") ctrl->set_mouse_filter(Control::MOUSE_FILTER_PASS);
        else if (mf == "ignore" || mf == "2") ctrl->set_mouse_filter(Control::MOUSE_FILTER_IGNORE);
    }

    // --- spacing (BoxContainer) ---
    if (node.has_property("spacing")) {
        if (auto* box = Object::cast_to<BoxContainer>(ctrl))
            box->add_theme_constant_override("separation", parse_int(node.get_value("spacing")));
    }

    // --- padding (MarginContainer) ---
    if (auto* margin = Object::cast_to<MarginContainer>(ctrl)) {
        if (node.has_property("padding")) {
            int v = parse_int(node.get_value("padding"));
            margin->add_theme_constant_override("margin_left",   v);
            margin->add_theme_constant_override("margin_top",    v);
            margin->add_theme_constant_override("margin_right",  v);
            margin->add_theme_constant_override("margin_bottom", v);
        }
        if (node.has_property("paddingLeft"))
            margin->add_theme_constant_override("margin_left",   parse_int(node.get_value("paddingLeft")));
        if (node.has_property("paddingTop"))
            margin->add_theme_constant_override("margin_top",    parse_int(node.get_value("paddingTop")));
        if (node.has_property("paddingRight"))
            margin->add_theme_constant_override("margin_right",  parse_int(node.get_value("paddingRight")));
        if (node.has_property("paddingBottom"))
            margin->add_theme_constant_override("margin_bottom", parse_int(node.get_value("paddingBottom")));
    }

    // --- columns (GridContainer) ---
    if (auto* grid = Object::cast_to<GridContainer>(ctrl)) {
        if (node.has_property("columns"))
            grid->set_columns(parse_int(node.get_value("columns"), 1));
    }

    // --- Label ---
    if (auto* lbl = Object::cast_to<Label>(ctrl)) {
        if (const auto* p = node.find_property("text"))
            lbl->set_text(String(resolve_text(*p).c_str()));
        if (node.has_property("wrap") && parse_bool(node.get_value("wrap")))
            lbl->set_autowrap_mode(TextServer::AUTOWRAP_WORD_SMART);
        if (node.has_property("align")) {
            const auto a = node.get_value("align");
            if (a == "center") lbl->set_horizontal_alignment(HORIZONTAL_ALIGNMENT_CENTER);
            else if (a == "right") lbl->set_horizontal_alignment(HORIZONTAL_ALIGNMENT_RIGHT);
        }
    }

    // --- RichTextLabel ---
    if (auto* rtl = Object::cast_to<RichTextLabel>(ctrl)) {
        if (const auto* p = node.find_property("text"))
            rtl->set_text(String(resolve_text(*p).c_str()));
        rtl->set_fit_content(node.has_property("fitContent")
                             ? parse_bool(node.get_value("fitContent")) : false);
    }

    // --- Button / LinkButton / CheckBox / CheckButton ---
    if (auto* btn = Object::cast_to<Button>(ctrl)) {
        if (const auto* p = node.find_property("text"))
            btn->set_text(String(resolve_text(*p).c_str()));
        if (node.has_property("toggleMode"))
            btn->set_toggle_mode(parse_bool(node.get_value("toggleMode")));
        if (node.has_property("buttonPressed"))
            btn->set_pressed(parse_bool(node.get_value("buttonPressed")));
        if (node.has_property("disabled"))
            btn->set_disabled(parse_bool(node.get_value("disabled")));
        if (node.has_property("flat"))
            btn->set_flat(parse_bool(node.get_value("flat")));
    }

    // --- LinkButton ---
    if (auto* lbtn = Object::cast_to<LinkButton>(ctrl)) {
        if (const auto* p = node.find_property("text"))
            lbtn->set_text(String(resolve_text(*p).c_str()));
    }

    // --- LineEdit ---
    if (auto* le = Object::cast_to<LineEdit>(ctrl)) {
        if (const auto* p = node.find_property("text"))
            le->set_text(String(resolve_text(*p).c_str()));
        if (const auto* p = node.find_property("placeholderText"))
            le->set_placeholder(String(resolve_text(*p).c_str()));
        if (node.has_property("editable"))
            le->set_editable(parse_bool(node.get_value("editable"), true));
    }

    // --- TextEdit ---
    if (auto* te = Object::cast_to<TextEdit>(ctrl)) {
        if (const auto* p = node.find_property("text"))
            te->set_text(String(resolve_text(*p).c_str()));
    }

    // --- TextureRect ---
    if (auto* tr = Object::cast_to<TextureRect>(ctrl)) {
        if (node.has_property("src")) {
            const auto path = resolve_asset_path(node.get_value("src"));
            { auto tex = load_texture(path); if (tex.is_valid()) tr->set_texture(tex); }
        }
        if (node.has_property("shrinkH") || node.has_property("shrinkV")) {
            tr->set_expand_mode(TextureRect::EXPAND_IGNORE_SIZE);
            tr->set_stretch_mode(TextureRect::STRETCH_KEEP_ASPECT_CENTERED);
        }
    }

    // --- TextureButton ---
    if (auto* tb = Object::cast_to<TextureButton>(ctrl)) {
        if (node.has_property("textureNormal")) {
            const auto path = resolve_asset_path(node.get_value("textureNormal"));
            { auto tex = load_texture(path); if (tex.is_valid()) tb->set_texture_normal(tex); }
        }
        if (node.has_property("texturePressed")) {
            const auto path = resolve_asset_path(node.get_value("texturePressed"));
            { auto tex = load_texture(path); if (tex.is_valid()) tb->set_texture_pressed(tex); }
        }
        if (node.has_property("textureHover")) {
            const auto path = resolve_asset_path(node.get_value("textureHover"));
            { auto tex = load_texture(path); if (tex.is_valid()) tb->set_texture_hover(tex); }
        }
        if (node.has_property("toggleMode"))
            tb->set_toggle_mode(parse_bool(node.get_value("toggleMode")));
        if (node.has_property("buttonPressed"))
            tb->set_pressed(parse_bool(node.get_value("buttonPressed")));
        if (node.has_property("ignoreTextureSize"))
            tb->set_ignore_texture_size(parse_bool(node.get_value("ignoreTextureSize")));
        tb->set_stretch_mode(TextureButton::STRETCH_KEEP_ASPECT_CENTERED);
    }

    // --- OptionButton ---
    if (auto* ob = Object::cast_to<OptionButton>(ctrl)) {
        if (node.has_property("fitToLongestItem"))
            ob->set_fit_to_longest_item(parse_bool(node.get_value("fitToLongestItem"), true));
    }

    // --- ProgressBar ---
    if (auto* pb = Object::cast_to<ProgressBar>(ctrl)) {
        if (node.has_property("min"))   pb->set_min(parse_float(node.get_value("min")));
        if (node.has_property("max"))   pb->set_max(parse_float(node.get_value("max")));
        if (node.has_property("value")) pb->set_value(parse_float(node.get_value("value")));
        if (node.has_property("showPercentage"))
            pb->set_show_percentage(parse_bool(node.get_value("showPercentage"), true));
    }

    // --- Tree ---
    if (auto* tree = Object::cast_to<Tree>(ctrl)) {
        if (node.has_property("hideRoot"))
            tree->set_hide_root(parse_bool(node.get_value("hideRoot")));
    }

    // --- TabContainer ---
    if (auto* tc = Object::cast_to<TabContainer>(ctrl)) {
        (void)tc; // children add themselves via add_child; titles are node names
    }

    // --- DockingHost ---
    if (nl == "dockinghost") {
        ctrl->set_h_size_flags(Control::SIZE_EXPAND_FILL);
        ctrl->set_v_size_flags(Control::SIZE_EXPAND_FILL);
        if (node.has_property("gap") && ctrl->has_method("set_gap"))
            ctrl->call("set_gap", parse_float(node.get_value("gap")));
    }

    // --- DockingContainer ---
    if (nl == "dockingcontainer") {
        const auto side = node.get_value("dockSide", "center");
        if (ctrl->has_method("set_dock_side"))
            ctrl->call("set_dock_side", String(side.c_str()));
        if (node.has_property("fixedWidth") && ctrl->has_method("set_fixed_width"))
            ctrl->call("set_fixed_width", (double)parse_int(node.get_value("fixedWidth")));
        if (node.has_property("fixedHeight") && ctrl->has_method("set_fixed_height"))
            ctrl->call("set_fixed_height", (double)parse_int(node.get_value("fixedHeight")));
        if (node.has_property("heightPercent") && ctrl->has_method("set_height_percent"))
            ctrl->call("set_height_percent", (double)parse_float(node.get_value("heightPercent")));
        if (ctrl->has_method("set_flex"))
            ctrl->call("set_flex", parse_bool(node.get_value("flex", "false")));
        if (side == "center") {
            ctrl->set_h_size_flags(Control::SIZE_EXPAND_FILL);
            ctrl->set_v_size_flags(Control::SIZE_EXPAND_FILL);
        } else {
            ctrl->set_h_size_flags(Control::SIZE_SHRINK_BEGIN);
            ctrl->set_v_size_flags(Control::SIZE_EXPAND_FILL);
        }
        if (node.has_property("fixedWidth")) {
            float w = static_cast<float>(parse_int(node.get_value("fixedWidth")));
            ctrl->set_custom_minimum_size(Vector2(w, ctrl->get_custom_minimum_size().y));
        }
        if (auto* tc = Object::cast_to<TabContainer>(ctrl)) {
            if (node.has_property("dragToRearrangeEnabled"))
                tc->set_drag_to_rearrange_enabled(parse_bool(node.get_value("dragToRearrangeEnabled")));
            if (node.has_property("tabsRearrangeGroup"))
                tc->set_tabs_rearrange_group(parse_int(node.get_value("tabsRearrangeGroup")));
        }
    }

    // --- Styling: color (font_color) ---
    if (node.has_property("color")) {
        float r, g, b, a = 1.0f;
        if (parse_color(node.get_value("color"), r, g, b, a)) {
            Color c(r, g, b, a);
            ctrl->add_theme_color_override("font_color", c);
            ctrl->add_theme_color_override("font_uneditable_color", c);
        }
    }

    // --- Styling: fontSize ---
    if (node.has_property("fontSize"))
        ctrl->add_theme_font_size_override("font_size", parse_int(node.get_value("fontSize")));

    // --- Styling: fontWeight ---
    if (node.has_property("fontWeight") && node.get_value("fontWeight") == "bold") {
        Ref<SystemFont> font;
        font.instantiate();
        font->set_font_weight(700);
        ctrl->add_theme_font_override("font", font);
    }

    // --- shrinkH / shrinkV (generic controls) ---
    if (node.has_property("shrinkH") && !Object::cast_to<TextureRect>(ctrl))
        ctrl->set_h_size_flags(Control::SIZE_SHRINK_CENTER);
    if (node.has_property("shrinkV") && !Object::cast_to<TextureRect>(ctrl))
        ctrl->set_v_size_flags(Control::SIZE_SHRINK_CENTER);

    // --- Generic Godot property fallback via schema_properties.h ---
    // For properties not handled by specific cases above, attempt ctrl->set()
    // using the Godot property name from the generated schema.
    {
        std::string type_name = ctrl->get_class().utf8().get_data();
        for (const auto& def : kSchemaProperties) {
            if (def.type_name != type_name) continue;
            const std::string sml_key(def.sml_name);
            if (!node.has_property(sml_key)) continue;
            // Skip properties already handled by specific code above.
            static constexpr const char* kHandled[] = {
                "text", "disabled", "toggleMode", "buttonPressed", "flat",
                "placeholderText", "editable", "wrap", "align", "fitContent",
                "src", "shrinkH", "shrinkV", "textureNormal", "texturePressed",
                "textureHover", "ignoreTextureSize", "fitToLongestItem",
                "min", "max", "value", "showPercentage", "hideRoot",
                "visible", "id", "spacing", "padding", "paddingLeft",
                "paddingTop", "paddingRight", "paddingBottom", "columns",
                "sizeFlagsHorizontal", "sizeFlagsVertical", "mouseFilter",
                "customMinimumSize", "fixedWidth", "fixedHeight", "width", "height",
                "anchorLeft", "anchorRight", "anchorTop", "anchorBottom", "anchors",
                "offsetLeft", "offsetTop", "offsetRight", "offsetBottom",
                "top", "left", "right", "bottom",
                "color", "fontSize", "fontWeight",
                "bgColor", "borderRadius", "borderColor", "borderWidth",
                "borderTop", "borderBottom", "borderLeft", "borderRight", "elevation",
                nullptr
            };
            bool already_handled = false;
            for (int i = 0; kHandled[i] != nullptr; ++i) {
                if (sml_key == kHandled[i]) { already_handled = true; break; }
            }
            if (already_handled) continue;
            const std::string godot_name(def.godot_name);
            ctrl->set(String(godot_name.c_str()), String(node.get_value(sml_key).c_str()));
        }
    }

    // --- Styling: bgColor / border / elevation ---
    if (node.has_property("bgColor") || node.has_property("borderRadius") ||
        node.has_property("borderColor") || node.has_property("borderWidth") ||
        node.has_property("borderTop") || node.has_property("borderBottom") ||
        node.has_property("borderLeft") || node.has_property("borderRight") ||
        node.has_property("elevation")) {
        Ref<StyleBoxFlat> style;
        style.instantiate();

        // Elevation baseline colors (dark theme defaults)
        if (node.has_property("elevation")) {
            const auto ev = node.get_value("elevation");
            if (ev == "raised")
                style->set_bg_color(Color(0.10f, 0.12f, 0.19f, 1.0f));
            else if (ev == "elevated")
                style->set_bg_color(Color(0.14f, 0.16f, 0.24f, 1.0f));
        }

        if (node.has_property("bgColor")) {
            float r, g, b, a = 1.0f;
            if (parse_color(node.get_value("bgColor"), r, g, b, a))
                style->set_bg_color(Color(r, g, b, a));
        }
        if (node.has_property("borderRadius")) {
            int rad = parse_int(node.get_value("borderRadius"));
            style->set_corner_radius_all(rad);
        }
        if (node.has_property("borderColor")) {
            float r, g, b, a = 1.0f;
            if (parse_color(node.get_value("borderColor"), r, g, b, a))
                style->set_border_color(Color(r, g, b, a));
        }
        if (node.has_property("borderWidth")) {
            int w = parse_int(node.get_value("borderWidth"));
            style->set_border_width_all(w);
        }
        if (node.has_property("borderTop"))
            style->set_border_width(SIDE_TOP,    parse_int(node.get_value("borderTop")));
        if (node.has_property("borderBottom"))
            style->set_border_width(SIDE_BOTTOM, parse_int(node.get_value("borderBottom")));
        if (node.has_property("borderLeft"))
            style->set_border_width(SIDE_LEFT,   parse_int(node.get_value("borderLeft")));
        if (node.has_property("borderRight"))
            style->set_border_width(SIDE_RIGHT,  parse_int(node.get_value("borderRight")));
        ctrl->add_theme_stylebox_override("panel", style);
        ctrl->add_theme_stylebox_override("normal", style);
    }
}

// ---------------------------------------------------------------------------
// MenuBar
// ---------------------------------------------------------------------------

void UiBuilder::build_menubar_children(Control* menu_bar, const smlcore::Node& node) {
    auto* mb = Object::cast_to<MenuBar>(menu_bar);
    if (!mb) return;

    if (node.has_property("preferGlobalMenu") && parse_bool(node.get_value("preferGlobalMenu")))
        mb->set_prefer_global_menu(true);

    for (const auto& child : node.children) {
        std::string cnl = child.name;
        std::transform(cnl.begin(), cnl.end(), cnl.begin(),
                       [](unsigned char c){ return std::tolower(c); });
        if (cnl != "popupmenu") continue;

        auto* popup = memnew(PopupMenu);
        const auto title = child.get_value("title", child.name);
        popup->set_name(String(title.c_str()));

        int item_id = 1;
        for (const auto& item : child.children) {
            std::string inl = item.name;
            std::transform(inl.begin(), inl.end(), inl.begin(),
                           [](unsigned char c){ return std::tolower(c); });
            if (inl != "item") continue;

            const auto* tp = item.find_property("text");
            std::string text = tp ? resolve_text(*tp) : "Item";
            popup->add_item(String(text.c_str()), item_id++);
        }
        mb->add_child(popup);
    }
}

// ---------------------------------------------------------------------------
// Text / asset resolution
// ---------------------------------------------------------------------------

std::string UiBuilder::resolve_text(const smlcore::Property& prop) const {
    const auto& v = prop.value;

    // Tuple: "@Strings.key, \"Fallback\"" — try the ref first, use fallback if not found
    if (prop.kind == smlcore::ValueKind::Tuple) {
        const auto comma = v.find(',');
        const auto ref   = (comma != std::string::npos) ? v.substr(0, comma) : v;
        // trim whitespace from ref
        std::string key_part = ref;
        key_part.erase(0, key_part.find_first_not_of(" \t\""));
        key_part.erase(key_part.find_last_not_of(" \t\"") + 1);

        std::string resolved;
        if (!key_part.empty() && key_part[0] == '@') {
            const auto dot = key_part.find('.', 1);
            if (dot != std::string::npos) {
                const auto key = key_part.substr(dot + 1);
                auto it = strings_.find(key);
                if (it != strings_.end()) resolved = it->second;
            }
        }
        if (!resolved.empty()) return resolved;

        // Use fallback (second tuple element, strip quotes)
        if (comma != std::string::npos) {
            std::string fallback = v.substr(comma + 1);
            fallback.erase(0, fallback.find_first_not_of(" \t\""));
            fallback.erase(fallback.find_last_not_of(" \t\"") + 1);
            if (!fallback.empty()) return fallback;
        }
        return key_part;
    }

    // Single string reference: @Namespace.key
    if (prop.kind == smlcore::ValueKind::Identifier && !v.empty() && v[0] == '@') {
        const auto dot = v.find('.', 1);
        if (dot != std::string::npos) {
            const auto key = v.substr(dot + 1);
            auto it = strings_.find(key);
            if (it != strings_.end()) return it->second;
            return key;
        }
        return v.substr(1);
    }
    return v;
}

std::string UiBuilder::resolve_asset_path(const std::string& raw) const {
    if (raw.empty()) return raw;
    // res:// or res:/ (Godot project-relative, maps to base_dir)
    if (raw.size() > 6 && raw.substr(0, 6) == "res://") {
        return base_dir_ + "/" + raw.substr(6);
    }
    if (raw.size() > 5 && raw.substr(0, 5) == "res:/") {
        return base_dir_ + "/" + raw.substr(5);
    }

    // appRes:/ — single-slash is the only valid form; appRes:// is rejected
    if (raw.size() > 9 && raw.substr(0, 9) == "appRes://") {
        return {}; // invalid: use appRes:/ (single slash)
    }
    if (raw.size() > 8 && raw.substr(0, 8) == "appRes:/") {
        const auto tail = raw.substr(8);
        return appres_root_.empty() ? (base_dir_ + "/" + tail)
                                    : (appres_root_ + "/" + tail);
    }
    // file://
    if (raw.size() > 7 && raw.substr(0, 7) == "file://") {
        auto path = raw.substr(7);
        if (path.size() >= 9 && path.substr(0, 9) == "localhost") path = path.substr(9);
        if (!path.empty() && path[0] != '/') path = "/" + path;
        return path;
    }
    // Absolute path
    if (!raw.empty() && raw[0] == '/') return raw;
    // Relative
    return base_dir_ + "/" + raw;
}


// ---------------------------------------------------------------------------
// Parsing helpers
// ---------------------------------------------------------------------------

bool UiBuilder::parse_bool(const std::string& v, bool fallback) {
    if (v == "true" || v == "1")  return true;
    if (v == "false" || v == "0") return false;
    return fallback;
}

int UiBuilder::parse_int(const std::string& v, int fallback) {
    if (v.empty()) return fallback;
    try { return std::stoi(v); } catch (...) { return fallback; }
}

float UiBuilder::parse_float(const std::string& v, float fallback) {
    if (v.empty()) return fallback;
    try { return std::stof(v); } catch (...) { return fallback; }
}

bool UiBuilder::parse_color(const std::string& v, float& r, float& g, float& b, float& a) {
    if (v.empty()) return false;
    // Godot Color::from_string can handle named colors and hex
    Color c = Color::from_string(String(v.c_str()), Color(1, 1, 1, 1));
    r = c.r; g = c.g; b = c.b; a = c.a;
    return true;
}

int UiBuilder::parse_size_flags(const std::string& v, int fallback) {
    // Accept numeric
    if (!v.empty() && (std::isdigit(static_cast<unsigned char>(v[0])) || v[0] == '-')) {
        try { return std::stoi(v); } catch (...) { return fallback; }
    }
    std::string vl = v;
    std::transform(vl.begin(), vl.end(), vl.begin(),
                   [](unsigned char c){ return std::tolower(c); });
    if (vl == "fill")         return Control::SIZE_FILL;
    if (vl == "expand")       return Control::SIZE_EXPAND;
    if (vl == "expandfill")   return Control::SIZE_EXPAND_FILL;
    if (vl == "shrinkcenter") return Control::SIZE_SHRINK_CENTER;
    if (vl == "shrinkbegin")  return Control::SIZE_FILL;
    if (vl == "shrinkend")    return Control::SIZE_SHRINK_END;
    return fallback;
}

} // namespace forge
