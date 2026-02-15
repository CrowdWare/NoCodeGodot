# EmbeddedProcessBase

## Inheritance

[EmbeddedProcessBase](EmbeddedProcessBase.md) → [Control](Control.md) → [CanvasItem](CanvasItem.md) → [Node](Node.md) → [Object](Object.md)

## Collection Items

This control appears to manage internal **items** (collection-style API).
Items are typically not represented as child nodes/properties in Godot.
In SML, this will be represented via **pseudo child elements** (documented per control).

## Derived Classes

### Direct subclasses

- [EmbeddedProcessMacOS](EmbeddedProcessMacOS.md)

## Properties

This page lists **only properties declared by `EmbeddedProcessBase`**.
Inherited properties are documented in: [Control](Control.md)

| Godot Property | SML Property | Type | Default |
|-|-|-|-|

## Events

This page lists **only signals declared by `EmbeddedProcessBase`**.
Inherited signals are documented in: [Control](Control.md)

| Godot Signal | SMS Event | Params |
|-|-|-|
| embedded_process_focused | `on <id>.embeddedProcessFocused() { ... }` | — |
| embedded_process_updated | `on <id>.embeddedProcessUpdated() { ... }` | — |
| embedding_completed | `on <id>.embeddingCompleted() { ... }` | — |
| embedding_failed | `on <id>.embeddingFailed() { ... }` | — |
