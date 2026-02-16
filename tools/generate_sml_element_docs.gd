extends SceneTree

var REPO_ROOT := ""
var OUT_DIR := ""
var REF_PATH := ""
func _initialize() -> void:
    REPO_ROOT = ProjectSettings.globalize_path("res://") + "/.."
    OUT_DIR = REPO_ROOT + "/docs/SML/Elements"
    REF_PATH = REPO_ROOT + "/docs/sms-reference.sml"
    _run()
    quit()

const EXTRA_CLASSES := ["Window", "PopupMenu"] 
const MANUAL_TYPES: Array[String] = ["Markdown", "Viewport3D"] 

func _run() -> void:
    _ensure_out_dir()

    # Collect all Control classes AND all their base classes (CanvasItem, Node, Object, ...)
    var targets := {}

    var classes := ClassDB.get_class_list()
    classes.sort()

    for c in classes:
        var cn := String(c)
        if _is_control(cn):
            var cur: String = cn  # CHANGED
            while cur != "":
                targets[cur] = true
                cur = String(ClassDB.get_parent_class(cur))  # CHANGED

    # Add extra non-Control UI classes (e.g. Window)
    for cn in EXTRA_CLASSES:
        if ClassDB.class_exists(cn):
            var cur: String = cn  # CHANGED
            while cur != "":
                targets[cur] = true
                cur = String(ClassDB.get_parent_class(cur))  # CHANGED

    # Generate docs
    var names: Array = targets.keys()
    for mt in MANUAL_TYPES:  # CHANGED
        if not names.has(mt):
            names.append(mt)
    names.sort()

    for name in names:
        _generate_doc(String(name))

    _generate_reference_sml(names)  # CHANGED

    print("SML element docs generated.")

func _ensure_out_dir() -> void:
    DirAccess.make_dir_recursive_absolute(OUT_DIR)

func _is_control(c_name: String) -> bool:
    return c_name == "Control" or ClassDB.is_parent_class(c_name, "Control")

