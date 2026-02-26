extends RefCounted

func get_spec() -> Dictionary:
    return {
        "name": "HBoxContainer",
        "backing": "HBoxContainer",
        "properties": [
            {"sml":"id",        "type":"identifier", "default":"—"},
            {"sml":"alignment", "type":"enum: begin, center, end", "default":"begin",
             "notes":"Child alignment along the horizontal axis. Use unquoted values."},
            {"sml":"spacing",   "type":"int",        "default":"—",
             "notes":"Gap between child elements in pixels. Documented in runtime overrides."},
        ],
        "examples_sml": [
            "HBoxContainer {",
            "    alignment: center",
            "    Label { text: \"Left\" }",
            "    Label { text: \"Right\" }",
            "}",
        ],
    }
