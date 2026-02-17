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
                "aliases": ["width", "height"],
                "merge": "partial",
                "precedence": "last-write-wins"
            },
            {
                "canonical": "position",
                "appliesTo": ["Control", "Window"],
                "aliases": ["x", "y", "left", "top", "pos"],
                "merge": "partial",
                "precedence": "last-write-wins"
            }
        ]
    }