func _generate_doc(c_name: String) -> void:
    var inheritance := _inheritance_chain(c_name)
    var props := _collect_properties(c_name)
    var signals := _collect_signals(c_name)

    var md := "# %s\n\n" % c_name
    md += "## Inheritance\n\n"
    # Create a linked inheritance chain: Class → Parent → ... → Object
    var linked: Array[String] = []
    for cls in inheritance:
        linked.append("[%s](%s.md)" % [cls, cls])
    md += " → ".join(linked) + "\n\n"

    if c_name != "PopupMenu" and _is_collection_control(c_name):  # CHANGED
        md += "## Collection Items\n\n"  # CHANGED
        md += "This control appears to manage internal **items** (collection-style API).\n"  # CHANGED
        md += "Items are typically not represented as child nodes/properties in Godot.\n"  # CHANGED
        md += "In SML, this will be represented via **pseudo child elements** (documented per control).\n\n"  # CHANGED

    # Back-navigation: for base classes, list derived classes.
    if c_name == "Control":
        md += "## Derived Controls\n\n"
        md += "This is the entry point for all UI controls.\n"
        md += "All classes listed below inherit from `Control`.\n\n"

        md += "### Direct subclasses\n\n"
        md += _md_link_list(_collect_subclasses("Control", true))

        md += "\n### All descendants (alphabetical)\n\n"
        md += _md_link_list(_collect_subclasses("Control", false))

        md += "\n### Manual SML elements\n\n"  # CHANGED
        md += "These are SML-only elements mapped to `Control` (not Godot ClassDB classes).\n\n"  # CHANGED
        md += _md_link_list(MANUAL_TYPES)  # CHANGED

        md += "\n"

    elif c_name in ["CanvasItem", "Node", "Object"]:
        md += "## Derived Classes\n\n"
        md += "Classes listed below inherit from `" + c_name + "`.\n\n"
        md += "### Direct subclasses\n\n"
        md += _md_link_list(_collect_subclasses(c_name, true))
        md += "\n"

    else:
        var direct := _collect_subclasses(c_name, true)
        if direct.size() > 0:
            md += "## Derived Classes\n\n"
            md += "### Direct subclasses\n\n"
            md += _md_link_list(direct)
            md += "\n"

    md += "## Properties\n\n"
    md += "This page lists **only properties declared by `" + c_name + "`**.\n"
    var parent := ""  # CHANGED
    if ClassDB.class_exists(c_name):  # CHANGED
        parent = String(ClassDB.get_parent_class(c_name))  # CHANGED
    elif inheritance.size() > 1:  # CHANGED
        parent = inheritance[1]  # CHANGED
    if parent != "":
        md += "Inherited properties are documented in: [" + parent + "](" + parent + ".md)\n\n"
    else:
        md += "\n"

    md += "| Godot Property | SML Property | Type | Default |\n|-|-|-|-|\n"  # CHANGED
    # CHANGED: Manual SML-only element (not a Godot class)
    if c_name == "Markdown":
        md += "| id | id | identifier | — |\n"
        md += "| padding | padding | int / int,int / int,int,int,int | 0 |\n"
        md += "| text | text | string | \"\" |\n"
        md += "| src | src | string | \"\" |\n"
        md += "\n> Note: `text` and `src` are alternative sources. Use one of them.\n\n"

        md += "### Examples\n\n"
        md += "```sml\n"
        md += "Markdown { padding: 8,8,8,20; text: \"# Header\" }\n"
        md += "Markdown { padding: 8; src: \"res:/sample.md\" }\n"
        md += "```\n"

    elif c_name == "Viewport3D":  # CHANGED
        md += "| id | id | identifier | — |\n"
        md += "| model | model | string (url) | \"\" |\n"
        md += "| modelSource | modelSource | string (url) | \"\" |\n"
        md += "| animation | animation | string (url) | \"\" |\n"
        md += "| animationSource | animationSource | string (url) | \"\" |\n"
        md += "| playAnimation | playAnimation | int (1-based) | 0 |\n"
        md += "| playFirstAnimation | playFirstAnimation | bool | false |\n"
        md += "| autoplayAnimation | autoplayAnimation | bool | false |\n"
        md += "| defaultAnimation | defaultAnimation | string | \"\" |\n"
        md += "| playLoop | playLoop | bool | false |\n"
        md += "| cameraDistance | cameraDistance | int | 0 |\n"
        md += "| lightEnergy | lightEnergy | int | 0 |\n"

        md += "\n> Note: `model` and `modelSource` are aliases. Same for `animation` and `animationSource`.\n"
        md += "> `id` is used to target camera/animation actions from SMS.\n\n"

        md += "### Examples\n\n"
        md += "```sml\n"
        md += "Viewport3D {\n"
        md += "    id: heroView\n"
        md += "    model: \"res:/assets/models/Idle.glb\"\n"
        md += "    playFirstAnimation: true\n"
        md += "}\n"
        md += "```\n"

    else:
        for p in props:
            md += "| %s | %s | %s | — |\n" % [String(p["name"]), _normalize_property(String(p["name"])), String(p["type"])]  # CHANGED

    md += "\n## Events\n\n"
    md += "This page lists **only signals declared by `" + c_name + "`**.\n"
    if parent != "":
        md += "Inherited signals are documented in: [" + parent + "](" + parent + ".md)\n\n"
    else:
        md += "\n"

    md += "| Godot Signal | SMS Event | Params |\n|-|-|-|\n"  # unchanged header (kept)
    for s in signals:
        md += "| %s | %s | %s |\n" % [String(s["name"]), _format_sms_handler(String(s["name"]), String(s["params_names"])), String(s["params"]) ]  # CHANGED


    # SML-only child structure for menu items (PopupMenu items are not properties in Godot).
    if c_name == "PopupMenu":
        md += "\n## SML Items\n\n"
        md += "`PopupMenu` items are defined as **SML child nodes** (pseudo elements).\n"
        md += "The runtime converts them to Godot menu items internally.\n\n"

        md += "### Supported item elements\n\n"
        md += "- `Item`\n"
        md += "- `CheckItem`\n"
        md += "- `Separator`\n\n"

        md += "### Item properties (SML)\n\n"
        md += "| Property | Type | Default | Notes |\n|-|-|-|-|\n"
        md += "| id | identifier | — | Optional. Enables id-based event sugar (`on <id>.pressed() { ... }`). |\n"
        md += "| text | string | \"\" | Display text. |\n"
        md += "| checked | bool | false | Only for `CheckItem`. |\n"
        md += "| disabled | bool | false | Optional. |\n"

        md += "\n### Example\n\n"
        md += "```sml\n"
        md += "PopupMenu { id: fileMenu\n"
        md += "    Item { id: open; text: \"Open\" }\n"
        md += "    Item { id: save; text: \"Save\" }\n"
        md += "    Separator { }\n"
        md += "    CheckItem { id: autosave; text: \"Auto Save\"; checked: true }\n"
        md += "}\n"
        md += "```\n\n"

        md += "### SMS Event Examples\n\n"
        md += "```sms\n"
        md += "// With explicit item ids:\n"
        md += "on open.pressed() { ... }\n"
        md += "on autosave.pressed() { ... }\n\n"
        md += "// Without item ids (container fallback):\n"
        md += "on fileMenu.idPressed(id) { ... }\n"
        md += "```\n"

    # SML-only child structure for ItemList items (ItemList manages items internally in Godot).
    if c_name == "ItemList":
        md += "\n## SML Items\n\n"
        md += "`ItemList` entries are defined as **SML child nodes** (pseudo elements).\n"
        md += "The runtime converts them to Godot ItemList items internally.\n\n"

        md += "### Supported item elements\n\n"
        md += "- `Item`\n\n"

        md += "### Item properties (SML)\n\n"
        md += "| Property | Type | Default | Notes |\n|-|-|-|-|\n"
        md += "| id | identifier | — | Optional. Enables id-based event sugar (`on <id>.selected() { ... }`). |\n"
        md += "| text | string | \"\" | Display text. |\n"
        md += "| icon | string | \"\" | Optional icon resource/path. |\n"
        md += "| selected | bool | false | Initial selection state (single-select). |\n"
        md += "| disabled | bool | false | Disables the item. |\n"
        md += "| tooltip | string | \"\" | Optional tooltip text. |\n"

        md += "\n### Example\n\n"
        md += "```sml\n"
        md += "ItemList { id: files\n"
        md += "    Item { id: a; text: \"Readme.md\"; icon: \"res:/icons/doc.svg\" }\n"
        md += "    Item { id: b; text: \"Todo.md\" }\n"
        md += "    Item { text: \"Disabled item\"; disabled: true }\n"
        md += "}\n"
        md += "```\n\n"

        md += "### SMS Event Examples\n\n"
        md += "```sms\n"
        md += "// With explicit item ids:\n"
        md += "on a.selected() { ... }\n\n"
        md += "// Without item ids (container fallback):\n"
        md += "on files.itemSelected(index) { ... }\n"
        md += "```\n"

    # SML-only child structure for OptionButton items.
    if c_name == "OptionButton":
        md += "\n## SML Items\n\n"
        md += "`OptionButton` options are defined as **SML child nodes** (pseudo elements).\n"
        md += "The runtime converts them to Godot OptionButton items internally.\n\n"

        md += "### Supported item elements\n\n"
        md += "- `Item`\n\n"

        md += "### Item properties (SML)\n\n"
        md += "| Property | Type | Default | Notes |\n|-|-|-|-|\n"
        md += "| id | identifier | — | Optional. Enables id-based event sugar (`on <id>.selected() { ... }`). |\n"
        md += "| text | string | \"\" | Display text. |\n"
        md += "| icon | string | \"\" | Optional icon resource/path. |\n"
        md += "| disabled | bool | false | Disables the option. |\n"
        md += "| selected | bool | false | If true, selects this option initially (first wins). |\n"

        md += "\n### Example\n\n"
        md += "```sml\n"
        md += "OptionButton { id: quality\n"
        md += "    Item { id: low; text: \"Low\" }\n"
        md += "    Item { id: med; text: \"Medium\"; selected: true }\n"
        md += "    Item { id: high; text: \"High\" }\n"
        md += "}\n"
        md += "```\n\n"

        md += "### SMS Event Examples\n\n"
        md += "```sms\n"
        md += "// With explicit item ids:\n"
        md += "on med.selected() { ... }\n\n"
        md += "// Container fallback (index based):\n"
        md += "on quality.itemSelected(index) { ... }\n"
        md += "```\n"

    # SML-only child structure for TabBar tabs (tabs are internal items, not child Controls).
    if c_name == "TabBar":
        md += "\n## SML Tabs\n\n"
        md += "`TabBar` tabs are defined as **SML child nodes** (pseudo elements).\n"
        md += "The runtime converts them to Godot TabBar tabs internally.\n\n"

        md += "### Supported tab elements\n\n"
        md += "- `Tab`\n\n"

        md += "### Tab properties (SML)\n\n"
        md += "| Property | Type | Default | Notes |\n|-|-|-|-|\n"
        md += "| id | identifier | — | Optional. Enables id-based event sugar (`on <id>.tabSelected() { ... }`). |\n"
        md += "| title | string | \"\" | Tab title. |\n"
        md += "| icon | string | \"\" | Optional icon resource/path. |\n"
        md += "| disabled | bool | false | Disables selecting the tab. |\n"
        md += "| hidden | bool | false | Hides the tab. |\n"
        md += "| selected | bool | false | If true, selects this tab initially (first wins). |\n"

        md += "\n### Example\n\n"
        md += "```sml\n"
        md += "TabBar { id: tabs\n"
        md += "    Tab { id: home; title: \"Home\"; selected: true }\n"
        md += "    Tab { id: settings; title: \"Settings\" }\n"
        md += "}\n"
        md += "```\n\n"

        md += "### SMS Event Examples\n\n"
        md += "```sms\n"
        md += "// With explicit tab ids:\n"
        md += "on home.tabSelected() { ... }\n\n"
        md += "// Container fallback (index based):\n"
        md += "on tabs.tabChanged(index) { ... }\n"
        md += "```\n"

    # Context properties: valid on child Controls only when used under a specific parent.
    if c_name == "TabContainer":
        md += "\n## Child Properties (Context)\n\n"
        md += "When a `Control` is used as a **direct child** of `TabContainer`, the following additional SML properties are supported.\n"
        md += "These properties do **not** belong to the child control itself; they are interpreted by the parent `TabContainer`.\n\n"

        md += "| SML Property | Type | Default | Description |\n|-|-|-|-|\n"
        md += "| tabTitle | string | \"\" | Title of the tab for this child page. |\n"
        md += "| tabIcon | string | \"\" | Optional icon resource/path for the tab (if supported by runtime). |\n"
        md += "| tabDisabled | bool | false | Disables selecting this tab. |\n"
        md += "| tabHidden | bool | false | Hides this tab from the tab bar. |\n"

        md += "\n### Example\n\n"
        md += "```sml\n"
        md += "TabContainer { id: tabs\n"
        md += "    Panel { tabTitle: \"Home\" }\n"
        md += "    Panel { tabTitle: \"Settings\"; tabDisabled: false }\n"
        md += "}\n"
        md += "```\n"

    # Generic fallback: collection controls without a dedicated SML pseudo-child spec yet.
    if _is_collection_control(c_name) and c_name not in ["PopupMenu", "ItemList", "OptionButton", "TabBar", "TabContainer"]:
        md += "\n## SML Items (TODO)\n\n"
        md += "This control appears to manage internal items, but a dedicated SML pseudo-child specification has not been defined yet.\n"
        md += "Use the generated signals and the `collection: true` marker in `sms-reference.sml` as implementation hints.\n"

    var path := "%s/%s.md" % [OUT_DIR, c_name]
    var f := FileAccess.open(path, FileAccess.WRITE)
    if f:
        f.store_string(md)
        f.close()

