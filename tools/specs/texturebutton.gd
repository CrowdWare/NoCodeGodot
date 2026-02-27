extends RefCounted

func get_spec() -> Dictionary:
    return {
        "name": "TextureButton",
        "backing": "TextureButton",
        "notes": [
            "A button that displays textures for each interaction state instead of a text label.",
            "All texture properties accept the same path formats as TextureRect: res://, user://, appRes://, file://, or absolute paths.",
        ],
        "properties": [
            {"sml":"id",               "type":"identifier",   "default":"—"},
            {"sml":"textureNormal",    "type":"string(url)",  "default":"\"\"",
             "notes":"Texture shown in the default (idle) state."},
            {"sml":"textureHover",     "type":"string(url)",  "default":"\"\"",
             "notes":"Texture shown when the mouse hovers over the button. Alias: textureHovered."},
            {"sml":"texturePressed",   "type":"string(url)",  "default":"\"\"",
             "notes":"Texture shown while the button is held down."},
            {"sml":"textureDisabled",  "type":"string(url)",  "default":"\"\"",
             "notes":"Texture shown when the button is disabled."},
            {"sml":"textureFocused",   "type":"string(url)",  "default":"\"\"",
             "notes":"Texture shown when the button has keyboard focus."},
            {"sml":"disabled",         "type":"bool",         "default":"false"},
            {"sml":"toggleMode",       "type":"bool",         "default":"false"},
        ],
        "examples_sml": [
            "TextureButton {",
            "    id: myBtn",
            "    textureNormal:  \"appRes://assets/icons/bell.png\"",
            "    textureHover:   \"appRes://assets/icons/bell_hover.png\"",
            "    texturePressed: \"appRes://assets/icons/bell_pressed.png\"",
            "}",
        ],
    }
