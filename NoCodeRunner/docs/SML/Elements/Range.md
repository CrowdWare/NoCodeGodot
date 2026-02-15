# Range

## Inheritance

[Range](Range.md) → [Control](Control.md) → [CanvasItem](CanvasItem.md) → [Node](Node.md) → [Object](Object.md)

## Collection Items

This control appears to manage internal **items** (collection-style API).
Items are typically not represented as child nodes/properties in Godot.
In SML, this will be represented via **pseudo child elements** (documented per control).

## Derived Classes

### Direct subclasses

- [AnimationTimelineEdit](AnimationTimelineEdit.md)
- [EditorSpinSlider](EditorSpinSlider.md)
- [ProgressBar](ProgressBar.md)
- [ScrollBar](ScrollBar.md)
- [Slider](Slider.md)
- [SpinBox](SpinBox.md)
- [TextureProgressBar](TextureProgressBar.md)

## Properties

This page lists **only properties declared by `Range`**.
Inherited properties are documented in: [Control](Control.md)

| Godot Property | SML Property | Type | Default |
|-|-|-|-|
| allow_greater | allowGreater | bool | — |
| allow_lesser | allowLesser | bool | — |
| exp_edit | expEdit | bool | — |
| max_value | maxValue | float | — |
| min_value | minValue | float | — |
| page | page | float | — |
| rounded | rounded | bool | — |
| step | step | float | — |
| value | value | float | — |

## Events

This page lists **only signals declared by `Range`**.
Inherited signals are documented in: [Control](Control.md)

| Godot Signal | SMS Event | Params |
|-|-|-|
| changed | `on <id>.changed() { ... }` | — |
| value_changed | `on <id>.valueChanged(value) { ... }` | float value |

## SML Items (TODO)

This control appears to manage internal items, but a dedicated SML pseudo-child specification has not been defined yet.
Use the generated signals and the `collection: true` marker in `sms-reference.sml` as implementation hints.
