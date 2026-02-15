# Control

## Inheritance

[Control](Control.md) → [CanvasItem](CanvasItem.md) → [Node](Node.md) → [Object](Object.md)

## Derived Controls

This is the entry point for all UI controls.
All classes listed below inherit from `Control`.

### Direct subclasses

- [BaseButton](BaseButton.md)
- [ColorRect](ColorRect.md)
- [Container](Container.md)
- [GraphEdit](GraphEdit.md)
- [ItemList](ItemList.md)
- [Label](Label.md)
- [LineEdit](LineEdit.md)
- [MenuBar](MenuBar.md)
- [NinePatchRect](NinePatchRect.md)
- [Panel](Panel.md)
- [Range](Range.md)
- [ReferenceRect](ReferenceRect.md)
- [RichTextLabel](RichTextLabel.md)
- [Separator](Separator.md)
- [TabBar](TabBar.md)
- [TextEdit](TextEdit.md)
- [TextureRect](TextureRect.md)
- [Tree](Tree.md)
- [VideoStreamPlayer](VideoStreamPlayer.md)

### All descendants (alphabetical)

- [AspectRatioContainer](AspectRatioContainer.md)
- [BaseButton](BaseButton.md)
- [BoxContainer](BoxContainer.md)
- [Button](Button.md)
- [CenterContainer](CenterContainer.md)
- [CheckBox](CheckBox.md)
- [CheckButton](CheckButton.md)
- [CodeEdit](CodeEdit.md)
- [ColorPicker](ColorPicker.md)
- [ColorPickerButton](ColorPickerButton.md)
- [ColorRect](ColorRect.md)
- [Container](Container.md)
- [EditorDock](EditorDock.md)
- [EditorInspector](EditorInspector.md)
- [EditorProperty](EditorProperty.md)
- [EditorResourcePicker](EditorResourcePicker.md)
- [EditorScriptPicker](EditorScriptPicker.md)
- [EditorSpinSlider](EditorSpinSlider.md)
- [EditorToaster](EditorToaster.md)
- [FileSystemDock](FileSystemDock.md)
- [FlowContainer](FlowContainer.md)
- [FoldableContainer](FoldableContainer.md)
- [GraphEdit](GraphEdit.md)
- [GraphElement](GraphElement.md)
- [GraphFrame](GraphFrame.md)
- [GraphNode](GraphNode.md)
- [GridContainer](GridContainer.md)
- [HBoxContainer](HBoxContainer.md)
- [HFlowContainer](HFlowContainer.md)
- [HScrollBar](HScrollBar.md)
- [HSeparator](HSeparator.md)
- [HSlider](HSlider.md)
- [HSplitContainer](HSplitContainer.md)
- [ItemList](ItemList.md)
- [Label](Label.md)
- [LineEdit](LineEdit.md)
- [LinkButton](LinkButton.md)
- [MarginContainer](MarginContainer.md)
- [MenuBar](MenuBar.md)
- [MenuButton](MenuButton.md)
- [NinePatchRect](NinePatchRect.md)
- [OpenXRBindingModifierEditor](OpenXRBindingModifierEditor.md)
- [OpenXRInteractionProfileEditor](OpenXRInteractionProfileEditor.md)
- [OpenXRInteractionProfileEditorBase](OpenXRInteractionProfileEditorBase.md)
- [OptionButton](OptionButton.md)
- [Panel](Panel.md)
- [PanelContainer](PanelContainer.md)
- [ProgressBar](ProgressBar.md)
- [Range](Range.md)
- [ReferenceRect](ReferenceRect.md)
- [RichTextLabel](RichTextLabel.md)
- [ScriptEditor](ScriptEditor.md)
- [ScriptEditorBase](ScriptEditorBase.md)
- [ScrollBar](ScrollBar.md)
- [ScrollContainer](ScrollContainer.md)
- [Separator](Separator.md)
- [Slider](Slider.md)
- [SpinBox](SpinBox.md)
- [SplitContainer](SplitContainer.md)
- [SubViewportContainer](SubViewportContainer.md)
- [TabBar](TabBar.md)
- [TabContainer](TabContainer.md)
- [TextEdit](TextEdit.md)
- [TextureButton](TextureButton.md)
- [TextureProgressBar](TextureProgressBar.md)
- [TextureRect](TextureRect.md)
- [Tree](Tree.md)
- [VBoxContainer](VBoxContainer.md)
- [VFlowContainer](VFlowContainer.md)
- [VScrollBar](VScrollBar.md)
- [VSeparator](VSeparator.md)
- [VSlider](VSlider.md)
- [VSplitContainer](VSplitContainer.md)
- [VideoStreamPlayer](VideoStreamPlayer.md)

