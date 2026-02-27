extends RefCounted

func get_spec() -> Dictionary:
    return {
        "name": "runtime_overrides",
        "propertiesByType": {
            "Control": [
                {
                    "sml": "mouseFilter",
                    "godot": "mouse_filter",
                    "type": "enum",
                    "default": "stop",
                    "description": "Mouse event filtering mode. Use enum values: stop, pass, ignore (without quotes)."
                },
                {
                    "sml": "shrinkH",
                    "godot": "size_flags_horizontal",
                    "type": "bool",
                    "default": "false",
                    "description": "Prevents horizontal expansion beyond explicit width. true = ShrinkBegin, false = Fill."
                },
                {
                    "sml": "shrinkV",
                    "godot": "size_flags_vertical",
                    "type": "bool",
                    "default": "false",
                    "description": "Prevents vertical expansion beyond explicit height. true = ShrinkBegin, false = Fill."
                },
                {
                    "sml": "bgColor",
                    "godot": "— (drawn via BgControl._Draw)",
                    "type": "color",
                    "default": "—",
                    "description": "Background color. Quoted string: \"#RRGGBB\" or \"#AARRGGBB\"."
                },
                {
                    "sml": "borderColor",
                    "godot": "— (drawn via BgControl._Draw)",
                    "type": "color",
                    "default": "—",
                    "description": "Border color. Quoted string: \"#RRGGBB\" or \"#AARRGGBB\"."
                },
                {
                    "sml": "borderWidth",
                    "godot": "— (drawn via BgControl._Draw)",
                    "type": "int",
                    "default": "—",
                    "description": "Border width in pixels (all sides)."
                },
                {
                    "sml": "borderRadius",
                    "godot": "— (drawn via BgControl._Draw)",
                    "type": "int",
                    "default": "—",
                    "description": "Corner radius in pixels (all corners)."
                },
                {
                    "sml": "borderTop",
                    "godot": "StyleBoxFlat.border_width_top",
                    "type": "int",
                    "default": "—",
                    "description": "Top border width in pixels. Can be combined with other border side properties."
                },
                {
                    "sml": "borderBottom",
                    "godot": "StyleBoxFlat.border_width_bottom",
                    "type": "int",
                    "default": "—",
                    "description": "Bottom border width in pixels."
                },
                {
                    "sml": "borderLeft",
                    "godot": "StyleBoxFlat.border_width_left",
                    "type": "int",
                    "default": "—",
                    "description": "Left border width in pixels."
                },
                {
                    "sml": "borderRight",
                    "godot": "StyleBoxFlat.border_width_right",
                    "type": "int",
                    "default": "—",
                    "description": "Right border width in pixels."
                },
                {
                    "sml": "shadowColor",
                    "godot": "StyleBoxFlat.shadow_color",
                    "type": "color",
                    "default": "—",
                    "description": "Outer drop shadow color. Quoted string: \"#AARRGGBB\"."
                },
                {
                    "sml": "shadowSize",
                    "godot": "StyleBoxFlat.shadow_size",
                    "type": "int",
                    "default": "—",
                    "description": "Outer drop shadow blur radius in pixels."
                },
                {
                    "sml": "shadowOffsetX",
                    "godot": "StyleBoxFlat.shadow_offset.x",
                    "type": "int",
                    "default": "0",
                    "description": "Horizontal shadow offset in pixels."
                },
                {
                    "sml": "shadowOffsetY",
                    "godot": "StyleBoxFlat.shadow_offset.y",
                    "type": "int",
                    "default": "0",
                    "description": "Vertical shadow offset in pixels."
                },
                {
                    "sml": "highlightColor",
                    "godot": "— (drawn via BgControl._Draw)",
                    "type": "color",
                    "default": "—",
                    "description": "Inner bevel highlight — thin lines drawn along top and left edges. Quoted string: \"#AARRGGBB\"."
                },
                {
                    "sml": "elevation",
                    "godot": "— (expands to profile properties before build)",
                    "type": "identifier",
                    "default": "—",
                    "description": "Named elevation profile from the Elevations theme block (e.g. raised, card, inset, flat). Expands to bgColor, borderColor, shadowColor, etc. Individual properties override the profile."
                },
                {
                    "sml": "padding",
                    "godot": "— (inner MarginContainer injected at build time)",
                    "type": "padding",
                    "default": "0",
                    "description": "Inner padding — space between the container border and its children. Use 1 (all sides), 2 (top/bottom left/right), or 4 (top right bottom left) integer values. On VBoxContainer/HBoxContainer, an inner layout container preserves child stacking order."
                }
            ],
            "Label": [
                {
                    "sml": "color",
                    "godot": "font_color (theme override)",
                    "type": "color",
                    "default": "—",
                    "description": "Font color. Quoted string: \"#RRGGBB\" or \"#AARRGGBB\"."
                },
                {
                    "sml": "fontSize",
                    "godot": "font_size (theme override)",
                    "type": "int",
                    "default": "—",
                    "description": "Font size in pixels. Alias: fontSizePx."
                },
                {
                    "sml": "font",
                    "godot": "font (theme override)",
                    "type": "string(path)",
                    "default": "—",
                    "description": "Path to a .ttf/.otf font file. Alias: fontSource."
                },
                {
                    "sml": "fontFace",
                    "godot": "font (theme override, via Fonts resource block)",
                    "type": "string",
                    "default": "—",
                    "description": "Font family name. Looks up 'Face-Weight' key in the Fonts resource block. Combine with fontWeight."
                },
                {
                    "sml": "fontWeight",
                    "godot": "font (theme override, via Fonts resource block)",
                    "type": "identifier or int",
                    "default": "regular",
                    "description": "Font weight for fontFace. Named aliases (unquoted): thin, extraLight, light, regular, medium, semiBold, bold, extraBold, black. Numeric: 100–900."
                },
                {
                    "sml": "bgColor",
                    "godot": "normal StyleBoxFlat (theme override)",
                    "type": "color",
                    "default": "—",
                    "description": "Background color. Quoted string: \"#RRGGBB\" or \"#AARRGGBB\"."
                },
                {
                    "sml": "borderColor",
                    "godot": "normal StyleBoxFlat (theme override)",
                    "type": "color",
                    "default": "—",
                    "description": "Border color. Quoted string: \"#RRGGBB\" or \"#AARRGGBB\"."
                },
                {
                    "sml": "borderWidth",
                    "godot": "normal StyleBoxFlat (theme override)",
                    "type": "int",
                    "default": "—",
                    "description": "Border width in pixels (all sides)."
                },
                {
                    "sml": "borderRadius",
                    "godot": "normal StyleBoxFlat (theme override)",
                    "type": "int",
                    "default": "—",
                    "description": "Corner radius in pixels (all corners)."
                }
            ],
            "RichTextLabel": [
                {
                    "sml": "color",
                    "godot": "default_color (theme override)",
                    "type": "color",
                    "default": "—",
                    "description": "Text color. Quoted string: \"#RRGGBB\" or \"#AARRGGBB\"."
                },
                {
                    "sml": "fontSize",
                    "godot": "normal_font_size (theme override)",
                    "type": "int",
                    "default": "—",
                    "description": "Font size in pixels. Alias: fontSizePx."
                },
                {
                    "sml": "font",
                    "godot": "normal_font (theme override)",
                    "type": "string(path)",
                    "default": "—",
                    "description": "Path to a .ttf/.otf font file. Alias: fontSource."
                },
                {
                    "sml": "fontFace",
                    "godot": "normal_font (theme override, via Fonts resource block)",
                    "type": "string",
                    "default": "—",
                    "description": "Font family name. Looks up 'Face-Weight' key in the Fonts resource block. Combine with fontWeight."
                },
                {
                    "sml": "fontWeight",
                    "godot": "normal_font (theme override, via Fonts resource block)",
                    "type": "identifier or int",
                    "default": "regular",
                    "description": "Font weight for fontFace. Named aliases (unquoted): thin, extraLight, light, regular, medium, semiBold, bold, extraBold, black. Numeric: 100–900."
                }
            ],
            "Button": [
                {
                    "sml": "color",
                    "godot": "font_color (theme override)",
                    "type": "color",
                    "default": "—",
                    "description": "Font color. Also applies to OptionButton and MenuButton (subclasses). Quoted string: \"#RRGGBB\" or \"#AARRGGBB\"."
                },
                {
                    "sml": "fontSize",
                    "godot": "font_size (theme override)",
                    "type": "int",
                    "default": "—",
                    "description": "Font size in pixels. Also applies to OptionButton and MenuButton. Alias: fontSizePx."
                },
                {
                    "sml": "font",
                    "godot": "font (theme override)",
                    "type": "string(path)",
                    "default": "—",
                    "description": "Path to a .ttf/.otf font file. Also applies to OptionButton and MenuButton. Alias: fontSource."
                },
                {
                    "sml": "fontFace",
                    "godot": "font (theme override, via Fonts resource block)",
                    "type": "string",
                    "default": "—",
                    "description": "Font family name. Also applies to OptionButton and MenuButton. Looks up 'Face-Weight' key in the Fonts resource block."
                },
                {
                    "sml": "fontWeight",
                    "godot": "font (theme override, via Fonts resource block)",
                    "type": "identifier or int",
                    "default": "regular",
                    "description": "Font weight for fontFace. Named aliases (unquoted): thin, extraLight, light, regular, medium, semiBold, bold, extraBold, black. Numeric: 100–900."
                },
                {
                    "sml": "bgColor",
                    "godot": "normal StyleBoxFlat (theme override)",
                    "type": "color",
                    "default": "—",
                    "description": "Background color (normal state). Quoted string: \"#RRGGBB\" or \"#AARRGGBB\"."
                },
                {
                    "sml": "borderColor",
                    "godot": "normal StyleBoxFlat (theme override)",
                    "type": "color",
                    "default": "—",
                    "description": "Border color. Quoted string: \"#RRGGBB\" or \"#AARRGGBB\"."
                },
                {
                    "sml": "borderWidth",
                    "godot": "normal StyleBoxFlat (theme override)",
                    "type": "int",
                    "default": "—",
                    "description": "Border width in pixels (all sides)."
                },
                {
                    "sml": "borderRadius",
                    "godot": "normal StyleBoxFlat (theme override)",
                    "type": "int",
                    "default": "—",
                    "description": "Corner radius in pixels (all corners)."
                }
            ],
            "LineEdit": [
                {
                    "sml": "color",
                    "godot": "font_color (theme override)",
                    "type": "color",
                    "default": "—",
                    "description": "Font color. Quoted string: \"#RRGGBB\" or \"#AARRGGBB\"."
                },
                {
                    "sml": "fontSize",
                    "godot": "font_size (theme override)",
                    "type": "int",
                    "default": "—",
                    "description": "Font size in pixels. Alias: fontSizePx."
                },
                {
                    "sml": "font",
                    "godot": "font (theme override)",
                    "type": "string(path)",
                    "default": "—",
                    "description": "Path to a .ttf/.otf font file. Alias: fontSource."
                },
                {
                    "sml": "fontFace",
                    "godot": "font (theme override, via Fonts resource block)",
                    "type": "string",
                    "default": "—",
                    "description": "Font family name. Looks up 'Face-Weight' key in the Fonts resource block. Combine with fontWeight."
                },
                {
                    "sml": "fontWeight",
                    "godot": "font (theme override, via Fonts resource block)",
                    "type": "identifier or int",
                    "default": "regular",
                    "description": "Font weight for fontFace. Named aliases (unquoted): thin, extraLight, light, regular, medium, semiBold, bold, extraBold, black. Numeric: 100–900."
                },
                {
                    "sml": "bgColor",
                    "godot": "normal StyleBoxFlat (theme override)",
                    "type": "color",
                    "default": "—",
                    "description": "Background color. Quoted string: \"#RRGGBB\" or \"#AARRGGBB\"."
                },
                {
                    "sml": "borderColor",
                    "godot": "normal StyleBoxFlat (theme override)",
                    "type": "color",
                    "default": "—",
                    "description": "Border color. Quoted string: \"#RRGGBB\" or \"#AARRGGBB\"."
                },
                {
                    "sml": "borderWidth",
                    "godot": "normal StyleBoxFlat (theme override)",
                    "type": "int",
                    "default": "—",
                    "description": "Border width in pixels (all sides)."
                },
                {
                    "sml": "borderRadius",
                    "godot": "normal StyleBoxFlat (theme override)",
                    "type": "int",
                    "default": "—",
                    "description": "Corner radius in pixels (all corners)."
                }
            ],
            "TextEdit": [
                {
                    "sml": "color",
                    "godot": "font_color (theme override)",
                    "type": "color",
                    "default": "—",
                    "description": "Font color. Also applies to CodeEdit (subclass). Quoted string: \"#RRGGBB\" or \"#AARRGGBB\"."
                },
                {
                    "sml": "fontSize",
                    "godot": "font_size (theme override)",
                    "type": "int",
                    "default": "—",
                    "description": "Font size in pixels. Also applies to CodeEdit. Alias: fontSizePx."
                },
                {
                    "sml": "font",
                    "godot": "font (theme override)",
                    "type": "string(path)",
                    "default": "—",
                    "description": "Path to a .ttf/.otf font file. Also applies to CodeEdit. Alias: fontSource."
                },
                {
                    "sml": "fontFace",
                    "godot": "font (theme override, via Fonts resource block)",
                    "type": "string",
                    "default": "—",
                    "description": "Font family name. Also applies to CodeEdit. Looks up 'Face-Weight' key in the Fonts resource block."
                },
                {
                    "sml": "fontWeight",
                    "godot": "font (theme override, via Fonts resource block)",
                    "type": "identifier or int",
                    "default": "regular",
                    "description": "Font weight for fontFace. Named aliases (unquoted): thin, extraLight, light, regular, medium, semiBold, bold, extraBold, black. Numeric: 100–900."
                },
                {
                    "sml": "bgColor",
                    "godot": "normal/read_only/focus StyleBoxFlat (theme override)",
                    "type": "color",
                    "default": "—",
                    "description": "Background color (all states). Also applies to CodeEdit. Quoted string: \"#RRGGBB\" or \"#AARRGGBB\"."
                },
                {
                    "sml": "borderColor",
                    "godot": "normal StyleBoxFlat (theme override)",
                    "type": "color",
                    "default": "—",
                    "description": "Border color. Quoted string: \"#RRGGBB\" or \"#AARRGGBB\"."
                },
                {
                    "sml": "borderWidth",
                    "godot": "normal StyleBoxFlat (theme override)",
                    "type": "int",
                    "default": "—",
                    "description": "Border width in pixels (all sides)."
                },
                {
                    "sml": "borderRadius",
                    "godot": "normal StyleBoxFlat (theme override)",
                    "type": "int",
                    "default": "—",
                    "description": "Corner radius in pixels (all corners)."
                }
            ],
            "PanelContainer": [
                {
                    "sml": "bgColor",
                    "godot": "panel StyleBoxFlat (theme override)",
                    "type": "color",
                    "default": "—",
                    "description": "Background color. Quoted string: \"#RRGGBB\" or \"#AARRGGBB\"."
                },
                {
                    "sml": "borderColor",
                    "godot": "panel StyleBoxFlat (theme override)",
                    "type": "color",
                    "default": "—",
                    "description": "Border color. Quoted string: \"#RRGGBB\" or \"#AARRGGBB\"."
                },
                {
                    "sml": "borderWidth",
                    "godot": "panel StyleBoxFlat (theme override)",
                    "type": "int",
                    "default": "—",
                    "description": "Border width in pixels (all sides)."
                },
                {
                    "sml": "borderRadius",
                    "godot": "panel StyleBoxFlat (theme override)",
                    "type": "int",
                    "default": "—",
                    "description": "Corner radius in pixels (all corners)."
                }
            ],
            "Panel": [
                {
                    "sml": "bgColor",
                    "godot": "panel StyleBoxFlat (theme override)",
                    "type": "color",
                    "default": "—",
                    "description": "Background color. Quoted string: \"#RRGGBB\" or \"#AARRGGBB\"."
                },
                {
                    "sml": "borderColor",
                    "godot": "panel StyleBoxFlat (theme override)",
                    "type": "color",
                    "default": "—",
                    "description": "Border color. Quoted string: \"#RRGGBB\" or \"#AARRGGBB\"."
                },
                {
                    "sml": "borderWidth",
                    "godot": "panel StyleBoxFlat (theme override)",
                    "type": "int",
                    "default": "—",
                    "description": "Border width in pixels (all sides)."
                },
                {
                    "sml": "borderRadius",
                    "godot": "panel StyleBoxFlat (theme override)",
                    "type": "int",
                    "default": "—",
                    "description": "Corner radius in pixels (all corners)."
                },
                {
                    "sml": "shadowColor",
                    "godot": "StyleBoxFlat.shadow_color",
                    "type": "color",
                    "default": "—",
                    "description": "Outer drop shadow color. Quoted string: \"#AARRGGBB\"."
                },
                {
                    "sml": "shadowSize",
                    "godot": "StyleBoxFlat.shadow_size",
                    "type": "int",
                    "default": "—",
                    "description": "Outer drop shadow blur radius in pixels."
                },
                {
                    "sml": "shadowOffsetX",
                    "godot": "StyleBoxFlat.shadow_offset.x",
                    "type": "int",
                    "default": "0",
                    "description": "Horizontal shadow offset in pixels."
                },
                {
                    "sml": "shadowOffsetY",
                    "godot": "StyleBoxFlat.shadow_offset.y",
                    "type": "int",
                    "default": "0",
                    "description": "Vertical shadow offset in pixels."
                },
                {
                    "sml": "elevation",
                    "godot": "— (expands to profile properties before build)",
                    "type": "identifier",
                    "default": "—",
                    "description": "Named elevation profile from the Elevations theme block (e.g. raised, card, inset, flat). Note: highlightColor is not supported on PanelContainer."
                }
            ],
            "VBoxContainer": [
                {
                    "sml": "spacing",
                    "godot": "separation (theme override)",
                    "type": "int",
                    "default": "—",
                    "description": "Gap between child elements in pixels."
                },
                {
                    "sml": "bgColor",
                    "godot": "— (drawn via BgVBoxContainer._Draw)",
                    "type": "color",
                    "default": "—",
                    "description": "Background color. Quoted string: \"#RRGGBB\" or \"#AARRGGBB\"."
                },
                {
                    "sml": "borderColor",
                    "godot": "— (drawn via BgVBoxContainer._Draw)",
                    "type": "color",
                    "default": "—",
                    "description": "Border color. Quoted string: \"#RRGGBB\" or \"#AARRGGBB\"."
                },
                {
                    "sml": "borderWidth",
                    "godot": "— (drawn via BgVBoxContainer._Draw)",
                    "type": "int",
                    "default": "—",
                    "description": "Border width in pixels (all sides)."
                },
                {
                    "sml": "borderRadius",
                    "godot": "— (drawn via BgVBoxContainer._Draw)",
                    "type": "int",
                    "default": "—",
                    "description": "Corner radius in pixels (all corners)."
                },
                {
                    "sml": "shadowColor",
                    "godot": "StyleBoxFlat.shadow_color",
                    "type": "color",
                    "default": "—",
                    "description": "Outer drop shadow color. Quoted string: \"#AARRGGBB\"."
                },
                {
                    "sml": "shadowSize",
                    "godot": "StyleBoxFlat.shadow_size",
                    "type": "int",
                    "default": "—",
                    "description": "Outer drop shadow blur radius in pixels."
                },
                {
                    "sml": "shadowOffsetX",
                    "godot": "StyleBoxFlat.shadow_offset.x",
                    "type": "int",
                    "default": "0",
                    "description": "Horizontal shadow offset in pixels."
                },
                {
                    "sml": "shadowOffsetY",
                    "godot": "StyleBoxFlat.shadow_offset.y",
                    "type": "int",
                    "default": "0",
                    "description": "Vertical shadow offset in pixels."
                },
                {
                    "sml": "highlightColor",
                    "godot": "— (drawn via BgVBoxContainer._Draw)",
                    "type": "color",
                    "default": "—",
                    "description": "Inner bevel highlight — thin lines drawn along top and left edges. Quoted string: \"#AARRGGBB\"."
                },
                {
                    "sml": "elevation",
                    "godot": "— (expands to profile properties before build)",
                    "type": "identifier",
                    "default": "—",
                    "description": "Named elevation profile from the Elevations theme block (e.g. raised, card, inset, flat). Expands to bgColor, borderColor, shadowColor, etc. Individual properties override the profile."
                }
            ],
            "HBoxContainer": [
                {
                    "sml": "spacing",
                    "godot": "separation (theme override)",
                    "type": "int",
                    "default": "—",
                    "description": "Gap between child elements in pixels."
                },
                {
                    "sml": "bgColor",
                    "godot": "— (drawn via BgHBoxContainer._Draw)",
                    "type": "color",
                    "default": "—",
                    "description": "Background color. Quoted string: \"#RRGGBB\" or \"#AARRGGBB\"."
                },
                {
                    "sml": "borderColor",
                    "godot": "— (drawn via BgHBoxContainer._Draw)",
                    "type": "color",
                    "default": "—",
                    "description": "Border color. Quoted string: \"#RRGGBB\" or \"#AARRGGBB\"."
                },
                {
                    "sml": "borderWidth",
                    "godot": "— (drawn via BgHBoxContainer._Draw)",
                    "type": "int",
                    "default": "—",
                    "description": "Border width in pixels (all sides)."
                },
                {
                    "sml": "borderRadius",
                    "godot": "— (drawn via BgHBoxContainer._Draw)",
                    "type": "int",
                    "default": "—",
                    "description": "Corner radius in pixels (all corners)."
                },
                {
                    "sml": "shadowColor",
                    "godot": "StyleBoxFlat.shadow_color",
                    "type": "color",
                    "default": "—",
                    "description": "Outer drop shadow color. Quoted string: \"#AARRGGBB\"."
                },
                {
                    "sml": "shadowSize",
                    "godot": "StyleBoxFlat.shadow_size",
                    "type": "int",
                    "default": "—",
                    "description": "Outer drop shadow blur radius in pixels."
                },
                {
                    "sml": "shadowOffsetX",
                    "godot": "StyleBoxFlat.shadow_offset.x",
                    "type": "int",
                    "default": "0",
                    "description": "Horizontal shadow offset in pixels."
                },
                {
                    "sml": "shadowOffsetY",
                    "godot": "StyleBoxFlat.shadow_offset.y",
                    "type": "int",
                    "default": "0",
                    "description": "Vertical shadow offset in pixels."
                },
                {
                    "sml": "highlightColor",
                    "godot": "— (drawn via BgHBoxContainer._Draw)",
                    "type": "color",
                    "default": "—",
                    "description": "Inner bevel highlight — thin lines drawn along top and left edges. Quoted string: \"#AARRGGBB\"."
                },
                {
                    "sml": "elevation",
                    "godot": "— (expands to profile properties before build)",
                    "type": "identifier",
                    "default": "—",
                    "description": "Named elevation profile from the Elevations theme block (e.g. raised, card, inset, flat). Expands to bgColor, borderColor, shadowColor, etc. Individual properties override the profile."
                }
            ],
            "Markdown": [
                {
                    "sml": "bgColor",
                    "godot": "— (drawn via MarkdownContainer._Draw)",
                    "type": "color",
                    "default": "—",
                    "description": "Background color. Quoted string: \"#RRGGBB\" or \"#AARRGGBB\"."
                },
                {
                    "sml": "borderColor",
                    "godot": "— (drawn via MarkdownContainer._Draw)",
                    "type": "color",
                    "default": "—",
                    "description": "Border color. Quoted string: \"#RRGGBB\" or \"#AARRGGBB\"."
                },
                {
                    "sml": "borderWidth",
                    "godot": "— (drawn via MarkdownContainer._Draw)",
                    "type": "int",
                    "default": "—",
                    "description": "Border width in pixels (all sides)."
                },
                {
                    "sml": "borderRadius",
                    "godot": "— (drawn via MarkdownContainer._Draw)",
                    "type": "int",
                    "default": "—",
                    "description": "Corner radius in pixels (all corners)."
                }
            ],
            "Tree": [
                {
                    "sml": "showGuides",
                    "godot": "—",
                    "type": "bool",
                    "default": "true",
                    "description": "Runtime extension: shows/hides guide lines between tree items."
                },
                {
                    "sml": "rowHeight",
                    "godot": "—",
                    "type": "int",
                    "default": "—",
                    "description": "Runtime extension: sets tree row spacing/height override."
                },
                {
                    "sml": "indent",
                    "godot": "—",
                    "type": "int",
                    "default": "—",
                    "description": "Runtime extension: sets item indentation via theme override."
                }
            ]
        },
        "actionsByType": {
            "Window": [
                {
                    "sms": "onClose",
                    "params": [
                        { "name": "callbackName", "type": "string" }
                    ],
                    "returns": "void"
                }
            ],
            "CodeEdit": [
                {
                    "sms": "onSave",
                    "params": [
                        { "name": "callbackName", "type": "string" }
                    ],
                    "returns": "void"
                }
            ]
        }
    }
