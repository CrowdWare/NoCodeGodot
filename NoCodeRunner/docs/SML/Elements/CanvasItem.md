# CanvasItem

## Inheritance

[CanvasItem](CanvasItem.md) → [Node](Node.md) → [Object](Object.md)

## Collection Items

This control appears to manage internal **items** (collection-style API).
Items are typically not represented as child nodes/properties in Godot.
In SML, this will be represented via **pseudo child elements** (documented per control).

## Derived Classes

Classes listed below inherit from `CanvasItem`.

### Direct subclasses

- [Control](Control.md)
- [Node2D](Node2D.md)

## Properties

This page lists **only properties declared by `CanvasItem`**.
Inherited properties are documented in: [Node](Node.md)

| Godot Property | SML Property | Type | Default |
|-|-|-|-|
| clip_children | clipChildren | int | — |
| light_mask | lightMask | int | — |
| modulate | modulate | Color | — |
| self_modulate | selfModulate | Color | — |
| show_behind_parent | showBehindParent | bool | — |
| texture_filter | textureFilter | int | — |
| texture_repeat | textureRepeat | int | — |
| top_level | topLevel | bool | — |
| use_parent_material | useParentMaterial | bool | — |
| visibility_layer | visibilityLayer | int | — |
| visible | visible | bool | — |
| y_sort_enabled | ySortEnabled | bool | — |
| z_as_relative | zAsRelative | bool | — |
| z_index | zIndex | int | — |

## Events

This page lists **only signals declared by `CanvasItem`**.
Inherited signals are documented in: [Node](Node.md)

| Godot Signal | SMS Event | Params |
|-|-|-|
| draw | `on <id>.draw() { ... }` | — |
| hidden | `on <id>.hidden() { ... }` | — |
| item_rect_changed | `on <id>.itemRectChanged() { ... }` | — |
| visibility_changed | `on <id>.visibilityChanged() { ... }` | — |

## SML Items (TODO)

This control appears to manage internal items, but a dedicated SML pseudo-child specification has not been defined yet.
Use the generated signals and the `collection: true` marker in `sms-reference.sml` as implementation hints.
