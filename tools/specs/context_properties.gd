extends RefCounted

func get_spec() -> Dictionary:
    return {
        "name": "context_properties",
        "rules": [
            {
                "parent": "TabBar",
                "child": "TabContainer",
                "properties": [
                    {
                        "sml": "title",
                        "type": "string",
                        "targetMeta": "tabTitle",
                        "description": "Tab title interpreted by the parent TabBar for this child page."
                    }
                ]
            }
        ]
    }