func _inheritance_chain(c_name: String) -> Array[String]:
    if c_name == "Markdown":
        return ["Markdown", "Control", "CanvasItem", "Node", "Object"]
    if c_name == "Viewport3D":
        return ["Viewport3D", "Control", "CanvasItem", "Node", "Object"]

    var chain: Array[String] = []
    var c := c_name
    while c != "":
        chain.append(c)
        c = ClassDB.get_parent_class(c)
    return chain

func _collect_properties(c_name: String) -> Array:
    if not ClassDB.class_exists(c_name):  # CHANGED
        return []  # CHANGED

    var parent := String(ClassDB.get_parent_class(c_name))

    var parent_names := {}
    if parent != "":
        for d in ClassDB.class_get_property_list(parent):
            var usage := int(d.get("usage", 0))  # CHANGED
            if (usage & PROPERTY_USAGE_EDITOR) == 0:  # CHANGED
                continue
            if (usage & PROPERTY_USAGE_CATEGORY) != 0: continue  # CHANGED
            if (usage & PROPERTY_USAGE_GROUP) != 0: continue  # CHANGED
            if (usage & PROPERTY_USAGE_SUBGROUP) != 0: continue  # CHANGED
            if (usage & PROPERTY_USAGE_INTERNAL) != 0: continue  # CHANGED
            if (usage & PROPERTY_USAGE_READ_ONLY) != 0: continue  # CHANGED

            var t := int(d.get("type", TYPE_NIL))  # CHANGED
            if not _is_sml_supported_type(t):  # CHANGED
                continue

            var n := String(d.get("name", ""))
            if n != "":
                parent_names[n] = true

    var collected := {}
    for d in ClassDB.class_get_property_list(c_name):
        var usage := int(d.get("usage", 0))  # CHANGED
        if (usage & PROPERTY_USAGE_EDITOR) == 0:  # CHANGED
            continue
        if (usage & PROPERTY_USAGE_CATEGORY) != 0: continue  # CHANGED
        if (usage & PROPERTY_USAGE_GROUP) != 0: continue  # CHANGED
        if (usage & PROPERTY_USAGE_SUBGROUP) != 0: continue  # CHANGED
        if (usage & PROPERTY_USAGE_INTERNAL) != 0: continue  # CHANGED
        if (usage & PROPERTY_USAGE_READ_ONLY) != 0: continue  # CHANGED

        var t := int(d.get("type", TYPE_NIL))  # CHANGED
        if not _is_sml_supported_type(t):  # CHANGED
            continue

        var name := String(d.get("name", ""))
        if name == "" or collected.has(name):
            continue
        if parent_names.has(name):
            continue

        collected[name] = {
            "name": name,
            "type": _type_name(t)
        }

    var out: Array = []
    for k in collected.keys():
        out.append(collected[k])
    out.sort_custom(func (a, b): return String(a["name"]) < String(b["name"]))
    return out

