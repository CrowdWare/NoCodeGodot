# Control

## Inheritance

[Control](Control.md) → [CanvasItem](CanvasItem.md) → [Node](Node.md) → [Object](Object.md)

## Collection Items

This control appears to manage internal **items** (collection-style API).
Items are typically not represented as child nodes/properties in Godot.
In SML, this will be represented via **pseudo child elements** (documented per control).

## Derived Controls

This is the entry point for all UI controls.
All classes listed below inherit from `Control`.

### Direct subclasses

- [ActionMapEditor](ActionMapEditor.md)
- [AnimationBezierTrackEdit](AnimationBezierTrackEdit.md)
- [AnimationMarkerEdit](AnimationMarkerEdit.md)
- [BaseButton](BaseButton.md)
- [CSGShapeEditor](CSGShapeEditor.md)
- [Camera2DEditor](Camera2DEditor.md)
- [CanvasItemEditorViewport](CanvasItemEditorViewport.md)
- [Cast2DEditor](Cast2DEditor.md)
- [CollisionShape2DEditor](CollisionShape2DEditor.md)
- [ColorRect](ColorRect.md)
- [Container](Container.md)
- [EditorAudioMeterNotches](EditorAudioMeterNotches.md)
- [EditorDockDragHint](EditorDockDragHint.md)
- [EditorInspectorCategory](EditorInspectorCategory.md)
- [EditorPropertyLayersGrid](EditorPropertyLayersGrid.md)
- [EmbeddedProcessBase](EmbeddedProcessBase.md)
- [GraphEdit](GraphEdit.md)
- [GraphEditFilter](GraphEditFilter.md)
- [GraphEditMinimap](GraphEditMinimap.md)
- [ItemList](ItemList.md)
- [Label](Label.md)
- [LayerHost](LayerHost.md)
- [LineEdit](LineEdit.md)
- [MenuBar](MenuBar.md)
- [MeshInstance3DEditor](MeshInstance3DEditor.md)
- [MeshLibraryEditor](MeshLibraryEditor.md)
- [MultiMeshEditor](MultiMeshEditor.md)
- [NavigationLink2DEditor](NavigationLink2DEditor.md)
- [NavigationRegion3DEditor](NavigationRegion3DEditor.md)
- [NinePatchRect](NinePatchRect.md)
- [Node3DEditorViewport](Node3DEditorViewport.md)
- [ObjectDBProfilerPanel](ObjectDBProfilerPanel.md)
- [Panel](Panel.md)
- [Range](Range.md)
- [ReferenceRect](ReferenceRect.md)
- [RichTextLabel](RichTextLabel.md)
- [SceneTreeEditor](SceneTreeEditor.md)
- [Separator](Separator.md)
- [Skeleton2DEditor](Skeleton2DEditor.md)
- [SnapshotView](SnapshotView.md)
- [SplitContainerDragger](SplitContainerDragger.md)
- [Sprite2DEditor](Sprite2DEditor.md)
- [TabBar](TabBar.md)
- [TextEdit](TextEdit.md)
- [TextureRect](TextureRect.md)
- [TileAtlasView](TileAtlasView.md)
- [Tree](Tree.md)
- [VideoStreamPlayer](VideoStreamPlayer.md)
- [ViewportNavigationControl](ViewportNavigationControl.md)
- [ViewportRotationControl](ViewportRotationControl.md)

### All descendants (alphabetical)

