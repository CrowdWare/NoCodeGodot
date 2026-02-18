extends RefCounted

func get_spec() -> Dictionary:
    return {
        "name": "DockingHost",
        "backing": "Container",
        "properties": [
            {"sml":"id", "type":"identifier", "default":"—"},
            {"sml":"gap", "type":"int", "default":"0"},
            {"sml":"endGap", "type":"int", "default":"30"},
            {"sml":"anchors", "type":"string", "default":"\"\""},
            {"sml":"x", "type":"int", "default":"0"},
            {"sml":"y", "type":"int", "default":"0"},
            {"sml":"width", "type":"int", "default":"0"},
            {"sml":"height", "type":"int", "default":"0"}
        ],
        "actions": [],
        "notes": [
            "Layouts multiple DockingContainer children using dockSide/fixedWidth/flex semantics.",
            "Typically used as a full-rect region inside a Window."
        ],
        "examples_sml": [
            "DockingHost {",
            "    id: mainDockHost",
            "    anchors: left | top | right | bottom",
            "    gap: 8",
            "}"
        ]
    }