func _collect_signals(c_name: String) -> Array:
    if not ClassDB.class_exists(c_name):  # CHANGED
        return []  # CHANGED

    var parent := String(ClassDB.get_parent_class(c_name))

    var parent_names := {}
    if parent != "":
        for d in ClassDB.class_get_signal_list(parent):
            var n := String(d.get("name", ""))
            if n != "":
                parent_names[n] = true

    var collected := {}
    for d in ClassDB.class_get_signal_list(c_name):
        var name := String(d.get("name", ""))
        if name == "" or collected.has(name):
            continue
        if parent_names.has(name):
            continue

        var args: Array = d.get("args", [])
        var parts: Array[String] = []
        var names_only: Array[String] = []
        for a in args:
            var p_name := _normalize_param(String(a.get("name","arg")), name)
            parts.append("%s %s" % [_type_name(int(a.get("type", TYPE_NIL))), p_name])  # CHANGED
            names_only.append(p_name)  # CHANGED

        collected[name] = {
            "name": name,
            "params": (", ".join(parts) if parts.size() > 0 else "—"),
            "params_names": (", ".join(names_only) if names_only.size() > 0 else "")
        }

    var out: Array = []
    for k in collected.keys():
        out.append(collected[k])
    out.sort_custom(func (a, b): return String(a["name"]) < String(b["name"]))
    return out

