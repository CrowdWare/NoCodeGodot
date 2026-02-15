# CodeTextEditor

## Inheritance

[CodeTextEditor](CodeTextEditor.md) → [VBoxContainer](VBoxContainer.md) → [BoxContainer](BoxContainer.md) → [Container](Container.md) → [Control](Control.md) → [CanvasItem](CanvasItem.md) → [Node](Node.md) → [Object](Object.md)

## Collection Items

This control appears to manage internal **items** (collection-style API).
Items are typically not represented as child nodes/properties in Godot.
In SML, this will be represented via **pseudo child elements** (documented per control).

## Properties

This page lists **only properties declared by `CodeTextEditor`**.
Inherited properties are documented in: [VBoxContainer](VBoxContainer.md)

| Godot Property | SML Property | Type | Default |
|-|-|-|-|

## Events

This page lists **only signals declared by `CodeTextEditor`**.
Inherited signals are documented in: [VBoxContainer](VBoxContainer.md)

| Godot Signal | SMS Event | Params |
|-|-|-|
| load_theme_settings | `on <id>.loadThemeSettings() { ... }` | — |
| navigation_preview_ended | `on <id>.navigationPreviewEnded() { ... }` | — |
| show_errors_panel | `on <id>.showErrorsPanel() { ... }` | — |
| show_warnings_panel | `on <id>.showWarningsPanel() { ... }` | — |
| validate_script | `on <id>.validateScript() { ... }` | — |
| zoomed | `on <id>.zoomed(pZoomFactor) { ... }` | float pZoomFactor |

## SML Items (TODO)

This control appears to manage internal items, but a dedicated SML pseudo-child specification has not been defined yet.
Use the generated signals and the `collection: true` marker in `sms-reference.sml` as implementation hints.
