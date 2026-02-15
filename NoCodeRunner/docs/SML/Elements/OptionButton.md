# OptionButton

## Inheritance

[OptionButton](OptionButton.md) → [Button](Button.md) → [BaseButton](BaseButton.md) → [Control](Control.md) → [CanvasItem](CanvasItem.md) → [Node](Node.md) → [Object](Object.md)

## Collection Items

This control appears to manage internal **items** (collection-style API).
Items are typically not represented as child nodes/properties in Godot.
In SML, this will be represented via **pseudo child elements** (documented per control).

## Derived Classes

### Direct subclasses

- [EditorVariantTypeOptionButton](EditorVariantTypeOptionButton.md)

## Properties

This page lists **only properties declared by `OptionButton`**.
Inherited properties are documented in: [Button](Button.md)

| Godot Property | SML Property | Type | Default |
|-|-|-|-|
| allow_reselect | allowReselect | bool | — |
| fit_to_longest_item | fitToLongestItem | bool | — |
| item_count | itemCount | int | — |
| selected | selected | int | — |

## Events

This page lists **only signals declared by `OptionButton`**.
Inherited signals are documented in: [Button](Button.md)

| Godot Signal | SMS Event | Params |
|-|-|-|
| item_focused | `on <id>.itemFocused(index) { ... }` | int index |
| item_selected | `on <id>.itemSelected(index) { ... }` | int index |
