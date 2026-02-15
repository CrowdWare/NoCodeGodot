# Button

## Inheritance

[Button](Button.md) → [BaseButton](BaseButton.md) → [Control](Control.md) → [CanvasItem](CanvasItem.md) → [Node](Node.md) → [Object](Object.md)

## Collection Items

This control appears to manage internal **items** (collection-style API).
Items are typically not represented as child nodes/properties in Godot.
In SML, this will be represented via **pseudo child elements** (documented per control).

## Derived Classes

### Direct subclasses

- [CheckBox](CheckBox.md)
- [CheckButton](CheckButton.md)
- [ColorPickerButton](ColorPickerButton.md)
- [ControlEditorPopupButton](ControlEditorPopupButton.md)
- [EditorInspectorActionButton](EditorInspectorActionButton.md)
- [EditorObjectSelector](EditorObjectSelector.md)
- [EditorTranslationPreviewButton](EditorTranslationPreviewButton.md)
- [MenuButton](MenuButton.md)
- [OptionButton](OptionButton.md)
- [ScreenSelect](ScreenSelect.md)

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

## SML Items (TODO)

This control appears to manage internal items, but a dedicated SML pseudo-child specification has not been defined yet.
Use the generated signals and the `collection: true` marker in `sms-reference.sml` as implementation hints.
