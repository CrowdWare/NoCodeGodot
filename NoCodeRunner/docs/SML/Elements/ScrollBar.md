# ScrollBar

## Inheritance

[ScrollBar](ScrollBar.md) → [Range](Range.md) → [Control](Control.md) → [CanvasItem](CanvasItem.md) → [Node](Node.md) → [Object](Object.md)

## Collection Items

This control appears to manage internal **items** (collection-style API).
Items are typically not represented as child nodes/properties in Godot.
In SML, this will be represented via **pseudo child elements** (documented per control).

## Derived Classes

### Direct subclasses

- [HScrollBar](HScrollBar.md)
- [VScrollBar](VScrollBar.md)

## Properties

This page lists **only properties declared by `ScrollBar`**.
Inherited properties are documented in: [Range](Range.md)

| Godot Property | SML Property | Type | Default |
|-|-|-|-|
| custom_step | customStep | float | — |

## Events

This page lists **only signals declared by `ScrollBar`**.
Inherited signals are documented in: [Range](Range.md)

| Godot Signal | SMS Event | Params |
|-|-|-|
| scrolling | `on <id>.scrolling() { ... }` | — |

## SML Items (TODO)

This control appears to manage internal items, but a dedicated SML pseudo-child specification has not been defined yet.
Use the generated signals and the `collection: true` marker in `sms-reference.sml` as implementation hints.
