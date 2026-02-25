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
                }
            ],
            "VBoxContainer": [
                {
                    "sml": "spacing",
                    "godot": "separation (theme override)",
                    "type": "int",
                    "default": "—",
                    "description": "Gap between child elements in pixels."
                }
            ],
            "HBoxContainer": [
                {
                    "sml": "spacing",
                    "godot": "separation (theme override)",
                    "type": "int",
                    "default": "—",
                    "description": "Gap between child elements in pixels."
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
