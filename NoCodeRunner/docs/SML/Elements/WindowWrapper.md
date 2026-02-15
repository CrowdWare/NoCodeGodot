# WindowWrapper

## Inheritance

[WindowWrapper](WindowWrapper.md) → [MarginContainer](MarginContainer.md) → [Container](Container.md) → [Control](Control.md) → [CanvasItem](CanvasItem.md) → [Node](Node.md) → [Object](Object.md)

## Collection Items

This control appears to manage internal **items** (collection-style API).
Items are typically not represented as child nodes/properties in Godot.
In SML, this will be represented via **pseudo child elements** (documented per control).

## Properties

This page lists **only properties declared by `WindowWrapper`**.
Inherited properties are documented in: [MarginContainer](MarginContainer.md)

| Godot Property | SML Property | Type | Default |
|-|-|-|-|

## Events

This page lists **only signals declared by `WindowWrapper`**.
Inherited signals are documented in: [MarginContainer](MarginContainer.md)

| Godot Signal | SMS Event | Params |
|-|-|-|
| window_close_requested | `on <id>.windowCloseRequested() { ... }` | — |
| window_size_changed | `on <id>.windowSizeChanged() { ... }` | — |
| window_visibility_changed | `on <id>.windowVisibilityChanged(visible) { ... }` | bool visible |

## SML Items (TODO)

This control appears to manage internal items, but a dedicated SML pseudo-child specification has not been defined yet.
Use the generated signals and the `collection: true` marker in `sms-reference.sml` as implementation hints.