## Properties

This page lists **only properties declared by `Control`**.
Inherited properties are documented in: [CanvasItem](CanvasItem.md)

| Godot Property | SML Property | Type | Default |
|-|-|-|-|
| accessibility_description | accessibilityDescription | string | — |
| accessibility_live | accessibilityLive | int | — |
| accessibility_name | accessibilityName | string | — |
| anchor_bottom | anchorBottom | float | — |
| anchor_left | anchorLeft | float | — |
| anchor_right | anchorRight | float | — |
| anchor_top | anchorTop | float | — |
| clip_contents | clipContents | bool | — |
| custom_minimum_size | customMinimumSize | Vector2 | — |
| focus_behavior_recursive | focusBehaviorRecursive | int | — |
| focus_mode | focusMode | int | — |
| grow_horizontal | growHorizontal | int | — |
| grow_vertical | growVertical | int | — |
| layout_direction | layoutDirection | int | — |
| localize_numeral_system | localizeNumeralSystem | bool | — |
| mouse_behavior_recursive | mouseBehaviorRecursive | int | — |
| mouse_default_cursor_shape | mouseDefaultCursorShape | int | — |
| mouse_filter | mouseFilter | int | — |
| mouse_force_pass_scroll_events | mouseForcePassScrollEvents | bool | — |
| offset_bottom | offsetBottom | float | — |
| offset_left | offsetLeft | float | — |
| offset_right | offsetRight | float | — |
| offset_top | offsetTop | float | — |
| pivot_offset | pivotOffset | Vector2 | — |
| pivot_offset_ratio | pivotOffsetRatio | Vector2 | — |
| position | position | Vector2 | — |
| rotation | rotation | float | — |
| scale | scale | Vector2 | — |
| size | size | Vector2 | — |
| size_flags_horizontal | sizeFlagsHorizontal | int | — |
| size_flags_stretch_ratio | sizeFlagsStretchRatio | float | — |
| size_flags_vertical | sizeFlagsVertical | int | — |
| theme_type_variation | themeTypeVariation | string | — |
| tooltip_auto_translate_mode | tooltipAutoTranslateMode | int | — |
| tooltip_text | tooltipText | string | — |

## Events

This page lists **only signals declared by `Control`**.
Inherited signals are documented in: [CanvasItem](CanvasItem.md)

| Godot Signal | SMS Event | Params |
|-|-|-|
| focus_entered | `on <id>.focusEntered() { ... }` | — |
| focus_exited | `on <id>.focusExited() { ... }` | — |
| gui_input | `on <id>.guiInput(event) { ... }` | Object event |
| minimum_size_changed | `on <id>.minimumSizeChanged() { ... }` | — |
| mouse_entered | `on <id>.mouseEntered() { ... }` | — |
| mouse_exited | `on <id>.mouseExited() { ... }` | — |
| resized | `on <id>.resized() { ... }` | — |
| size_flags_changed | `on <id>.sizeFlagsChanged() { ... }` | — |
| theme_changed | `on <id>.themeChanged() { ... }` | — |
