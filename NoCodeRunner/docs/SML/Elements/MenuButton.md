# MenuButton

## Inheritance

[MenuButton](MenuButton.md) → [Button](Button.md) → [BaseButton](BaseButton.md) → [Control](Control.md) → [CanvasItem](CanvasItem.md) → [Node](Node.md) → [Object](Object.md)

## Collection Items

This control appears to manage internal **items** (collection-style API).
Items are typically not represented as child nodes/properties in Godot.
In SML, this will be represented via **pseudo child elements** (documented per control).

## Properties

This page lists **only properties declared by `MenuButton`**.
Inherited properties are documented in: [Button](Button.md)

| Godot Property | SML Property | Type | Default |
|-|-|-|-|
| item_count | itemCount | int | — |
| switch_on_hover | switchOnHover | bool | — |

## Events

This page lists **only signals declared by `MenuButton`**.
Inherited signals are documented in: [Button](Button.md)

| Godot Signal | SMS Event | Params |
|-|-|-|
| about_to_popup | `on <id>.aboutToPopup() { ... }` | — |

## SML Items (TODO)

This control appears to manage internal items, but a dedicated SML pseudo-child specification has not been defined yet.
Use the generated signals and the `collection: true` marker in `sms-reference.sml` as implementation hints.
