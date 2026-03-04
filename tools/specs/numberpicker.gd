extends RefCounted

func get_spec() -> Dictionary:
    return {
        "name": "NumberPicker",
        "extends": "LineEdit",
        "notes": [
            "Editor-like numeric input with select-all on focus, drag-to-adjust, and formatted preview text.",
            "Configured at runtime via ui.configureNumericLineEdit(...), ui.setNumericLineEditValue(...), ui.getNumericLineEditValue(...)."
        ],
        "properties": [
            {"name": "placeholderText", "type": "string", "default": ""},
            {"name": "sizeFlagsHorizontal", "type": "sizeflags", "default": "fill"},
            {"name": "sizeFlagsVertical", "type": "sizeflags", "default": "fill"}
        ],
        "actions": [],
        "events": [
            {"name":"textChanged", "params":[{"name":"newText","type":"string"}]},
            {"name":"textSubmitted", "params":[{"name":"newText","type":"string"}]
            }
        ]
    }
