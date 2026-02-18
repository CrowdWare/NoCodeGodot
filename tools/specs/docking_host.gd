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
            "Supports side columns with optional bottom companions (farLeftBottom, leftBottom, rightBottom, farRightBottom).",
            "If only top or bottom panel of a side is present/visible, that panel fills the full host height.",
            "If both are present/visible, top and bottom split the host height 50/50.",
            "Resize handles are created per interior gap as upper and lower segments to keep middle gap area clickable for menu buttons.",
            "Typically used as a full-rect region inside a Window."
        ],
        "examples_sml": [
            "DockingHost {",
            "    id: mainDockHost",
            "    anchors: left | top | right | bottom",
            "    gap: 8",
            "    endGap: 30",
            "}"
        ]
    }
