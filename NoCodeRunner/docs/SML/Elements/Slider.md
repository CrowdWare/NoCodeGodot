# Slider

## Inheritance

[Slider](Slider.md) → [Range](Range.md) → [Control](Control.md) → [CanvasItem](CanvasItem.md) → [Node](Node.md) → [Object](Object.md)

## Collection Items

This control appears to manage internal **items** (collection-style API).
Items are typically not represented as child nodes/properties in Godot.
In SML, this will be represented via **pseudo child elements** (documented per control).

## Derived Classes

### Direct subclasses

- [HSlider](HSlider.md)
- [VSlider](VSlider.md)

## Properties

This page lists **only properties declared by `Slider`**.
Inherited properties are documented in: [Range](Range.md)

| Godot Property | SML Property | Type | Default |
|-|-|-|-|
| editable | editable | bool | — |
| scrollable | scrollable | bool | — |
| tick_count | tickCount | int | — |
| ticks_on_borders | ticksOnBorders | bool | — |
| ticks_position | ticksPosition | int | — |

## Events

This page lists **only signals declared by `Slider`**.
Inherited signals are documented in: [Range](Range.md)

| Godot Signal | SMS Event | Params |
|-|-|-|
| drag_ended | `on <id>.dragEnded(valueChanged) { ... }` | bool valueChanged |
| drag_started | `on <id>.dragStarted() { ... }` | — |

## SML Items (TODO)

This control appears to manage internal items, but a dedicated SML pseudo-child specification has not been defined yet.
Use the generated signals and the `collection: true` marker in `sms-reference.sml` as implementation hints.
