# GraphFrame

## Inheritance

[GraphFrame](GraphFrame.md) → [GraphElement](GraphElement.md) → [Container](Container.md) → [Control](Control.md) → [CanvasItem](CanvasItem.md) → [Node](Node.md) → [Object](Object.md)

## Collection Items

This control appears to manage internal **items** (collection-style API).
Items are typically not represented as child nodes/properties in Godot.
In SML, this will be represented via **pseudo child elements** (documented per control).

## Properties

This page lists **only properties declared by `GraphFrame`**.
Inherited properties are documented in: [GraphElement](GraphElement.md)

| Godot Property | SML Property | Type | Default |
|-|-|-|-|
| autoshrink_enabled | autoshrinkEnabled | bool | — |
| autoshrink_margin | autoshrinkMargin | int | — |
| drag_margin | dragMargin | int | — |
| tint_color | tintColor | Color | — |
| tint_color_enabled | tintColorEnabled | bool | — |
| title | title | string | — |

## Events

This page lists **only signals declared by `GraphFrame`**.
Inherited signals are documented in: [GraphElement](GraphElement.md)

| Godot Signal | SMS Event | Params |
|-|-|-|
| autoshrink_changed | `on <id>.autoshrinkChanged() { ... }` | — |

## SML Items (TODO)

This control appears to manage internal items, but a dedicated SML pseudo-child specification has not been defined yet.
Use the generated signals and the `collection: true` marker in `sms-reference.sml` as implementation hints.
