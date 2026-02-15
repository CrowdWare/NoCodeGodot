# AnimationTimelineEdit

## Inheritance

[AnimationTimelineEdit](AnimationTimelineEdit.md) → [Range](Range.md) → [Control](Control.md) → [CanvasItem](CanvasItem.md) → [Node](Node.md) → [Object](Object.md)

## Collection Items

This control appears to manage internal **items** (collection-style API).
Items are typically not represented as child nodes/properties in Godot.
In SML, this will be represented via **pseudo child elements** (documented per control).

## Properties

This page lists **only properties declared by `AnimationTimelineEdit`**.
Inherited properties are documented in: [Range](Range.md)

| Godot Property | SML Property | Type | Default |
|-|-|-|-|

## Events

This page lists **only signals declared by `AnimationTimelineEdit`**.
Inherited signals are documented in: [Range](Range.md)

| Godot Signal | SMS Event | Params |
|-|-|-|
| filter_changed | `on <id>.filterChanged() { ... }` | — |
| length_changed | `on <id>.lengthChanged(size) { ... }` | float size |
| name_limit_changed | `on <id>.nameLimitChanged() { ... }` | — |
| timeline_changed | `on <id>.timelineChanged(position, timelineOnly) { ... }` | float position, bool timelineOnly |
| track_added | `on <id>.trackAdded(track) { ... }` | int track |
| zoom_changed | `on <id>.zoomChanged() { ... }` | — |