func _type_name(t: int) -> String:  # CHANGED
    match t:
        TYPE_BOOL: return "bool"
        TYPE_INT: return "int"
        TYPE_FLOAT: return "float"
        TYPE_STRING: return "string"
        TYPE_VECTOR2: return "Vector2"
        TYPE_VECTOR3: return "Vector3"
        TYPE_COLOR: return "Color"
        TYPE_OBJECT: return "Object"
        _: return "Variant"


# CHANGED: Only document properties that SML can represent and set.
func _is_sml_supported_type(t: int) -> bool:
    match t:
        TYPE_BOOL, TYPE_INT, TYPE_FLOAT, TYPE_STRING, TYPE_VECTOR2, TYPE_VECTOR3, TYPE_COLOR:
            return true
        _:
            return false


# Normalize a property name for SML (snake_case -> lowerCamelCase, and also lower first char).
func _normalize_property(name: String) -> String:
    return _to_lower_camel(name)

# Normalize a parameter name for SMS (snake_case -> lowerCamelCase).
# `signal_name` is the Godot signal that owns this parameter.
func _normalize_param(param_name: String, signal_name: String) -> String:
    return _to_lower_camel(param_name)  # CHANGED

# Default signal normalization (snake_case -> lowerCamelCase)
func _normalize_event(name: String) -> String:  # CHANGED
    return _to_lower_camel(name)



