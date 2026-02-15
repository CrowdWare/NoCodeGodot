# GraphNode

## Inheritance

[GraphNode](GraphNode.md) → [GraphElement](GraphElement.md) → [Container](Container.md) → [Control](Control.md) → [CanvasItem](CanvasItem.md) → [Node](Node.md) → [Object](Object.md)

## Collection Items

This control appears to manage internal **items** (collection-style API).
Items are typically not represented as child nodes/properties in Godot.
In SML, this will be represented via **pseudo child elements** (documented per control).

## Properties

This page lists **only properties declared by `GraphNode`**.
Inherited properties are documented in: [GraphElement](GraphElement.md)

| Godot Property | SML Property | Type | Default |
|-|-|-|-|
| ignore_invalid_connection_type | ignoreInvalidConnectionType | bool | — |
| slots_focus_mode | slotsFocusMode | int | — |
| title | title | string | — |

## Events

This page lists **only signals declared by `GraphNode`**.
Inherited signals are documented in: [GraphElement](GraphElement.md)

| Godot Signal | SMS Event | Params |
|-|-|-|
| slot_sizes_changed | `on <id>.slotSizesChanged() { ... }` | — |
| slot_updated | `on <id>.slotUpdated(slotIndex) { ... }` | int slotIndex |
