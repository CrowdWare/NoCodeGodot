# AnimationTrackEditor

## Inheritance

[AnimationTrackEditor](AnimationTrackEditor.md) → [VBoxContainer](VBoxContainer.md) → [BoxContainer](BoxContainer.md) → [Container](Container.md) → [Control](Control.md) → [CanvasItem](CanvasItem.md) → [Node](Node.md) → [Object](Object.md)

## Collection Items

This control appears to manage internal **items** (collection-style API).
Items are typically not represented as child nodes/properties in Godot.
In SML, this will be represented via **pseudo child elements** (documented per control).

## Properties

This page lists **only properties declared by `AnimationTrackEditor`**.
Inherited properties are documented in: [VBoxContainer](VBoxContainer.md)

| Godot Property | SML Property | Type | Default |
|-|-|-|-|

## Events

This page lists **only signals declared by `AnimationTrackEditor`**.
Inherited signals are documented in: [VBoxContainer](VBoxContainer.md)

| Godot Signal | SMS Event | Params |
|-|-|-|
| animation_len_changed | `on <id>.animationLenChanged(len) { ... }` | float len |
| animation_step_changed | `on <id>.animationStepChanged(step) { ... }` | float step |
| keying_changed | `on <id>.keyingChanged() { ... }` | — |
| timeline_changed | `on <id>.timelineChanged(position, timelineOnly, updatePositionOnly) { ... }` | float position, bool timelineOnly, bool updatePositionOnly |
