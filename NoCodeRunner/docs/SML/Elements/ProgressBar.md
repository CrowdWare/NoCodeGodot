# ProgressBar

## Inheritance

[ProgressBar](ProgressBar.md) → [Range](Range.md) → [Control](Control.md) → [CanvasItem](CanvasItem.md) → [Node](Node.md) → [Object](Object.md)

## Collection Items

This control appears to manage internal **items** (collection-style API).
Items are typically not represented as child nodes/properties in Godot.
In SML, this will be represented via **pseudo child elements** (documented per control).

## Properties

This page lists **only properties declared by `ProgressBar`**.
Inherited properties are documented in: [Range](Range.md)

| Godot Property | SML Property | Type | Default |
|-|-|-|-|
| editor_preview_indeterminate | editorPreviewIndeterminate | bool | — |
| fill_mode | fillMode | int | — |
| indeterminate | indeterminate | bool | — |
| show_percentage | showPercentage | bool | — |

## Events

This page lists **only signals declared by `ProgressBar`**.
Inherited signals are documented in: [Range](Range.md)

| Godot Signal | SMS Event | Params |
|-|-|-|

## SML Items (TODO)

This control appears to manage internal items, but a dedicated SML pseudo-child specification has not been defined yet.
Use the generated signals and the `collection: true` marker in `sms-reference.sml` as implementation hints.