- [AbstractPolygon2DEditor](AbstractPolygon2DEditor.md)
- [ActionMapEditor](ActionMapEditor.md)
- [AnchorPresetPicker](AnchorPresetPicker.md)
- [AnimationBezierTrackEdit](AnimationBezierTrackEdit.md)
- [AnimationMarkerEdit](AnimationMarkerEdit.md)
- [AnimationNodeBlendSpace1DEditor](AnimationNodeBlendSpace1DEditor.md)
- [AnimationNodeBlendSpace2DEditor](AnimationNodeBlendSpace2DEditor.md)
- [AnimationNodeBlendTreeEditor](AnimationNodeBlendTreeEditor.md)
- [AnimationNodeStateMachineEditor](AnimationNodeStateMachineEditor.md)
- [AnimationPlayerEditor](AnimationPlayerEditor.md)
- [AnimationTimelineEdit](AnimationTimelineEdit.md)
- [AnimationTrackEditor](AnimationTrackEditor.md)
- [AnimationTreeEditor](AnimationTreeEditor.md)
- [AnimationTreeNodeEditorPlugin](AnimationTreeNodeEditorPlugin.md)
- [ArrayPanelContainer](ArrayPanelContainer.md)
- [AspectRatioContainer](AspectRatioContainer.md)
- [BackgroundProgress](BackgroundProgress.md)
- [BaseButton](BaseButton.md)
- [BoxContainer](BoxContainer.md)
- [Button](Button.md)
- [CSGShapeEditor](CSGShapeEditor.md)
- [Camera2DEditor](Camera2DEditor.md)
- [CanvasItemEditor](CanvasItemEditor.md)
- [CanvasItemEditorViewport](CanvasItemEditorViewport.md)
- [Cast2DEditor](Cast2DEditor.md)
- [CenterContainer](CenterContainer.md)
- [CheckBox](CheckBox.md)
- [CheckButton](CheckButton.md)
- [CodeEdit](CodeEdit.md)
- [CodeTextEditor](CodeTextEditor.md)
- [CollisionPolygon2DEditor](CollisionPolygon2DEditor.md)
- [CollisionShape2DEditor](CollisionShape2DEditor.md)
- [ColorPicker](ColorPicker.md)
- [ColorPickerButton](ColorPickerButton.md)
- [ColorRect](ColorRect.md)
- [ConnectionsDock](ConnectionsDock.md)
- [Container](Container.md)
- [ControlEditorPopupButton](ControlEditorPopupButton.md)
- [ControlEditorPresetPicker](ControlEditorPresetPicker.md)
- [ControlEditorToolbar](ControlEditorToolbar.md)
- [ControlPositioningWarning](ControlPositioningWarning.md)
- [DefaultThemeEditorPreview](DefaultThemeEditorPreview.md)
- [DockSplitContainer](DockSplitContainer.md)
- [EditorAssetLibrary](EditorAssetLibrary.md)
- [EditorAudioBus](EditorAudioBus.md)
- [EditorAudioBuses](EditorAudioBuses.md)
- [EditorAudioMeterNotches](EditorAudioMeterNotches.md)
- [EditorAutoloadSettings](EditorAutoloadSettings.md)
- [EditorBottomPanel](EditorBottomPanel.md)
- [EditorDebuggerInspector](EditorDebuggerInspector.md)
- [EditorDebuggerNode](EditorDebuggerNode.md)
- [EditorDebuggerTree](EditorDebuggerTree.md)
- [EditorDock](EditorDock.md)
- [EditorDockDragHint](EditorDockDragHint.md)
- [EditorEventSearchBar](EditorEventSearchBar.md)
- [EditorExpressionEvaluator](EditorExpressionEvaluator.md)
- [EditorHelp](EditorHelp.md)
- [EditorHelpBit](EditorHelpBit.md)
- [EditorInspector](EditorInspector.md)
- [EditorInspectorActionButton](EditorInspectorActionButton.md)
- [EditorInspectorArray](EditorInspectorArray.md)
- [EditorInspectorCategory](EditorInspectorCategory.md)
- [EditorInspectorSection](EditorInspectorSection.md)
- [EditorLog](EditorLog.md)
- [EditorMainScreen](EditorMainScreen.md)
- [EditorNetworkProfiler](EditorNetworkProfiler.md)
- [EditorObjectSelector](EditorObjectSelector.md)
- [EditorPaginator](EditorPaginator.md)
- [EditorPerformanceProfiler](EditorPerformanceProfiler.md)
- [EditorPluginSettings](EditorPluginSettings.md)
- [EditorProfiler](EditorProfiler.md)
- [EditorProperty](EditorProperty.md)
- [EditorPropertyAnchorsPreset](EditorPropertyAnchorsPreset.md)
- [EditorPropertyArray](EditorPropertyArray.md)
- [EditorPropertyCheck](EditorPropertyCheck.md)
- [EditorPropertyColor](EditorPropertyColor.md)
- [EditorPropertyEnum](EditorPropertyEnum.md)
- [EditorPropertyFlags](EditorPropertyFlags.md)
- [EditorPropertyFloat](EditorPropertyFloat.md)
- [EditorPropertyInteger](EditorPropertyInteger.md)
- [EditorPropertyLayers](EditorPropertyLayers.md)
- [EditorPropertyLayersGrid](EditorPropertyLayersGrid.md)
- [EditorPropertyLocale](EditorPropertyLocale.md)
- [EditorPropertyLocalizableString](EditorPropertyLocalizableString.md)
- [EditorPropertyMultilineText](EditorPropertyMultilineText.md)
- [EditorPropertyNodePath](EditorPropertyNodePath.md)
- [EditorPropertyPath](EditorPropertyPath.md)
- [EditorPropertyResource](EditorPropertyResource.md)
- [EditorPropertyText](EditorPropertyText.md)
- [EditorPropertyTextEnum](EditorPropertyTextEnum.md)
- [EditorPropertyVector2](EditorPropertyVector2.md)
- [EditorPropertyVector2i](EditorPropertyVector2i.md)
- [EditorPropertyVectorN](EditorPropertyVectorN.md)
- [EditorResourcePicker](EditorResourcePicker.md)
- [EditorRunBar](EditorRunBar.md)
- [EditorRunNative](EditorRunNative.md)
- [EditorSceneTabs](EditorSceneTabs.md)
- [EditorScriptPicker](EditorScriptPicker.md)
- [EditorSpinSlider](EditorSpinSlider.md)
- [EditorTitleBar](EditorTitleBar.md)
- [EditorToaster](EditorToaster.md)
- [EditorTranslationPreviewButton](EditorTranslationPreviewButton.md)
- [EditorValidationPanel](EditorValidationPanel.md)
- [EditorVariantTypeOptionButton](EditorVariantTypeOptionButton.md)
- [EditorVersionButton](EditorVersionButton.md)
- [EditorVisualProfiler](EditorVisualProfiler.md)
- [EditorZoomWidget](EditorZoomWidget.md)
- [EmbeddedProcessBase](EmbeddedProcessBase.md)
- [EmbeddedProcessMacOS](EmbeddedProcessMacOS.md)
- [EventListenerLineEdit](EventListenerLineEdit.md)
- [FileSystemDock](FileSystemDock.md)
- [FileSystemList](FileSystemList.md)
- [FindBar](FindBar.md)
- [FindInFilesContainer](FindInFilesContainer.md)
- [FindReplaceBar](FindReplaceBar.md)
- [FlowContainer](FlowContainer.md)
- [FoldableContainer](FoldableContainer.md)
- [GameView](GameView.md)
- [GraphEdit](GraphEdit.md)
- [GraphEditFilter](GraphEditFilter.md)
- [GraphEditMinimap](GraphEditMinimap.md)
- [GraphElement](GraphElement.md)
- [GraphFrame](GraphFrame.md)
- [GraphNode](GraphNode.md)
- [GridContainer](GridContainer.md)
- [GridMapEditor](GridMapEditor.md)
- [GroupSettingsEditor](GroupSettingsEditor.md)
- [GroupsDock](GroupsDock.md)
- [GroupsEditor](GroupsEditor.md)
- [HBoxContainer](HBoxContainer.md)
- [HFlowContainer](HFlowContainer.md)
- [HScrollBar](HScrollBar.md)
- [HSeparator](HSeparator.md)
- [HSlider](HSlider.md)
- [HSplitContainer](HSplitContainer.md)
- [HistoryDock](HistoryDock.md)
- [ImportDefaultsEditor](ImportDefaultsEditor.md)
- [ImportDock](ImportDock.md)
- [InspectorDock](InspectorDock.md)
- [ItemList](ItemList.md)
- [Label](Label.md)
- [LayerHost](LayerHost.md)
- [LightOccluder2DEditor](LightOccluder2DEditor.md)
- [Line2DEditor](Line2DEditor.md)
- [LineEdit](LineEdit.md)
- [LinkButton](LinkButton.md)
- [LocalizationEditor](LocalizationEditor.md)
- [MarginContainer](MarginContainer.md)
- [MenuBar](MenuBar.md)
- [MenuButton](MenuButton.md)
- [MeshInstance3DEditor](MeshInstance3DEditor.md)
- [MeshLibraryEditor](MeshLibraryEditor.md)
- [MultiMeshEditor](MultiMeshEditor.md)
- [NavigationLink2DEditor](NavigationLink2DEditor.md)
- [NavigationObstacle2DEditor](NavigationObstacle2DEditor.md)
- [NavigationRegion2DEditor](NavigationRegion2DEditor.md)
- [NavigationRegion3DEditor](NavigationRegion3DEditor.md)
- [NinePatchRect](NinePatchRect.md)
- [Node3DEditor](Node3DEditor.md)
- [Node3DEditorViewport](Node3DEditorViewport.md)
- [Node3DEditorViewportContainer](Node3DEditorViewportContainer.md)
- [ObjectDBProfilerPanel](ObjectDBProfilerPanel.md)
- [OpenXRBindingModifierEditor](OpenXRBindingModifierEditor.md)
- [OpenXRInteractionProfileEditor](OpenXRInteractionProfileEditor.md)
- [OpenXRInteractionProfileEditorBase](OpenXRInteractionProfileEditorBase.md)
- [OptionButton](OptionButton.md)
- [Panel](Panel.md)
- [PanelContainer](PanelContainer.md)
- [Path2DEditor](Path2DEditor.md)
- [Polygon2DEditor](Polygon2DEditor.md)
- [Polygon3DEditor](Polygon3DEditor.md)
- [ProgressBar](ProgressBar.md)
- [ProgressDialog](ProgressDialog.md)
- [ProjectExportTextureFormatError](ProjectExportTextureFormatError.md)
- [QuickOpenResultContainer](QuickOpenResultContainer.md)
- [Range](Range.md)
- [ReferenceRect](ReferenceRect.md)
- [ReplicationEditor](ReplicationEditor.md)
- [ResourcePreloaderEditor](ResourcePreloaderEditor.md)
- [RichTextLabel](RichTextLabel.md)
- [SceneTreeDock](SceneTreeDock.md)
- [SceneTreeEditor](SceneTreeEditor.md)
- [ScreenSelect](ScreenSelect.md)
- [ScriptEditor](ScriptEditor.md)
- [ScriptEditorBase](ScriptEditorBase.md)
- [ScriptEditorDebugger](ScriptEditorDebugger.md)
- [ScriptTextEditor](ScriptTextEditor.md)
- [ScrollBar](ScrollBar.md)
- [ScrollContainer](ScrollContainer.md)
- [SectionedInspector](SectionedInspector.md)
- [Separator](Separator.md)
- [ShaderFileEditor](ShaderFileEditor.md)
- [ShaderGlobalsEditor](ShaderGlobalsEditor.md)
- [SignalsDock](SignalsDock.md)
- [SizeFlagPresetPicker](SizeFlagPresetPicker.md)
- [Skeleton2DEditor](Skeleton2DEditor.md)
- [Slider](Slider.md)
- [SnapshotClassView](SnapshotClassView.md)
- [SnapshotNodeView](SnapshotNodeView.md)
- [SnapshotObjectView](SnapshotObjectView.md)
- [SnapshotRefCountedView](SnapshotRefCountedView.md)
- [SnapshotSummaryView](SnapshotSummaryView.md)
- [SnapshotView](SnapshotView.md)
- [SpinBox](SpinBox.md)
- [SpinBoxLineEdit](SpinBoxLineEdit.md)
- [SplitContainer](SplitContainer.md)
- [SplitContainerDragger](SplitContainerDragger.md)
- [Sprite2DEditor](Sprite2DEditor.md)
- [SpriteFramesEditor](SpriteFramesEditor.md)
- [SubViewportContainer](SubViewportContainer.md)
- [SwitchSeparator](SwitchSeparator.md)
- [TabBar](TabBar.md)
- [TabContainer](TabContainer.md)
- [TextEdit](TextEdit.md)
- [TextEditor](TextEditor.md)
- [TextureButton](TextureButton.md)
- [TextureProgressBar](TextureProgressBar.md)
- [TextureRect](TextureRect.md)
- [ThemeEditor](ThemeEditor.md)
- [ThemeEditorPreview](ThemeEditorPreview.md)
- [ThemeItemImportTree](ThemeItemImportTree.md)
- [ThemeTypeEditor](ThemeTypeEditor.md)
- [TileAtlasView](TileAtlasView.md)
- [TileMapLayerEditor](TileMapLayerEditor.md)
- [TileSetAtlasSourceEditor](TileSetAtlasSourceEditor.md)
- [TileSetEditor](TileSetEditor.md)
- [TileSetScenesCollectionSourceEditor](TileSetScenesCollectionSourceEditor.md)
- [TileSetSourceItemList](TileSetSourceItemList.md)
- [Tree](Tree.md)
- [VBoxContainer](VBoxContainer.md)
- [VFlowContainer](VFlowContainer.md)
- [VScrollBar](VScrollBar.md)
- [VSeparator](VSeparator.md)
- [VSlider](VSlider.md)
- [VSplitContainer](VSplitContainer.md)
- [VideoStreamPlayer](VideoStreamPlayer.md)
- [ViewportNavigationControl](ViewportNavigationControl.md)
- [ViewportRotationControl](ViewportRotationControl.md)
- [WindowWrapper](WindowWrapper.md)

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
