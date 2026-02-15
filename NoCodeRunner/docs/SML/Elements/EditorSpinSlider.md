# EditorSpinSlider

## Inheritance

[EditorSpinSlider](EditorSpinSlider.md) → [Range](Range.md) → [Control](Control.md) → [CanvasItem](CanvasItem.md) → [Node](Node.md) → [Object](Object.md)

## Collection Items

This control appears to manage internal **items** (collection-style API).
Items are typically not represented as child nodes/properties in Godot.
In SML, this will be represented via **pseudo child elements** (documented per control).

## Properties

This page lists **only properties declared by `EditorSpinSlider`**.
Inherited properties are documented in: [Range](Range.md)

| Godot Property | SML Property | Type | Default |
|-|-|-|-|
| control_state | controlState | bool | — |
| editing_integer | editingInteger | bool | — |
| flat | flat | bool | — |
| hide_slider | hideSlider | bool | — |
| label | label | string | — |
| read_only | readOnly | bool | — |
| suffix | suffix | string | — |

## Events

This page lists **only signals declared by `EditorSpinSlider`**.
Inherited signals are documented in: [Range](Range.md)

| Godot Signal | SMS Event | Params |
|-|-|-|
| grabbed | `on <id>.grabbed() { ... }` | — |
| ungrabbed | `on <id>.ungrabbed() { ... }` | — |
| updown_pressed | `on <id>.updownPressed() { ... }` | — |
| value_focus_entered | `on <id>.valueFocusEntered() { ... }` | — |
| value_focus_exited | `on <id>.valueFocusExited() { ... }` | — |
