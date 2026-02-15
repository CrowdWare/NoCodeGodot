# Range

## Inheritance

[Range](Range.md) → [Control](Control.md) → [CanvasItem](CanvasItem.md) → [Node](Node.md) → [Object](Object.md)

## Derived Classes

### Direct subclasses

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
