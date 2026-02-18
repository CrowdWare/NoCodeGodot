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
- [DockingContainer](DockingContainer.md)
- [DockingHost](DockingHost.md)
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
- [Markdown](Markdown.md)
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
- [Viewport3D](Viewport3D.md)

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

## Runtime Actions

This page lists **callable methods declared by `Control`**.
Inherited actions are documented in: [CanvasItem](CanvasItem.md)

| Godot Method | SMS Call | Params | Returns |
|-|-|-|-|
| accept_event | `<id>.acceptEvent()` | — | void |
| accessibility_drag | `<id>.accessibilityDrag()` | — | void |
| accessibility_drop | `<id>.accessibilityDrop()` | — | void |
| begin_bulk_theme_override | `<id>.beginBulkThemeOverride()` | — | void |
| end_bulk_theme_override | `<id>.endBulkThemeOverride()` | — | void |
| find_next_valid_focus | `<id>.findNextValidFocus()` | — | Object |
| find_prev_valid_focus | `<id>.findPrevValidFocus()` | — | Object |
| find_valid_focus_neighbor | `<id>.findValidFocusNeighbor(side)` | int side | Object |
| get_anchor | `<id>.getAnchor(side)` | int side | float |
| get_begin | `<id>.getBegin()` | — | Vector2 |
| get_combined_minimum_size | `<id>.getCombinedMinimumSize()` | — | Vector2 |
| get_combined_pivot_offset | `<id>.getCombinedPivotOffset()` | — | Vector2 |
| get_cursor_shape | `<id>.getCursorShape(position)` | Vector2 position | int |
| get_default_cursor_shape | `<id>.getDefaultCursorShape()` | — | int |
| get_end | `<id>.getEnd()` | — | Vector2 |
| get_focus_mode_with_override | `<id>.getFocusModeWithOverride()` | — | int |
| get_focus_neighbor | `<id>.getFocusNeighbor(side)` | int side | Variant |
| get_global_rect | `<id>.getGlobalRect()` | — | Variant |
| get_h_grow_direction | `<id>.getHGrowDirection()` | — | int |
| get_h_size_flags | `<id>.getHSizeFlags()` | — | int |
| get_minimum_size | `<id>.getMinimumSize()` | — | Vector2 |
| get_mouse_filter_with_override | `<id>.getMouseFilterWithOverride()` | — | int |
| get_offset | `<id>.getOffset(offset)` | int offset | float |
| get_parent_area_size | `<id>.getParentAreaSize()` | — | Vector2 |
| get_parent_control | `<id>.getParentControl()` | — | Object |
| get_rect | `<id>.getRect()` | — | Variant |
| get_screen_position | `<id>.getScreenPosition()` | — | Vector2 |
| get_stretch_ratio | `<id>.getStretchRatio()` | — | float |
| get_theme_default_base_scale | `<id>.getThemeDefaultBaseScale()` | — | float |
| get_theme_default_font | `<id>.getThemeDefaultFont()` | — | Object |
| get_theme_default_font_size | `<id>.getThemeDefaultFontSize()` | — | int |
| get_tooltip | `<id>.getTooltip(atPosition)` | Vector2 atPosition | string |
| get_v_grow_direction | `<id>.getVGrowDirection()` | — | int |
| get_v_size_flags | `<id>.getVSizeFlags()` | — | int |
| grab_click_focus | `<id>.grabClickFocus()` | — | void |
| grab_focus | `<id>.grabFocus(hideFocus)` | bool hideFocus | void |
| has_focus | `<id>.hasFocus(ignoreHiddenFocus)` | bool ignoreHiddenFocus | bool |
| is_auto_translating | `<id>.isAutoTranslating()` | — | bool |
| is_clipping_contents | `<id>.isClippingContents()` | — | bool |
| is_drag_successful | `<id>.isDragSuccessful()` | — | bool |
| is_force_pass_scroll_events | `<id>.isForcePassScrollEvents()` | — | bool |
| is_layout_rtl | `<id>.isLayoutRtl()` | — | bool |
| is_localizing_numeral_system | `<id>.isLocalizingNumeralSystem()` | — | bool |
| release_focus | `<id>.releaseFocus()` | — | void |
| reset_size | `<id>.resetSize()` | — | void |
| set_anchor | `<id>.setAnchor(side, anchor, keepOffset, pushOppositeAnchor)` | int side, float anchor, bool keepOffset, bool pushOppositeAnchor | void |
| set_anchor_and_offset | `<id>.setAnchorAndOffset(side, anchor, offset, pushOppositeAnchor)` | int side, float anchor, float offset, bool pushOppositeAnchor | void |
| set_anchors_and_offsets_preset | `<id>.setAnchorsAndOffsetsPreset(preset, resizeMode, margin)` | int preset, int resizeMode, int margin | void |
| set_begin | `<id>.setBegin(position)` | Vector2 position | void |
| set_default_cursor_shape | `<id>.setDefaultCursorShape(shape)` | int shape | void |
| set_end | `<id>.setEnd(position)` | Vector2 position | void |
| set_force_pass_scroll_events | `<id>.setForcePassScrollEvents(forcePassScrollEvents)` | bool forcePassScrollEvents | void |
| set_h_grow_direction | `<id>.setHGrowDirection(direction)` | int direction | void |
| set_h_size_flags | `<id>.setHSizeFlags(flags)` | int flags | void |
| set_offset | `<id>.setOffset(side, offset)` | int side, float offset | void |
| set_offsets_preset | `<id>.setOffsetsPreset(preset, resizeMode, margin)` | int preset, int resizeMode, int margin | void |
| set_stretch_ratio | `<id>.setStretchRatio(ratio)` | float ratio | void |
| set_v_grow_direction | `<id>.setVGrowDirection(direction)` | int direction | void |
| set_v_size_flags | `<id>.setVSizeFlags(flags)` | int flags | void |
| update_minimum_size | `<id>.updateMinimumSize()` | — | void |
| warp_mouse | `<id>.warpMouse(position)` | Vector2 position | void |
