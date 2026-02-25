extends RefCounted

func get_spec() -> Dictionary:
    return {
        "name": "TextureRect",
        "backing": "TextureRect",
        "notes": ["Displays an image or SVG. Use width/height to set a fixed size, combined with shrinkH/shrinkV to prevent the control from expanding beyond that size inside a container."],
        "properties": [
            {"sml":"id",       "type":"identifier", "default":"—"},
            {"sml":"src",      "type":"string(url)", "default":"\"\"",
             "notes":"Path to the image or SVG file (res:// or user://)"},
            {"sml":"alt",      "type":"string",     "default":"\"\"",
             "notes":"Accessibility description of the image"},
            {"sml":"width",    "type":"int",        "default":"—",
             "notes":"Sets CustomMinimumSize.X. Combine with shrinkH: true for a fixed width."},
            {"sml":"height",   "type":"int",        "default":"—",
             "notes":"Sets CustomMinimumSize.Y. Combine with shrinkV: true for a fixed height."},
            {"sml":"shrinkH",  "type":"bool",       "default":"false",
             "notes":"Prevents horizontal expansion beyond the explicit width. true = ShrinkBegin, false = Fill."},
            {"sml":"shrinkV",  "type":"bool",       "default":"false",
             "notes":"Prevents vertical expansion beyond the explicit height. true = ShrinkBegin, false = Fill."},
        ],
        "examples_sml": [
            "TextureRect {",
            "    src: \"res://logo.svg\"",
            "    width: 72",
            "    height: 72",
            "    shrinkH: true",
            "    shrinkV: true",
            "}",
        ],
    }