# Formats the full SMS handler syntax shown in docs.
# Example: Godot `button_up` -> `on <id>.buttonUp()`
func _format_sms_handler(godot_signal: String, params_names: String) -> String:
    var ev := _normalize_event(godot_signal)  # CHANGED
    if params_names == "":
        return "`on <id>." + ev + "() { ... }`"
    return "`on <id>." + ev + "(" + params_names + ") { ... }`"

# Shared lowerCamelCase conversion:
# - snake_case -> lowerCamelCase
# - PascalCase -> lowerCamelCase (e.g. Shortcut -> shortcut)
func _to_lower_camel(s: String) -> String:
    if s.find("_") != -1:
        var parts := s.split("_")
        if parts.size() == 0:
            return s
        var out := parts[0].to_lower()
        for i in range(1, parts.size()):
            out += parts[i].capitalize()
        return out

    # no underscores: just lowercase first char
    if s.length() == 0:
        return s
    return s.substr(0, 1).to_lower() + s.substr(1)


# Helper: collect subclasses of a class.
func _collect_subclasses(base: String, direct_only: bool) -> Array[String]:
    var out: Array[String] = []
    var classes := ClassDB.get_class_list()
    for c in classes:
        var cn := String(c)
        if cn == base:
            continue
        if direct_only:
            if String(ClassDB.get_parent_class(cn)) == base:
                out.append(cn)
        else:
            if ClassDB.is_parent_class(cn, base):
                out.append(cn)
    out.sort()
    return out

func _md_link_list(names: Array[String]) -> String:
    if names.size() == 0:
        return "(none)\n"
    var lines: Array[String] = []
    for n in names:
        lines.append("- [" + n + "](" + n + ".md)")
    return "\n".join(lines) + "\n"
 
