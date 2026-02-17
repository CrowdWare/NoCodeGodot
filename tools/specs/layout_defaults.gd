extends RefCounted

func get_spec() -> Dictionary:
    return {
        "name": "layout_defaults",
        "autoFillMaxSizeTypes": [
            "Window",
            "Panel",
            "PanelContainer",
            "HBoxContainer",
            "VBoxContainer",
            "CodeEdit",
            "Markdown",
            "TabContainer",
            "Tree"
        ],
        "menuBarDefaults": {
            "x": 0,
            "y": 0,
            "height": 28,
            "anchorLeft": true,
            "anchorRight": true,
            "anchorTop": true,
            "minHeight": 28,
            "zIndex": 1000
        }
    }