extends RefCounted

func get_spec() -> Dictionary:
    return {
        "name": "Markdown",
        "backing": "VBoxContainer",
        "properties": [
            {"sml":"id", "type":"identifier", "default":"—"},
            {"sml":"padding", "type":"padding", "default":"0"},
            {"sml":"text", "type":"string", "default":"\"\""},
            {"sml":"src", "type":"string", "default":"\"\""},
        ],
        "actions": [],
    }