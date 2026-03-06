extends RefCounted

func get_spec() -> Dictionary:
    return {
        "name": "Terminal",
        "backing": "Panel",
        "notes": [
            "Headless/CLI-oriented root marker.",
            "ForgeRunner uses this root to route startup download progress to console/log output."
        ],
        "properties": [
            {"sml":"id", "type":"identifier", "default":"—"},
            {"sml":"title", "type":"string", "default":"\"\""},
            {"sml":"size", "type":"vec2i", "default":"640, 480"},
            {"sml":"pos", "type":"vec2i", "default":"0, 0"}
        ],
        "examples_sml": [
            "Terminal {",
            "    id: demoTerminal",
            "}",
        ],
    }
