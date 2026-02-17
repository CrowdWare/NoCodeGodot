extends RefCounted

func get_spec() -> Dictionary:
    return {
        "name": "runtime_overrides",
        "propertiesByType": {
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
        }
    }