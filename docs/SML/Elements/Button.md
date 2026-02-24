# Button

## Inheritance

[Button](Button.md) → [BaseButton](BaseButton.md) → [Control](Control.md) → [CanvasItem](CanvasItem.md) → [Node](Node.md) → [Object](Object.md)

## Derived Classes

### Direct subclasses

- [CheckBox](CheckBox.md)
- [CheckButton](CheckButton.md)
- [ColorPickerButton](ColorPickerButton.md)
- [MenuButton](MenuButton.md)
- [OptionButton](OptionButton.md)

## Properties

This page lists **only properties declared by `Button`**.
Inherited properties are documented in: [BaseButton](BaseButton.md)

| Godot Property | SML Property | Type | Default |
|-|-|-|-|
| alignment | alignment | int | — |
| autowrap_mode | autowrapMode | int | — |
| autowrap_trim_flags | autowrapTrimFlags | int | — |
| clip_text | clipText | bool | — |
| expand_icon | expandIcon | bool | — |
| flat | flat | bool | — |
| icon_alignment | iconAlignment | int | — |
| language | language | string | — |
| text | text | string | — |
| text_direction | textDirection | int | — |
| text_overrun_behavior | textOverrunBehavior | int | — |
| vertical_icon_alignment | verticalIconAlignment | int | — |

## Events

This page lists **only signals declared by `Button`**.
Inherited signals are documented in: [BaseButton](BaseButton.md)

| Godot Signal | SMS Event | Params |
|-|-|-|

## Runtime Actions

This page lists **callable methods declared by `Button`**.
Inherited actions are documented in: [BaseButton](BaseButton.md)

| Godot Method | SMS Call | Params | Returns |
|-|-|-|-|
| get_button_icon | `<id>.getButtonIcon()` | — | Object |
| get_text_alignment | `<id>.getTextAlignment()` | — | int |
| set_text_alignment | `<id>.setTextAlignment(alignment)` | int alignment | void |

## Attached Properties

These properties are declared by a parent provider and set on this element using the qualified syntax `<providerId>.property: value` or `ProviderType.property: value`.

### Provided by `TabContainer`

| Attached Property | Type | Description |
|-|-|-|
| title | string | Tab title read by the parent TabContainer. Use attached property syntax: `<containerId>.title: "Caption"` or `TabContainer.title: "Caption"`. |

### Provided by `DockingContainer`

| Attached Property | Type | Description |
|-|-|-|
| title | string | Tab title read by the parent DockingContainer. Use attached property syntax: `<containerId>.title: "Caption"` or `DockingContainer.title: "Caption"`. |

