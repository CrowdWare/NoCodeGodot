extends RefCounted

func get_spec() -> Dictionary:
    return {
        "name": "context_properties",
        "rules": [
            {
                "parent": "TabContainer",
                "child": "*",
                "properties": [
                    {
                        "sml": "title",
                        "type": "string",
                        "targetMeta": "tabTitle",
                        "description": "Tab title interpreted by the parent TabContainer for any child page."
                    }
                ]
            },
            {
                "parent": "DockingContainer",
                "child": "*",
                "properties": [
                    {
                        "sml": "title",
                        "type": "string",
                        "targetMeta": "tabTitle",
                        "description": "Tab title interpreted by the DockingContainer's internal TabContainer for any child page."
                    }
                ]
            }
        ]
    }