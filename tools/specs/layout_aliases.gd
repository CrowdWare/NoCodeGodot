extends RefCounted

# Declarative layout alias policy for SML.
# Canonical properties:
# - size (vec2)
# - position (vec2)
#
# Aliases are applied in source order (last write wins).
func get_spec() -> Dictionary:
    return {
        "name": "layout_aliases",
        "rules": [
            {
                "canonical": "size",
                "appliesTo": ["Control", "Window"],
                "aliases": [
                    {"name": "size", "mode": "whole"},
                    {"name": "width", "mode": "x"},
                    {"name": "height", "mode": "y"}
                ],
                "merge": "partial",
                "precedence": "last-write-wins"
            },
            {
                "canonical": "position",
                "appliesTo": ["Control", "Window"],
                "aliases": [
                    {"name": "position", "mode": "whole"},
                    {"name": "pos", "mode": "whole"},
                    {"name": "x", "mode": "x"},
                    {"name": "left", "mode": "x"},
                    {"name": "y", "mode": "y"},
                    {"name": "top", "mode": "y"}
                ],
                "merge": "partial",
                "precedence": "last-write-wins"
            }
        ]
    }