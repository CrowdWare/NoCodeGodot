extends RefCounted

func get_spec() -> Dictionary:
    return {
        "name": "VBoxContainer",
        "backing": "VBoxContainer",
        "properties": [
            {"sml":"id",        "type":"identifier", "default":"—"},
            {"sml":"alignment", "type":"enum: begin, center, end", "default":"begin",
             "notes":"Child alignment along the vertical axis. Use unquoted values."},
            {"sml":"spacing",   "type":"int",        "default":"—",
             "notes":"Gap between child elements in pixels. Documented in runtime overrides."},
        ],
        "examples_sml": [
            "VBoxContainer {",
            "    alignment: center",
            "    Label { text: \"Top\" }",
            "    Label { text: \"Bottom\" }",
            "}",
        ],
    }
