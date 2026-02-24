extends RefCounted

func get_spec() -> Dictionary:
    return {
        "name": "WindowDrag",
        "backing": "Panel",
        "properties": [
            {"sml":"id", "type":"identifier", "default":"—"},
            {"sml":"anchors", "type":"string", "default":"\"\""},
            {"sml":"x", "type":"int", "default":"0"},
            {"sml":"y", "type":"int", "default":"0"},
            {"sml":"width", "type":"int", "default":"0"},
            {"sml":"height", "type":"int", "default":"32"}
        ],
        "actions": [],
        "notes": [
            "Provides a native draggable title/caption area for custom frameless layouts.",
            "Single left click starts OS window drag.",
            "Double left click toggles maximize/restore (windowed <-> maximized).",
            "Useful together with Window.extendToTitle: true.",
            "When used with DockingHost, set WindowDrag.height analogous to DockingHost.offsetTop so caption drag area and dock top offset align."
        ],
        "examples_sml": [
            "Window {",
            "    id: mainWindow",
            "    extendToTitle: true",
            "",
            "    WindowDrag {",
            "        id: titleDrag",
            "        anchors: left | top | right",
            "        height: 34",
            "    }",
            "}"
        ]
    }
