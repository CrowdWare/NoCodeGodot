# FoldableContainer

## Inheritance

[FoldableContainer](FoldableContainer.md) → [Container](Container.md) → [Control](Control.md) → [CanvasItem](CanvasItem.md) → [Node](Node.md) → [Object](Object.md)

## Collection Items

This control appears to manage internal **items** (collection-style API).
Items are typically not represented as child nodes/properties in Godot.
In SML, this will be represented via **pseudo child elements** (documented per control).

## Properties

This page lists **only properties declared by `FoldableContainer`**.
Inherited properties are documented in: [Container](Container.md)

| Godot Property | SML Property | Type | Default |
|-|-|-|-|
| folded | folded | bool | — |
| language | language | string | — |
| title | title | string | — |
| title_alignment | titleAlignment | int | — |
| title_position | titlePosition | int | — |
| title_text_direction | titleTextDirection | int | — |
| title_text_overrun_behavior | titleTextOverrunBehavior | int | — |

## Events

This page lists **only signals declared by `FoldableContainer`**.
Inherited signals are documented in: [Container](Container.md)

| Godot Signal | SMS Event | Params |
|-|-|-|
| folding_changed | `on <id>.foldingChanged(isFolded) { ... }` | bool isFolded |
