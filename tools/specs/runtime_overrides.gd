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
                }
            ],
            "Label": [
                {
                    "sml": "color",
                    "godot": "font_color (theme override)",
                    "type": "color",
                    "default": "—",
                    "description": "Font color. Hex format: #RRGGBB or #AARRGGBB."
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
                }
            ],
            "RichTextLabel": [
                {
                    "sml": "color",
                    "godot": "default_color (theme override)",
                    "type": "color",
                    "default": "—",
                    "description": "Text color. Hex format: #RRGGBB or #AARRGGBB."
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
                    "description": "Font color. Hex format: #RRGGBB or #AARRGGBB."
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
                }
            ],
            "TextEdit": [
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