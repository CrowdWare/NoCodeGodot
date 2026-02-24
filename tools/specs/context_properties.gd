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
                        "description": "Tab title read by the parent TabContainer. Use attached property syntax: `<containerId>.title: \"Caption\"` or `TabContainer.title: \"Caption\"`."
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
                        "description": "Tab title read by the parent DockingContainer. Use attached property syntax: `<containerId>.title: \"Caption\"` or `DockingContainer.title: \"Caption\"`."
                    }
                ]
            }
        ]
    }