# CHANGED: In SML, only these controls are modeled as item-collections (pseudo children).
# Avoid heuristic detection (it flags too many normal controls, e.g. Button).
func _is_collection_control(c_name: String) -> bool:
    match c_name:
        "PopupMenu", "ItemList", "OptionButton", "TabBar", "TabContainer", "Tree", "MenuButton":
            return true
        _:
            return false

func _has_count_property(c_name: String) -> bool:
    for d in ClassDB.class_get_property_list(c_name):
        var n := String(d.get("name", ""))
        if n == "":
            continue
        if n == "item_count" or n == "tab_count":
            return true
        if n.ends_with("_count"):
            return true
    return false

func _has_item_methods(c_name: String) -> bool:
    for m in ClassDB.class_get_method_list(c_name):
        var mn := String(m.get("name", ""))
        if mn == "":
            continue
        if mn.begins_with("add_item"):
            return true
        if mn.begins_with("insert_item"):
            return true
        if mn.begins_with("remove_item"):
            return true
        if mn.begins_with("set_item_"):
            return true
        if mn.begins_with("get_item_"):
            return true
        if mn == "create_item":
            return true
        if mn.begins_with("add_tab") or mn.begins_with("set_tab_") or mn.begins_with("get_tab_"):
            return true
    return false

func _has_item_signals(c_name: String) -> bool:
    for s in ClassDB.class_get_signal_list(c_name):
        var sn := String(s.get("name", ""))
        if sn == "":
            continue
        # Common item/index/id signals
        if sn.find("item_") != -1:
            return true
        if sn.find("tab_") != -1:
            return true
        if sn == "id_pressed" or sn == "index_pressed":
            return true

        # Or any signal that exposes an index/id argument
        var args: Array = s.get("args", [])
        for a in args:
            var an := String(a.get("name", ""))
            if an == "index" or an == "id":
                return true
    return false

