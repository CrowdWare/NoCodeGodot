# AnimationBezierTrackEdit

## Inheritance

[AnimationBezierTrackEdit](AnimationBezierTrackEdit.md) → [Control](Control.md) → [CanvasItem](CanvasItem.md) → [Node](Node.md) → [Object](Object.md)

## Collection Items

This control appears to manage internal **items** (collection-style API).
Items are typically not represented as child nodes/properties in Godot.
In SML, this will be represented via **pseudo child elements** (documented per control).

## Properties

This page lists **only properties declared by `AnimationBezierTrackEdit`**.
Inherited properties are documented in: [Control](Control.md)

| Godot Property | SML Property | Type | Default |
|-|-|-|-|

## Events

This page lists **only signals declared by `AnimationBezierTrackEdit`**.
Inherited signals are documented in: [Control](Control.md)

| Godot Signal | SMS Event | Params |
|-|-|-|
| clear_selection | `on <id>.clearSelection() { ... }` | — |
| deselect_key | `on <id>.deselectKey(index, track) { ... }` | int index, int track |
| select_key | `on <id>.selectKey(index, single, track) { ... }` | int index, bool single, int track |
| timeline_changed | `on <id>.timelineChanged(position, timelineOnly) { ... }` | float position, bool timelineOnly |

## SML Items (TODO)

This control appears to manage internal items, but a dedicated SML pseudo-child specification has not been defined yet.
Use the generated signals and the `collection: true` marker in `sms-reference.sml` as implementation hints.
