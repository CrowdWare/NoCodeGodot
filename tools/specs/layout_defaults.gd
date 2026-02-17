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
        ]
    }