extends RefCounted

func get_spec() -> Dictionary:
    return {
        "name": "DockingContainer",
        "backing": "PanelContainer",
        "properties": [
            {"sml":"id", "type":"identifier", "default":"—"},
            {"sml":"dockSide", "type":"enum", "default":"center"},
            {"sml":"fixedWidth", "type":"int", "default":"240"},
            {"sml":"minFixedWidth", "type":"int", "default":"140"},
            {"sml":"flex", "type":"bool", "default":"false"},
            {"sml":"closeable", "type":"bool", "default":"true"},
            {"sml":"dragToRearrangeEnabled", "type":"bool", "default":"true"},
            {"sml":"tabsRearrangeGroup", "type":"int", "default":"1"}
        ],
        "actions": [],
        "notes": [
            "Automatically creates an internal TabContainer.",
            "Direct child controls become tabs; use context property 'title' on each child to define tab captions.",
            "dockSide supports: farLeft, farLeftBottom, left, leftBottom, center, right, rightBottom, farRight, farRightBottom.",
            "Use enum syntax without quotes, e.g. dockSide: left.",
            "dragToRearrangeEnabled: false excludes this container from docking move targets (kebab menu).",
            "A container is not listed as move target for itself (same dock slot is filtered)."
        ],
        "examples_sml": [
            "DockingContainer {",
            "    id: leftDock",
            "    dockSide: left",
            "    fixedWidth: 280",
            "    dragToRearrangeEnabled: true",
            "    tabsRearrangeGroup: 1",
            "}",
            "",
            "DockingContainer {",
            "    id: leftBottomDock",
            "    dockSide: leftBottom",
            "    fixedWidth: 280",
            "    dragToRearrangeEnabled: true",
            "    tabsRearrangeGroup: 1",
            "}",
        ]
    }