# CHANGED: Codex-friendly reference schema (no Markdown tables).
func _generate_reference_sml(names: Array) -> void:
    var sml := "Reference {\n"
    sml += "    generator: \"generate_sml_element_docs.gd\"\n"
    sml += "    propertyFilter: \"inspector-editable + supported scalar types\"\n"
    sml += "    naming: \"snake_case -> lowerCamelCase\"\n"
    sml += "\n"

    for n in names:
        var c_name := String(n)
        if c_name == "Markdown":  # CHANGED
            sml += "    Type {\n"
            sml += "        name: \"Markdown\"\n"
            sml += "        parent: \"Control\"\n"
            sml += "\n        Properties {\n"
            sml += "            Prop { sml: \"id\"; type: \"identifier\" }\n"
            sml += "            Prop { sml: \"padding\"; type: \"padding\"; default: \"0\" }\n"
            sml += "            Prop { sml: \"text\"; type: \"string\"; default: \"\\\"\\\"\" }\n"
            sml += "            Prop { sml: \"src\"; type: \"string\"; default: \"\\\"\\\"\" }\n"
            sml += "        }\n"
            sml += "\n        Events {\n"
            sml += "        }\n"
            sml += "    }\n\n"
            continue

        if c_name == "Viewport3D":  # CHANGED
            sml += "    Type {\n"
            sml += "        name: \"Viewport3D\"\n"
            sml += "        parent: \"Control\"\n"
            sml += "\n        Properties {\n"
            sml += "            Prop { sml: \"id\"; type: \"identifier\" }\n"
            sml += "            Prop { sml: \"model\"; type: \"string\"; default: \"\\\"\\\"\" }\n"
            sml += "            Prop { sml: \"modelSource\"; type: \"string\"; default: \"\\\"\\\"\" }\n"
            sml += "            Prop { sml: \"animation\"; type: \"string\"; default: \"\\\"\\\"\" }\n"
            sml += "            Prop { sml: \"animationSource\"; type: \"string\"; default: \"\\\"\\\"\" }\n"
            sml += "            Prop { sml: \"playAnimation\"; type: \"int\"; default: \"0\" }\n"
            sml += "            Prop { sml: \"playFirstAnimation\"; type: \"bool\"; default: \"false\" }\n"
            sml += "            Prop { sml: \"autoplayAnimation\"; type: \"bool\"; default: \"false\" }\n"
            sml += "            Prop { sml: \"defaultAnimation\"; type: \"string\"; default: \"\\\"\\\"\" }\n"
            sml += "            Prop { sml: \"playLoop\"; type: \"bool\"; default: \"false\" }\n"
            sml += "            Prop { sml: \"cameraDistance\"; type: \"int\"; default: \"0\" }\n"
            sml += "            Prop { sml: \"lightEnergy\"; type: \"int\"; default: \"0\" }\n"
            sml += "        }\n"
            sml += "\n        Events {\n"
            sml += "        }\n"
            sml += "    }\n\n"
            continue

        var parent := String(ClassDB.get_parent_class(c_name))
        var props := _collect_properties(c_name)
        var signals := _collect_signals(c_name)

        sml += "    Type {\n"
        sml += "        name: \"%s\"\n" % c_name
        if parent != "":
            sml += "        parent: \"%s\"\n" % parent

        if _is_collection_control(c_name):
            sml += "        collection: true\n"  # CHANGED

        # Properties (local only)
        sml += "\n        Properties {\n"
        for p in props:
            sml += "            Prop { godot: \"%s\"; sml: \"%s\"; type: \"%s\" }\n" % [String(p["name"]), _normalize_property(String(p["name"])), String(p["type"]) ]
        sml += "        }\n"

        # Events (local only)
        sml += "\n        Events {\n"
        for s in signals:
            sml += "            Event { godot: \"%s\"; sms: \"%s\"; params: \"%s\" }\n" % [String(s["name"]), _normalize_event(String(s["name"])), String(s["params"]) ]
        sml += "        }\n"

        # Pseudo-children for menu items
        if c_name == "PopupMenu":
            sml += "\n        PseudoChildren {\n"
            sml += "            Item { props: \"id,text,disabled\" }\n"
            sml += "            CheckItem { props: \"id,text,checked,disabled\" }\n"
            sml += "            Separator { props: \"\" }\n"
            sml += "        }\n"
        if c_name == "ItemList":
            sml += "\n        PseudoChildren {\n"
            sml += "            Item { props: \"id,text,icon,selected,disabled,tooltip\" }\n"
            sml += "        }\n"
        if c_name == "OptionButton":
            sml += "\n        PseudoChildren {\n"
            sml += "            Item { props: \"id,text,icon,disabled,selected\" }\n"
            sml += "        }\n"
        if c_name == "TabBar":
            sml += "\n        PseudoChildren {\n"
            sml += "            Tab { props: \"id,title,icon,disabled,hidden,selected\" }\n"
            sml += "        }\n"
        # Context properties for children of TabContainer
        if c_name == "TabContainer":
            sml += "\n        ChildContext {\n"
            sml += "            # These properties are valid on TabContainer children (pages)\n"
            sml += "            Prop { sml: \"tabTitle\"; type: \"string\"; default: \"\" }\n"
            sml += "            Prop { sml: \"tabIcon\"; type: \"string\"; default: \"\" }\n"
            sml += "            Prop { sml: \"tabDisabled\"; type: \"bool\"; default: \"false\" }\n"
            sml += "            Prop { sml: \"tabHidden\"; type: \"bool\"; default: \"false\" }\n"
            sml += "        }\n"

        if _is_collection_control(c_name) and c_name not in ["PopupMenu", "ItemList", "OptionButton", "TabBar", "TabContainer"]:
            sml += "\n        # TODO: Define PseudoChildren schema for this collection control\n"

        sml += "    }\n\n"

    sml += "}\n"

    var f := FileAccess.open(REF_PATH, FileAccess.WRITE)
    if f:
        f.store_string(sml)
        f.close()