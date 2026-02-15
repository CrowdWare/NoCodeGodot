# BaseButton

## Inheritance

[BaseButton](BaseButton.md) → [Control](Control.md) → [CanvasItem](CanvasItem.md) → [Node](Node.md) → [Object](Object.md)

## Collection Items

This control appears to manage internal **items** (collection-style API).
Items are typically not represented as child nodes/properties in Godot.
In SML, this will be represented via **pseudo child elements** (documented per control).

## Derived Classes

### Direct subclasses

- [Button](Button.md)
- [LinkButton](LinkButton.md)
- [TextureButton](TextureButton.md)

## Properties

This page lists **only properties declared by `BaseButton`**.
Inherited properties are documented in: [Control](Control.md)

| Godot Property | SML Property | Type | Default |
|-|-|-|-|
| action_mode | actionMode | int | — |
| button_mask | buttonMask | int | — |
| button_pressed | buttonPressed | bool | — |
| disabled | disabled | bool | — |
| keep_pressed_outside | keepPressedOutside | bool | — |
| shortcut_feedback | shortcutFeedback | bool | — |
| shortcut_in_tooltip | shortcutInTooltip | bool | — |
| toggle_mode | toggleMode | bool | — |

## Events

This page lists **only signals declared by `BaseButton`**.
Inherited signals are documented in: [Control](Control.md)

| Godot Signal | SMS Event | Params |
|-|-|-|
| button_down | `on <id>.buttonDown() { ... }` | — |
| button_up | `on <id>.buttonUp() { ... }` | — |
| pressed | `on <id>.pressed() { ... }` | — |
| toggled | `on <id>.toggled(toggledOn) { ... }` | bool toggledOn |

## SML Items (TODO)

This control appears to manage internal items, but a dedicated SML pseudo-child specification has not been defined yet.
Use the generated signals and the `collection: true` marker in `sms-reference.sml` as implementation hints.
