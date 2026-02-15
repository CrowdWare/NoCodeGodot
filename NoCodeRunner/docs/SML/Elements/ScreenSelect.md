# ScreenSelect

## Inheritance

[ScreenSelect](ScreenSelect.md) → [Button](Button.md) → [BaseButton](BaseButton.md) → [Control](Control.md) → [CanvasItem](CanvasItem.md) → [Node](Node.md) → [Object](Object.md)

## Collection Items

This control appears to manage internal **items** (collection-style API).
Items are typically not represented as child nodes/properties in Godot.
In SML, this will be represented via **pseudo child elements** (documented per control).

## Properties

This page lists **only properties declared by `ScreenSelect`**.
Inherited properties are documented in: [Button](Button.md)

| Godot Property | SML Property | Type | Default |
|-|-|-|-|

## Events

This page lists **only signals declared by `ScreenSelect`**.
Inherited signals are documented in: [Button](Button.md)

| Godot Signal | SMS Event | Params |
|-|-|-|
| request_open_in_screen | `on <id>.requestOpenInScreen(screen) { ... }` | int screen |

## SML Items (TODO)

This control appears to manage internal items, but a dedicated SML pseudo-child specification has not been defined yet.
Use the generated signals and the `collection: true` marker in `sms-reference.sml` as implementation hints.
