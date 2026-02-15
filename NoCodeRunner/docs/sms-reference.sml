Reference {
    generator: "generate_sml_element_docs.gd"
    propertyFilter: "inspector-editable + supported scalar types"
    naming: "snake_case -> lowerCamelCase"

    Type {
        name: "AbstractPolygon2DEditor"
        parent: "HBoxContainer"
        collection: true

        Properties {
        }

        Events {
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "ActionMapEditor"
        parent: "Control"
        collection: true

        Properties {
        }

        Events {
            Event { godot: "action_added"; sms: "actionAdded"; params: "string name" }
            Event { godot: "action_edited"; sms: "actionEdited"; params: "string name, Variant newAction" }
            Event { godot: "action_removed"; sms: "actionRemoved"; params: "string name" }
            Event { godot: "action_renamed"; sms: "actionRenamed"; params: "string oldName, string newName" }
            Event { godot: "action_reordered"; sms: "actionReordered"; params: "string actionName, string relativeTo, bool before" }
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "AnchorPresetPicker"
        parent: "ControlEditorPresetPicker"
        collection: true

        Properties {
        }

        Events {
            Event { godot: "anchors_preset_selected"; sms: "anchorsPresetSelected"; params: "int preset" }
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "AnimationBezierTrackEdit"
        parent: "Control"
        collection: true

        Properties {
        }

        Events {
            Event { godot: "clear_selection"; sms: "clearSelection"; params: "—" }
            Event { godot: "deselect_key"; sms: "deselectKey"; params: "int index, int track" }
            Event { godot: "select_key"; sms: "selectKey"; params: "int index, bool single, int track" }
            Event { godot: "timeline_changed"; sms: "timelineChanged"; params: "float position, bool timelineOnly" }
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "AnimationMarkerEdit"
        parent: "Control"
        collection: true

        Properties {
        }

        Events {
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "AnimationNodeBlendSpace1DEditor"
        parent: "AnimationTreeNodeEditorPlugin"
        collection: true

        Properties {
        }

        Events {
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "AnimationNodeBlendSpace2DEditor"
        parent: "AnimationTreeNodeEditorPlugin"
        collection: true

        Properties {
        }

        Events {
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "AnimationNodeBlendTreeEditor"
        parent: "AnimationTreeNodeEditorPlugin"
        collection: true

        Properties {
        }

        Events {
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "AnimationNodeStateMachineEditor"
        parent: "AnimationTreeNodeEditorPlugin"
        collection: true

        Properties {
        }

        Events {
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "AnimationPlayerEditor"
        parent: "EditorDock"
        collection: true

        Properties {
        }

        Events {
            Event { godot: "animation_selected"; sms: "animationSelected"; params: "string name" }
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "AnimationTimelineEdit"
        parent: "Range"
        collection: true

        Properties {
        }

        Events {
            Event { godot: "filter_changed"; sms: "filterChanged"; params: "—" }
            Event { godot: "length_changed"; sms: "lengthChanged"; params: "float size" }
            Event { godot: "name_limit_changed"; sms: "nameLimitChanged"; params: "—" }
            Event { godot: "timeline_changed"; sms: "timelineChanged"; params: "float position, bool timelineOnly" }
            Event { godot: "track_added"; sms: "trackAdded"; params: "int track" }
            Event { godot: "zoom_changed"; sms: "zoomChanged"; params: "—" }
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "AnimationTrackEditor"
        parent: "VBoxContainer"
        collection: true

        Properties {
        }

        Events {
            Event { godot: "animation_len_changed"; sms: "animationLenChanged"; params: "float len" }
            Event { godot: "animation_step_changed"; sms: "animationStepChanged"; params: "float step" }
            Event { godot: "keying_changed"; sms: "keyingChanged"; params: "—" }
            Event { godot: "timeline_changed"; sms: "timelineChanged"; params: "float position, bool timelineOnly, bool updatePositionOnly" }
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "AnimationTreeEditor"
        parent: "EditorDock"
        collection: true

        Properties {
        }

        Events {
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "AnimationTreeNodeEditorPlugin"
        parent: "VBoxContainer"
        collection: true

        Properties {
        }

        Events {
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "ArrayPanelContainer"
        parent: "PanelContainer"
        collection: true

        Properties {
        }

        Events {
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "AspectRatioContainer"
        parent: "Container"
        collection: true

        Properties {
            Prop { godot: "alignment_horizontal"; sml: "alignmentHorizontal"; type: "int" }
            Prop { godot: "alignment_vertical"; sml: "alignmentVertical"; type: "int" }
            Prop { godot: "ratio"; sml: "ratio"; type: "float" }
            Prop { godot: "stretch_mode"; sml: "stretchMode"; type: "int" }
        }

        Events {
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "BackgroundProgress"
        parent: "HBoxContainer"
        collection: true

        Properties {
        }

        Events {
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "BaseButton"
        parent: "Control"
        collection: true

        Properties {
            Prop { godot: "action_mode"; sml: "actionMode"; type: "int" }
            Prop { godot: "button_mask"; sml: "buttonMask"; type: "int" }
            Prop { godot: "button_pressed"; sml: "buttonPressed"; type: "bool" }
            Prop { godot: "disabled"; sml: "disabled"; type: "bool" }
            Prop { godot: "keep_pressed_outside"; sml: "keepPressedOutside"; type: "bool" }
            Prop { godot: "shortcut_feedback"; sml: "shortcutFeedback"; type: "bool" }
            Prop { godot: "shortcut_in_tooltip"; sml: "shortcutInTooltip"; type: "bool" }
            Prop { godot: "toggle_mode"; sml: "toggleMode"; type: "bool" }
        }

        Events {
            Event { godot: "button_down"; sms: "buttonDown"; params: "—" }
            Event { godot: "button_up"; sms: "buttonUp"; params: "—" }
            Event { godot: "pressed"; sms: "pressed"; params: "—" }
            Event { godot: "toggled"; sms: "toggled"; params: "bool toggledOn" }
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "BoxContainer"
        parent: "Container"
        collection: true

        Properties {
            Prop { godot: "alignment"; sml: "alignment"; type: "int" }
            Prop { godot: "vertical"; sml: "vertical"; type: "bool" }
        }

        Events {
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "Button"
        parent: "BaseButton"
        collection: true

        Properties {
            Prop { godot: "alignment"; sml: "alignment"; type: "int" }
            Prop { godot: "autowrap_mode"; sml: "autowrapMode"; type: "int" }
            Prop { godot: "autowrap_trim_flags"; sml: "autowrapTrimFlags"; type: "int" }
            Prop { godot: "clip_text"; sml: "clipText"; type: "bool" }
            Prop { godot: "expand_icon"; sml: "expandIcon"; type: "bool" }
            Prop { godot: "flat"; sml: "flat"; type: "bool" }
            Prop { godot: "icon_alignment"; sml: "iconAlignment"; type: "int" }
            Prop { godot: "language"; sml: "language"; type: "string" }
            Prop { godot: "text"; sml: "text"; type: "string" }
            Prop { godot: "text_direction"; sml: "textDirection"; type: "int" }
            Prop { godot: "text_overrun_behavior"; sml: "textOverrunBehavior"; type: "int" }
            Prop { godot: "vertical_icon_alignment"; sml: "verticalIconAlignment"; type: "int" }
        }

        Events {
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "CSGShapeEditor"
        parent: "Control"
        collection: true

        Properties {
        }

        Events {
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "Camera2DEditor"
        parent: "Control"
        collection: true

        Properties {
        }

        Events {
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "CanvasItem"
        parent: "Node"
        collection: true

        Properties {
            Prop { godot: "clip_children"; sml: "clipChildren"; type: "int" }
            Prop { godot: "light_mask"; sml: "lightMask"; type: "int" }
            Prop { godot: "modulate"; sml: "modulate"; type: "Color" }
            Prop { godot: "self_modulate"; sml: "selfModulate"; type: "Color" }
            Prop { godot: "show_behind_parent"; sml: "showBehindParent"; type: "bool" }
            Prop { godot: "texture_filter"; sml: "textureFilter"; type: "int" }
            Prop { godot: "texture_repeat"; sml: "textureRepeat"; type: "int" }
            Prop { godot: "top_level"; sml: "topLevel"; type: "bool" }
            Prop { godot: "use_parent_material"; sml: "useParentMaterial"; type: "bool" }
            Prop { godot: "visibility_layer"; sml: "visibilityLayer"; type: "int" }
            Prop { godot: "visible"; sml: "visible"; type: "bool" }
            Prop { godot: "y_sort_enabled"; sml: "ySortEnabled"; type: "bool" }
            Prop { godot: "z_as_relative"; sml: "zAsRelative"; type: "bool" }
            Prop { godot: "z_index"; sml: "zIndex"; type: "int" }
        }

        Events {
            Event { godot: "draw"; sms: "draw"; params: "—" }
            Event { godot: "hidden"; sms: "hidden"; params: "—" }
            Event { godot: "item_rect_changed"; sms: "itemRectChanged"; params: "—" }
            Event { godot: "visibility_changed"; sms: "visibilityChanged"; params: "—" }
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "CanvasItemEditor"
        parent: "VBoxContainer"
        collection: true

        Properties {
        }

        Events {
            Event { godot: "item_group_status_changed"; sms: "itemGroupStatusChanged"; params: "—" }
            Event { godot: "item_lock_status_changed"; sms: "itemLockStatusChanged"; params: "—" }
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "CanvasItemEditorViewport"
        parent: "Control"
        collection: true

        Properties {
        }

        Events {
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "Cast2DEditor"
        parent: "Control"
        collection: true

        Properties {
        }

        Events {
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "CenterContainer"
        parent: "Container"
        collection: true

        Properties {
            Prop { godot: "use_top_left"; sml: "useTopLeft"; type: "bool" }
        }

        Events {
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "CheckBox"
        parent: "Button"
        collection: true

        Properties {
        }

        Events {
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "CheckButton"
        parent: "Button"
        collection: true

        Properties {
        }

        Events {
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "CodeEdit"
        parent: "TextEdit"
        collection: true

        Properties {
            Prop { godot: "auto_brace_completion_enabled"; sml: "autoBraceCompletionEnabled"; type: "bool" }
            Prop { godot: "auto_brace_completion_highlight_matching"; sml: "autoBraceCompletionHighlightMatching"; type: "bool" }
            Prop { godot: "code_completion_enabled"; sml: "codeCompletionEnabled"; type: "bool" }
            Prop { godot: "gutters_draw_bookmarks"; sml: "guttersDrawBookmarks"; type: "bool" }
            Prop { godot: "gutters_draw_breakpoints_gutter"; sml: "guttersDrawBreakpointsGutter"; type: "bool" }
            Prop { godot: "gutters_draw_executing_lines"; sml: "guttersDrawExecutingLines"; type: "bool" }
            Prop { godot: "gutters_draw_fold_gutter"; sml: "guttersDrawFoldGutter"; type: "bool" }
            Prop { godot: "gutters_draw_line_numbers"; sml: "guttersDrawLineNumbers"; type: "bool" }
            Prop { godot: "gutters_line_numbers_min_digits"; sml: "guttersLineNumbersMinDigits"; type: "int" }
            Prop { godot: "gutters_zero_pad_line_numbers"; sml: "guttersZeroPadLineNumbers"; type: "bool" }
            Prop { godot: "indent_automatic"; sml: "indentAutomatic"; type: "bool" }
            Prop { godot: "indent_size"; sml: "indentSize"; type: "int" }
            Prop { godot: "indent_use_spaces"; sml: "indentUseSpaces"; type: "bool" }
            Prop { godot: "line_folding"; sml: "lineFolding"; type: "bool" }
            Prop { godot: "symbol_lookup_on_click"; sml: "symbolLookupOnClick"; type: "bool" }
            Prop { godot: "symbol_tooltip_on_hover"; sml: "symbolTooltipOnHover"; type: "bool" }
        }

        Events {
            Event { godot: "breakpoint_toggled"; sms: "breakpointToggled"; params: "int line" }
            Event { godot: "code_completion_requested"; sms: "codeCompletionRequested"; params: "—" }
            Event { godot: "symbol_hovered"; sms: "symbolHovered"; params: "string symbol, int line, int column" }
            Event { godot: "symbol_lookup"; sms: "symbolLookup"; params: "string symbol, int line, int column" }
            Event { godot: "symbol_validate"; sms: "symbolValidate"; params: "string symbol" }
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "CodeTextEditor"
        parent: "VBoxContainer"
        collection: true

        Properties {
        }

        Events {
            Event { godot: "load_theme_settings"; sms: "loadThemeSettings"; params: "—" }
            Event { godot: "navigation_preview_ended"; sms: "navigationPreviewEnded"; params: "—" }
            Event { godot: "show_errors_panel"; sms: "showErrorsPanel"; params: "—" }
            Event { godot: "show_warnings_panel"; sms: "showWarningsPanel"; params: "—" }
            Event { godot: "validate_script"; sms: "validateScript"; params: "—" }
            Event { godot: "zoomed"; sms: "zoomed"; params: "float pZoomFactor" }
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "CollisionPolygon2DEditor"
        parent: "AbstractPolygon2DEditor"
        collection: true

        Properties {
        }

        Events {
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "CollisionShape2DEditor"
        parent: "Control"
        collection: true

        Properties {
        }

        Events {
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "ColorPicker"
        parent: "VBoxContainer"
        collection: true

        Properties {
            Prop { godot: "can_add_swatches"; sml: "canAddSwatches"; type: "bool" }
            Prop { godot: "color"; sml: "color"; type: "Color" }
            Prop { godot: "color_mode"; sml: "colorMode"; type: "int" }
            Prop { godot: "color_modes_visible"; sml: "colorModesVisible"; type: "bool" }
            Prop { godot: "deferred_mode"; sml: "deferredMode"; type: "bool" }
            Prop { godot: "edit_alpha"; sml: "editAlpha"; type: "bool" }
            Prop { godot: "edit_intensity"; sml: "editIntensity"; type: "bool" }
            Prop { godot: "hex_visible"; sml: "hexVisible"; type: "bool" }
            Prop { godot: "picker_shape"; sml: "pickerShape"; type: "int" }
            Prop { godot: "presets_visible"; sml: "presetsVisible"; type: "bool" }
            Prop { godot: "sampler_visible"; sml: "samplerVisible"; type: "bool" }
            Prop { godot: "sliders_visible"; sml: "slidersVisible"; type: "bool" }
        }

        Events {
            Event { godot: "color_changed"; sms: "colorChanged"; params: "Color color" }
            Event { godot: "preset_added"; sms: "presetAdded"; params: "Color color" }
            Event { godot: "preset_removed"; sms: "presetRemoved"; params: "Color color" }
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "ColorPickerButton"
        parent: "Button"
        collection: true

        Properties {
            Prop { godot: "color"; sml: "color"; type: "Color" }
            Prop { godot: "edit_alpha"; sml: "editAlpha"; type: "bool" }
            Prop { godot: "edit_intensity"; sml: "editIntensity"; type: "bool" }
        }

        Events {
            Event { godot: "color_changed"; sms: "colorChanged"; params: "Color color" }
            Event { godot: "picker_created"; sms: "pickerCreated"; params: "—" }
            Event { godot: "popup_closed"; sms: "popupClosed"; params: "—" }
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "ColorRect"
        parent: "Control"
        collection: true

        Properties {
            Prop { godot: "color"; sml: "color"; type: "Color" }
        }

        Events {
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "ConnectionsDock"
        parent: "VBoxContainer"
        collection: true

        Properties {
        }

        Events {
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "Container"
        parent: "Control"
        collection: true

        Properties {
        }

        Events {
            Event { godot: "pre_sort_children"; sms: "preSortChildren"; params: "—" }
            Event { godot: "sort_children"; sms: "sortChildren"; params: "—" }
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "Control"
        parent: "CanvasItem"
        collection: true

        Properties {
            Prop { godot: "accessibility_description"; sml: "accessibilityDescription"; type: "string" }
            Prop { godot: "accessibility_live"; sml: "accessibilityLive"; type: "int" }
            Prop { godot: "accessibility_name"; sml: "accessibilityName"; type: "string" }
            Prop { godot: "anchor_bottom"; sml: "anchorBottom"; type: "float" }
            Prop { godot: "anchor_left"; sml: "anchorLeft"; type: "float" }
            Prop { godot: "anchor_right"; sml: "anchorRight"; type: "float" }
            Prop { godot: "anchor_top"; sml: "anchorTop"; type: "float" }
            Prop { godot: "clip_contents"; sml: "clipContents"; type: "bool" }
            Prop { godot: "custom_minimum_size"; sml: "customMinimumSize"; type: "Vector2" }
            Prop { godot: "focus_behavior_recursive"; sml: "focusBehaviorRecursive"; type: "int" }
            Prop { godot: "focus_mode"; sml: "focusMode"; type: "int" }
            Prop { godot: "grow_horizontal"; sml: "growHorizontal"; type: "int" }
            Prop { godot: "grow_vertical"; sml: "growVertical"; type: "int" }
            Prop { godot: "layout_direction"; sml: "layoutDirection"; type: "int" }
            Prop { godot: "localize_numeral_system"; sml: "localizeNumeralSystem"; type: "bool" }
            Prop { godot: "mouse_behavior_recursive"; sml: "mouseBehaviorRecursive"; type: "int" }
            Prop { godot: "mouse_default_cursor_shape"; sml: "mouseDefaultCursorShape"; type: "int" }
            Prop { godot: "mouse_filter"; sml: "mouseFilter"; type: "int" }
            Prop { godot: "mouse_force_pass_scroll_events"; sml: "mouseForcePassScrollEvents"; type: "bool" }
            Prop { godot: "offset_bottom"; sml: "offsetBottom"; type: "float" }
            Prop { godot: "offset_left"; sml: "offsetLeft"; type: "float" }
            Prop { godot: "offset_right"; sml: "offsetRight"; type: "float" }
            Prop { godot: "offset_top"; sml: "offsetTop"; type: "float" }
            Prop { godot: "pivot_offset"; sml: "pivotOffset"; type: "Vector2" }
            Prop { godot: "pivot_offset_ratio"; sml: "pivotOffsetRatio"; type: "Vector2" }
            Prop { godot: "position"; sml: "position"; type: "Vector2" }
            Prop { godot: "rotation"; sml: "rotation"; type: "float" }
            Prop { godot: "scale"; sml: "scale"; type: "Vector2" }
            Prop { godot: "size"; sml: "size"; type: "Vector2" }
            Prop { godot: "size_flags_horizontal"; sml: "sizeFlagsHorizontal"; type: "int" }
            Prop { godot: "size_flags_stretch_ratio"; sml: "sizeFlagsStretchRatio"; type: "float" }
            Prop { godot: "size_flags_vertical"; sml: "sizeFlagsVertical"; type: "int" }
            Prop { godot: "theme_type_variation"; sml: "themeTypeVariation"; type: "string" }
            Prop { godot: "tooltip_auto_translate_mode"; sml: "tooltipAutoTranslateMode"; type: "int" }
            Prop { godot: "tooltip_text"; sml: "tooltipText"; type: "string" }
        }

        Events {
            Event { godot: "focus_entered"; sms: "focusEntered"; params: "—" }
            Event { godot: "focus_exited"; sms: "focusExited"; params: "—" }
            Event { godot: "gui_input"; sms: "guiInput"; params: "Object event" }
            Event { godot: "minimum_size_changed"; sms: "minimumSizeChanged"; params: "—" }
            Event { godot: "mouse_entered"; sms: "mouseEntered"; params: "—" }
            Event { godot: "mouse_exited"; sms: "mouseExited"; params: "—" }
            Event { godot: "resized"; sms: "resized"; params: "—" }
            Event { godot: "size_flags_changed"; sms: "sizeFlagsChanged"; params: "—" }
            Event { godot: "theme_changed"; sms: "themeChanged"; params: "—" }
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "ControlEditorPopupButton"
        parent: "Button"
        collection: true

        Properties {
        }

        Events {
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "ControlEditorPresetPicker"
        parent: "MarginContainer"
        collection: true

        Properties {
        }

        Events {
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "ControlEditorToolbar"
        parent: "HBoxContainer"
        collection: true

        Properties {
        }

        Events {
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "ControlPositioningWarning"
        parent: "MarginContainer"
        collection: true

        Properties {
        }

        Events {
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "DefaultThemeEditorPreview"
        parent: "ThemeEditorPreview"
        collection: true

        Properties {
        }

        Events {
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "DockSplitContainer"
        parent: "SplitContainer"
        collection: true

        Properties {
        }

        Events {
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "EditorAssetLibrary"
        parent: "PanelContainer"
        collection: true

        Properties {
        }

        Events {
            Event { godot: "install_asset"; sms: "installAsset"; params: "string zipPath, string name" }
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "EditorAudioBus"
        parent: "PanelContainer"
        collection: true

        Properties {
        }

        Events {
            Event { godot: "delete_request"; sms: "deleteRequest"; params: "—" }
            Event { godot: "drop_end_request"; sms: "dropEndRequest"; params: "—" }
            Event { godot: "dropped"; sms: "dropped"; params: "—" }
            Event { godot: "duplicate_request"; sms: "duplicateRequest"; params: "—" }
            Event { godot: "vol_reset_request"; sms: "volResetRequest"; params: "—" }
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "EditorAudioBuses"
        parent: "EditorDock"
        collection: true

        Properties {
        }

        Events {
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "EditorAudioMeterNotches"
        parent: "Control"
        collection: true

        Properties {
        }

        Events {
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "EditorAutoloadSettings"
        parent: "VBoxContainer"
        collection: true

        Properties {
        }

        Events {
            Event { godot: "autoload_changed"; sms: "autoloadChanged"; params: "—" }
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "EditorBottomPanel"
        parent: "TabContainer"
        collection: true

        Properties {
        }

        Events {
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "EditorDebuggerInspector"
        parent: "EditorInspector"
        collection: true

        Properties {
        }

        Events {
            Event { godot: "object_property_updated"; sms: "objectPropertyUpdated"; params: "int id, string property" }
            Event { godot: "object_selected"; sms: "objectSelected"; params: "int id" }
            Event { godot: "objects_edited"; sms: "objectsEdited"; params: "Variant ids, string property, Object , string field" }
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "EditorDebuggerNode"
        parent: "EditorDock"
        collection: true

        Properties {
        }

        Events {
            Event { godot: "breaked"; sms: "breaked"; params: "bool reallydid, bool canDebug" }
            Event { godot: "breakpoint_set_in_tree"; sms: "breakpointSetInTree"; params: "Object , int line, bool enabled, int debugger" }
            Event { godot: "breakpoint_toggled"; sms: "breakpointToggled"; params: "string path, int line, bool enabled" }
            Event { godot: "breakpoints_cleared_in_tree"; sms: "breakpointsClearedInTree"; params: "int debugger" }
            Event { godot: "clear_execution"; sms: "clearExecution"; params: "Object " }
            Event { godot: "goto_script_line"; sms: "gotoScriptLine"; params: "—" }
            Event { godot: "set_execution"; sms: "setExecution"; params: "Object , int line" }
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "EditorDebuggerTree"
        parent: "Tree"
        collection: true

        Properties {
        }

        Events {
            Event { godot: "objects_selected"; sms: "objectsSelected"; params: "Variant objectIds, int debugger" }
            Event { godot: "open"; sms: "open"; params: "—" }
            Event { godot: "save_node"; sms: "saveNode"; params: "int objectId, string filename, int debugger" }
            Event { godot: "selection_cleared"; sms: "selectionCleared"; params: "int debugger" }
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "EditorDock"
        parent: "MarginContainer"
        collection: true

        Properties {
            Prop { godot: "available_layouts"; sml: "availableLayouts"; type: "int" }
            Prop { godot: "closable"; sml: "closable"; type: "bool" }
            Prop { godot: "default_slot"; sml: "defaultSlot"; type: "int" }
            Prop { godot: "force_show_icon"; sml: "forceShowIcon"; type: "bool" }
            Prop { godot: "global"; sml: "global"; type: "bool" }
            Prop { godot: "layout_key"; sml: "layoutKey"; type: "string" }
            Prop { godot: "title"; sml: "title"; type: "string" }
            Prop { godot: "title_color"; sml: "titleColor"; type: "Color" }
            Prop { godot: "transient"; sml: "transient"; type: "bool" }
        }

        Events {
            Event { godot: "_tab_style_changed"; sms: "TabStyleChanged"; params: "—" }
            Event { godot: "closed"; sms: "closed"; params: "—" }
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "EditorDockDragHint"
        parent: "Control"
        collection: true

        Properties {
        }

        Events {
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "EditorEventSearchBar"
        parent: "HBoxContainer"
        collection: true

        Properties {
        }

        Events {
            Event { godot: "value_changed"; sms: "valueChanged"; params: "—" }
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "EditorExpressionEvaluator"
        parent: "VBoxContainer"
        collection: true

        Properties {
        }

        Events {
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "EditorHelp"
        parent: "VBoxContainer"
        collection: true

        Properties {
        }

        Events {
            Event { godot: "go_to_help"; sms: "goToHelp"; params: "—" }
            Event { godot: "request_save_history"; sms: "requestSaveHistory"; params: "—" }
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "EditorHelpBit"
        parent: "VBoxContainer"
        collection: true

        Properties {
        }

        Events {
            Event { godot: "request_hide"; sms: "requestHide"; params: "—" }
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "EditorInspector"
        parent: "ScrollContainer"
        collection: true

        Properties {
        }

        Events {
            Event { godot: "edited_object_changed"; sms: "editedObjectChanged"; params: "—" }
            Event { godot: "object_id_selected"; sms: "objectIdSelected"; params: "int id" }
            Event { godot: "property_deleted"; sms: "propertyDeleted"; params: "string property" }
            Event { godot: "property_edited"; sms: "propertyEdited"; params: "string property" }
            Event { godot: "property_keyed"; sms: "propertyKeyed"; params: "string property, Variant value, bool advance" }
            Event { godot: "property_selected"; sms: "propertySelected"; params: "string property" }
            Event { godot: "property_toggled"; sms: "propertyToggled"; params: "string property, bool checked" }
            Event { godot: "resource_selected"; sms: "resourceSelected"; params: "Object resource, string path" }
            Event { godot: "restart_requested"; sms: "restartRequested"; params: "—" }
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "EditorInspectorActionButton"
        parent: "Button"
        collection: true

        Properties {
        }

        Events {
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "EditorInspectorArray"
        parent: "EditorInspectorSection"
        collection: true

        Properties {
        }

        Events {
            Event { godot: "page_change_request"; sms: "pageChangeRequest"; params: "—" }
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "EditorInspectorCategory"
        parent: "Control"
        collection: true

        Properties {
        }

        Events {
            Event { godot: "unfavorite_all"; sms: "unfavoriteAll"; params: "—" }
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "EditorInspectorSection"
        parent: "Container"
        collection: true

        Properties {
        }

        Events {
            Event { godot: "property_keyed"; sms: "propertyKeyed"; params: "Variant property" }
            Event { godot: "section_toggled_by_user"; sms: "sectionToggledByUser"; params: "Variant property, bool value" }
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "EditorLog"
        parent: "EditorDock"
        collection: true

        Properties {
        }

        Events {
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "EditorMainScreen"
        parent: "PanelContainer"
        collection: true

        Properties {
        }

        Events {
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "EditorNetworkProfiler"
        parent: "VBoxContainer"
        collection: true

        Properties {
        }

        Events {
            Event { godot: "enable_profiling"; sms: "enableProfiling"; params: "bool enable" }
            Event { godot: "open_request"; sms: "openRequest"; params: "string path" }
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "EditorObjectSelector"
        parent: "Button"
        collection: true

        Properties {
        }

        Events {
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "EditorPaginator"
        parent: "HBoxContainer"
        collection: true

        Properties {
        }

        Events {
            Event { godot: "page_changed"; sms: "pageChanged"; params: "int page" }
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "EditorPerformanceProfiler"
        parent: "HSplitContainer"
        collection: true

        Properties {
        }

        Events {
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "EditorPluginSettings"
        parent: "VBoxContainer"
        collection: true

        Properties {
        }

        Events {
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "EditorProfiler"
        parent: "VBoxContainer"
        collection: true

        Properties {
        }

        Events {
            Event { godot: "break_request"; sms: "breakRequest"; params: "—" }
            Event { godot: "enable_profiling"; sms: "enableProfiling"; params: "bool enable" }
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "EditorProperty"
        parent: "Container"
        collection: true

        Properties {
            Prop { godot: "checkable"; sml: "checkable"; type: "bool" }
            Prop { godot: "checked"; sml: "checked"; type: "bool" }
            Prop { godot: "deletable"; sml: "deletable"; type: "bool" }
            Prop { godot: "draw_background"; sml: "drawBackground"; type: "bool" }
            Prop { godot: "draw_label"; sml: "drawLabel"; type: "bool" }
            Prop { godot: "draw_warning"; sml: "drawWarning"; type: "bool" }
            Prop { godot: "keying"; sml: "keying"; type: "bool" }
            Prop { godot: "label"; sml: "label"; type: "string" }
            Prop { godot: "name_split_ratio"; sml: "nameSplitRatio"; type: "float" }
            Prop { godot: "read_only"; sml: "readOnly"; type: "bool" }
            Prop { godot: "selectable"; sml: "selectable"; type: "bool" }
            Prop { godot: "use_folding"; sml: "useFolding"; type: "bool" }
        }

        Events {
            Event { godot: "multiple_properties_changed"; sms: "multiplePropertiesChanged"; params: "Variant properties, Variant value" }
            Event { godot: "object_id_selected"; sms: "objectIdSelected"; params: "Variant property, int id" }
            Event { godot: "property_can_revert_changed"; sms: "propertyCanRevertChanged"; params: "Variant property, bool canRevert" }
            Event { godot: "property_changed"; sms: "propertyChanged"; params: "Variant property, Variant value, Variant field, bool changing" }
            Event { godot: "property_checked"; sms: "propertyChecked"; params: "Variant property, bool checked" }
            Event { godot: "property_deleted"; sms: "propertyDeleted"; params: "Variant property" }
            Event { godot: "property_favorited"; sms: "propertyFavorited"; params: "Variant property, bool favorited" }
            Event { godot: "property_keyed"; sms: "propertyKeyed"; params: "Variant property" }
            Event { godot: "property_keyed_with_value"; sms: "propertyKeyedWithValue"; params: "Variant property, Variant value" }
            Event { godot: "property_overridden"; sms: "propertyOverridden"; params: "—" }
            Event { godot: "property_pinned"; sms: "propertyPinned"; params: "Variant property, bool pinned" }
            Event { godot: "resource_selected"; sms: "resourceSelected"; params: "string path, Object resource" }
            Event { godot: "selected"; sms: "selected"; params: "string path, int focusableIdx" }
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "EditorPropertyAnchorsPreset"
        parent: "EditorProperty"
        collection: true

        Properties {
        }

        Events {
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "EditorPropertyArray"
        parent: "EditorProperty"
        collection: true

        Properties {
        }

        Events {
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "EditorPropertyCheck"
        parent: "EditorProperty"
        collection: true

        Properties {
        }

        Events {
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "EditorPropertyColor"
        parent: "EditorProperty"
        collection: true

        Properties {
        }

        Events {
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "EditorPropertyEnum"
        parent: "EditorProperty"
        collection: true

        Properties {
        }

        Events {
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "EditorPropertyFlags"
        parent: "EditorProperty"
        collection: true

        Properties {
        }

        Events {
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "EditorPropertyFloat"
        parent: "EditorProperty"
        collection: true

        Properties {
        }

        Events {
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "EditorPropertyInteger"
        parent: "EditorProperty"
        collection: true

        Properties {
        }

        Events {
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "EditorPropertyLayers"
        parent: "EditorProperty"
        collection: true

        Properties {
        }

        Events {
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "EditorPropertyLayersGrid"
        parent: "Control"
        collection: true

        Properties {
        }

        Events {
            Event { godot: "flag_changed"; sms: "flagChanged"; params: "int flag" }
            Event { godot: "rename_confirmed"; sms: "renameConfirmed"; params: "int layerId, string newName" }
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "EditorPropertyLocale"
        parent: "EditorProperty"
        collection: true

        Properties {
        }

        Events {
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "EditorPropertyLocalizableString"
        parent: "EditorProperty"
        collection: true

        Properties {
        }

        Events {
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "EditorPropertyMultilineText"
        parent: "EditorProperty"
        collection: true

        Properties {
        }

        Events {
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "EditorPropertyNodePath"
        parent: "EditorProperty"
        collection: true

        Properties {
        }

        Events {
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "EditorPropertyPath"
        parent: "EditorProperty"
        collection: true

        Properties {
        }

        Events {
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "EditorPropertyResource"
        parent: "EditorProperty"
        collection: true

        Properties {
        }

        Events {
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "EditorPropertyText"
        parent: "EditorProperty"
        collection: true

        Properties {
        }

        Events {
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "EditorPropertyTextEnum"
        parent: "EditorProperty"
        collection: true

        Properties {
        }

        Events {
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "EditorPropertyVector2"
        parent: "EditorPropertyVectorN"
        collection: true

        Properties {
        }

        Events {
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "EditorPropertyVector2i"
        parent: "EditorPropertyVectorN"
        collection: true

        Properties {
        }

        Events {
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "EditorPropertyVectorN"
        parent: "EditorProperty"
        collection: true

        Properties {
        }

        Events {
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "EditorResourcePicker"
        parent: "HBoxContainer"
        collection: true

        Properties {
            Prop { godot: "base_type"; sml: "baseType"; type: "string" }
            Prop { godot: "editable"; sml: "editable"; type: "bool" }
            Prop { godot: "toggle_mode"; sml: "toggleMode"; type: "bool" }
        }

        Events {
            Event { godot: "resource_changed"; sms: "resourceChanged"; params: "Object resource" }
            Event { godot: "resource_selected"; sms: "resourceSelected"; params: "Object resource, bool inspect" }
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "EditorRunBar"
        parent: "MarginContainer"
        collection: true

        Properties {
        }

        Events {
            Event { godot: "play_pressed"; sms: "playPressed"; params: "—" }
            Event { godot: "stop_pressed"; sms: "stopPressed"; params: "—" }
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "EditorRunNative"
        parent: "HBoxContainer"
        collection: true

        Properties {
        }

        Events {
            Event { godot: "native_run"; sms: "nativeRun"; params: "Object preset" }
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "EditorSceneTabs"
        parent: "MarginContainer"
        collection: true

        Properties {
        }

        Events {
            Event { godot: "tab_changed"; sms: "tabChanged"; params: "int tabIndex" }
            Event { godot: "tab_closed"; sms: "tabClosed"; params: "int tabIndex" }
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "EditorScriptPicker"
        parent: "EditorResourcePicker"
        collection: true

        Properties {
        }

        Events {
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "EditorSpinSlider"
        parent: "Range"
        collection: true

        Properties {
            Prop { godot: "control_state"; sml: "controlState"; type: "bool" }
            Prop { godot: "editing_integer"; sml: "editingInteger"; type: "bool" }
            Prop { godot: "flat"; sml: "flat"; type: "bool" }
            Prop { godot: "hide_slider"; sml: "hideSlider"; type: "bool" }
            Prop { godot: "label"; sml: "label"; type: "string" }
            Prop { godot: "read_only"; sml: "readOnly"; type: "bool" }
            Prop { godot: "suffix"; sml: "suffix"; type: "string" }
        }

        Events {
            Event { godot: "grabbed"; sms: "grabbed"; params: "—" }
            Event { godot: "ungrabbed"; sms: "ungrabbed"; params: "—" }
            Event { godot: "updown_pressed"; sms: "updownPressed"; params: "—" }
            Event { godot: "value_focus_entered"; sms: "valueFocusEntered"; params: "—" }
            Event { godot: "value_focus_exited"; sms: "valueFocusExited"; params: "—" }
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "EditorTitleBar"
        parent: "HBoxContainer"
        collection: true

        Properties {
        }

        Events {
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "EditorToaster"
        parent: "HBoxContainer"
        collection: true

        Properties {
        }

        Events {
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "EditorTranslationPreviewButton"
        parent: "Button"
        collection: true

        Properties {
        }

        Events {
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "EditorValidationPanel"
        parent: "PanelContainer"
        collection: true

        Properties {
        }

        Events {
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "EditorVariantTypeOptionButton"
        parent: "OptionButton"
        collection: true

        Properties {
        }

        Events {
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "EditorVersionButton"
        parent: "LinkButton"
        collection: true

        Properties {
        }

        Events {
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "EditorVisualProfiler"
        parent: "VBoxContainer"
        collection: true

        Properties {
        }

        Events {
            Event { godot: "enable_profiling"; sms: "enableProfiling"; params: "bool enable" }
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "EditorZoomWidget"
        parent: "HBoxContainer"
        collection: true

        Properties {
            Prop { godot: "zoom"; sml: "zoom"; type: "float" }
        }

        Events {
            Event { godot: "zoom_changed"; sms: "zoomChanged"; params: "float zoom" }
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "EmbeddedProcessBase"
        parent: "Control"
        collection: true

        Properties {
        }

        Events {
            Event { godot: "embedded_process_focused"; sms: "embeddedProcessFocused"; params: "—" }
            Event { godot: "embedded_process_updated"; sms: "embeddedProcessUpdated"; params: "—" }
            Event { godot: "embedding_completed"; sms: "embeddingCompleted"; params: "—" }
            Event { godot: "embedding_failed"; sms: "embeddingFailed"; params: "—" }
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "EmbeddedProcessMacOS"
        parent: "EmbeddedProcessBase"
        collection: true

        Properties {
        }

        Events {
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "EventListenerLineEdit"
        parent: "LineEdit"
        collection: true

        Properties {
        }

        Events {
            Event { godot: "event_changed"; sms: "eventChanged"; params: "Object event" }
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "FileSystemDock"
        parent: "EditorDock"
        collection: true

        Properties {
        }

        Events {
            Event { godot: "display_mode_changed"; sms: "displayModeChanged"; params: "—" }
            Event { godot: "file_removed"; sms: "fileRemoved"; params: "string file" }
            Event { godot: "files_moved"; sms: "filesMoved"; params: "string oldFile, string newFile" }
            Event { godot: "folder_color_changed"; sms: "folderColorChanged"; params: "—" }
            Event { godot: "folder_moved"; sms: "folderMoved"; params: "string oldFolder, string newFolder" }
            Event { godot: "folder_removed"; sms: "folderRemoved"; params: "string folder" }
            Event { godot: "inherit"; sms: "inherit"; params: "string file" }
            Event { godot: "instantiate"; sms: "instantiate"; params: "Variant files" }
            Event { godot: "resource_removed"; sms: "resourceRemoved"; params: "Object resource" }
            Event { godot: "selection_changed"; sms: "selectionChanged"; params: "—" }
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "FileSystemList"
        parent: "ItemList"
        collection: true

        Properties {
        }

        Events {
            Event { godot: "item_edited"; sms: "itemEdited"; params: "—" }
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "FindBar"
        parent: "HBoxContainer"
        collection: true

        Properties {
        }

        Events {
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "FindInFilesContainer"
        parent: "EditorDock"
        collection: true

        Properties {
        }

        Events {
            Event { godot: "files_modified"; sms: "filesModified"; params: "string paths" }
            Event { godot: "result_selected"; sms: "resultSelected"; params: "string path, int lineNumber, int begin, int end" }
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "FindReplaceBar"
        parent: "HBoxContainer"
        collection: true

        Properties {
        }

        Events {
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "FlowContainer"
        parent: "Container"
        collection: true

        Properties {
            Prop { godot: "alignment"; sml: "alignment"; type: "int" }
            Prop { godot: "last_wrap_alignment"; sml: "lastWrapAlignment"; type: "int" }
            Prop { godot: "reverse_fill"; sml: "reverseFill"; type: "bool" }
            Prop { godot: "vertical"; sml: "vertical"; type: "bool" }
        }

        Events {
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "FoldableContainer"
        parent: "Container"
        collection: true

        Properties {
            Prop { godot: "folded"; sml: "folded"; type: "bool" }
            Prop { godot: "language"; sml: "language"; type: "string" }
            Prop { godot: "title"; sml: "title"; type: "string" }
            Prop { godot: "title_alignment"; sml: "titleAlignment"; type: "int" }
            Prop { godot: "title_position"; sml: "titlePosition"; type: "int" }
            Prop { godot: "title_text_direction"; sml: "titleTextDirection"; type: "int" }
            Prop { godot: "title_text_overrun_behavior"; sml: "titleTextOverrunBehavior"; type: "int" }
        }

        Events {
            Event { godot: "folding_changed"; sms: "foldingChanged"; params: "bool isFolded" }
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "GameView"
        parent: "VBoxContainer"
        collection: true

        Properties {
        }

        Events {
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "GraphEdit"
        parent: "Control"
        collection: true

        Properties {
            Prop { godot: "connection_lines_antialiased"; sml: "connectionLinesAntialiased"; type: "bool" }
            Prop { godot: "connection_lines_curvature"; sml: "connectionLinesCurvature"; type: "float" }
            Prop { godot: "connection_lines_thickness"; sml: "connectionLinesThickness"; type: "float" }
            Prop { godot: "grid_pattern"; sml: "gridPattern"; type: "int" }
            Prop { godot: "minimap_enabled"; sml: "minimapEnabled"; type: "bool" }
            Prop { godot: "minimap_opacity"; sml: "minimapOpacity"; type: "float" }
            Prop { godot: "minimap_size"; sml: "minimapSize"; type: "Vector2" }
            Prop { godot: "panning_scheme"; sml: "panningScheme"; type: "int" }
            Prop { godot: "right_disconnects"; sml: "rightDisconnects"; type: "bool" }
            Prop { godot: "scroll_offset"; sml: "scrollOffset"; type: "Vector2" }
            Prop { godot: "show_arrange_button"; sml: "showArrangeButton"; type: "bool" }
            Prop { godot: "show_grid"; sml: "showGrid"; type: "bool" }
            Prop { godot: "show_grid_buttons"; sml: "showGridButtons"; type: "bool" }
            Prop { godot: "show_menu"; sml: "showMenu"; type: "bool" }
            Prop { godot: "show_minimap_button"; sml: "showMinimapButton"; type: "bool" }
            Prop { godot: "show_zoom_buttons"; sml: "showZoomButtons"; type: "bool" }
            Prop { godot: "show_zoom_label"; sml: "showZoomLabel"; type: "bool" }
            Prop { godot: "snapping_distance"; sml: "snappingDistance"; type: "int" }
            Prop { godot: "snapping_enabled"; sml: "snappingEnabled"; type: "bool" }
            Prop { godot: "zoom"; sml: "zoom"; type: "float" }
            Prop { godot: "zoom_max"; sml: "zoomMax"; type: "float" }
            Prop { godot: "zoom_min"; sml: "zoomMin"; type: "float" }
            Prop { godot: "zoom_step"; sml: "zoomStep"; type: "float" }
        }

        Events {
            Event { godot: "begin_node_move"; sms: "beginNodeMove"; params: "—" }
            Event { godot: "connection_drag_ended"; sms: "connectionDragEnded"; params: "—" }
            Event { godot: "connection_drag_started"; sms: "connectionDragStarted"; params: "Variant fromNode, int fromPort, bool isOutput" }
            Event { godot: "connection_from_empty"; sms: "connectionFromEmpty"; params: "Variant toNode, int toPort, Vector2 releasePosition" }
            Event { godot: "connection_request"; sms: "connectionRequest"; params: "Variant fromNode, int fromPort, Variant toNode, int toPort" }
            Event { godot: "connection_to_empty"; sms: "connectionToEmpty"; params: "Variant fromNode, int fromPort, Vector2 releasePosition" }
            Event { godot: "copy_nodes_request"; sms: "copyNodesRequest"; params: "—" }
            Event { godot: "cut_nodes_request"; sms: "cutNodesRequest"; params: "—" }
            Event { godot: "delete_nodes_request"; sms: "deleteNodesRequest"; params: "Variant nodes" }
            Event { godot: "disconnection_request"; sms: "disconnectionRequest"; params: "Variant fromNode, int fromPort, Variant toNode, int toPort" }
            Event { godot: "duplicate_nodes_request"; sms: "duplicateNodesRequest"; params: "—" }
            Event { godot: "end_node_move"; sms: "endNodeMove"; params: "—" }
            Event { godot: "frame_rect_changed"; sms: "frameRectChanged"; params: "Object frame, Variant newRect" }
            Event { godot: "graph_elements_linked_to_frame_request"; sms: "graphElementsLinkedToFrameRequest"; params: "Variant elements, Variant frame" }
            Event { godot: "node_deselected"; sms: "nodeDeselected"; params: "Object node" }
            Event { godot: "node_selected"; sms: "nodeSelected"; params: "Object node" }
            Event { godot: "paste_nodes_request"; sms: "pasteNodesRequest"; params: "—" }
            Event { godot: "popup_request"; sms: "popupRequest"; params: "Vector2 atPosition" }
            Event { godot: "scroll_offset_changed"; sms: "scrollOffsetChanged"; params: "Vector2 offset" }
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "GraphEditFilter"
        parent: "Control"
        collection: true

        Properties {
        }

        Events {
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "GraphEditMinimap"
        parent: "Control"
        collection: true

        Properties {
        }

        Events {
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "GraphElement"
        parent: "Container"
        collection: true

        Properties {
            Prop { godot: "draggable"; sml: "draggable"; type: "bool" }
            Prop { godot: "position_offset"; sml: "positionOffset"; type: "Vector2" }
            Prop { godot: "resizable"; sml: "resizable"; type: "bool" }
            Prop { godot: "scaling_menus"; sml: "scalingMenus"; type: "bool" }
            Prop { godot: "selectable"; sml: "selectable"; type: "bool" }
            Prop { godot: "selected"; sml: "selected"; type: "bool" }
        }

        Events {
            Event { godot: "delete_request"; sms: "deleteRequest"; params: "—" }
            Event { godot: "dragged"; sms: "dragged"; params: "Vector2 from, Vector2 to" }
            Event { godot: "node_deselected"; sms: "nodeDeselected"; params: "—" }
            Event { godot: "node_selected"; sms: "nodeSelected"; params: "—" }
            Event { godot: "position_offset_changed"; sms: "positionOffsetChanged"; params: "—" }
            Event { godot: "raise_request"; sms: "raiseRequest"; params: "—" }
            Event { godot: "resize_end"; sms: "resizeEnd"; params: "Vector2 newSize" }
            Event { godot: "resize_request"; sms: "resizeRequest"; params: "Vector2 newSize" }
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "GraphFrame"
        parent: "GraphElement"
        collection: true

        Properties {
            Prop { godot: "autoshrink_enabled"; sml: "autoshrinkEnabled"; type: "bool" }
            Prop { godot: "autoshrink_margin"; sml: "autoshrinkMargin"; type: "int" }
            Prop { godot: "drag_margin"; sml: "dragMargin"; type: "int" }
            Prop { godot: "tint_color"; sml: "tintColor"; type: "Color" }
            Prop { godot: "tint_color_enabled"; sml: "tintColorEnabled"; type: "bool" }
            Prop { godot: "title"; sml: "title"; type: "string" }
        }

        Events {
            Event { godot: "autoshrink_changed"; sms: "autoshrinkChanged"; params: "—" }
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "GraphNode"
        parent: "GraphElement"
        collection: true

        Properties {
            Prop { godot: "ignore_invalid_connection_type"; sml: "ignoreInvalidConnectionType"; type: "bool" }
            Prop { godot: "slots_focus_mode"; sml: "slotsFocusMode"; type: "int" }
            Prop { godot: "title"; sml: "title"; type: "string" }
        }

        Events {
            Event { godot: "slot_sizes_changed"; sms: "slotSizesChanged"; params: "—" }
            Event { godot: "slot_updated"; sms: "slotUpdated"; params: "int slotIndex" }
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "GridContainer"
        parent: "Container"
        collection: true

        Properties {
            Prop { godot: "columns"; sml: "columns"; type: "int" }
        }

        Events {
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "GridMapEditor"
        parent: "VBoxContainer"
        collection: true

        Properties {
        }

        Events {
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "GroupSettingsEditor"
        parent: "VBoxContainer"
        collection: true

        Properties {
        }

        Events {
            Event { godot: "group_changed"; sms: "groupChanged"; params: "—" }
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "GroupsDock"
        parent: "EditorDock"
        collection: true

        Properties {
        }

        Events {
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "GroupsEditor"
        parent: "VBoxContainer"
        collection: true

        Properties {
        }

        Events {
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "HBoxContainer"
        parent: "BoxContainer"
        collection: true

        Properties {
        }

        Events {
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "HFlowContainer"
        parent: "FlowContainer"
        collection: true

        Properties {
        }

        Events {
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "HScrollBar"
        parent: "ScrollBar"
        collection: true

        Properties {
        }

        Events {
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "HSeparator"
        parent: "Separator"
        collection: true

        Properties {
        }

        Events {
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "HSlider"
        parent: "Slider"
        collection: true

        Properties {
        }

        Events {
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "HSplitContainer"
        parent: "SplitContainer"
        collection: true

        Properties {
        }

        Events {
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "HistoryDock"
        parent: "EditorDock"
        collection: true

        Properties {
        }

        Events {
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "ImportDefaultsEditor"
        parent: "VBoxContainer"
        collection: true

        Properties {
        }

        Events {
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "ImportDock"
        parent: "EditorDock"
        collection: true

        Properties {
        }

        Events {
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "InspectorDock"
        parent: "EditorDock"
        collection: true

        Properties {
        }

        Events {
            Event { godot: "request_help"; sms: "requestHelp"; params: "—" }
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "ItemList"
        parent: "Control"
        collection: true

        Properties {
            Prop { godot: "allow_reselect"; sml: "allowReselect"; type: "bool" }
            Prop { godot: "allow_rmb_select"; sml: "allowRmbSelect"; type: "bool" }
            Prop { godot: "allow_search"; sml: "allowSearch"; type: "bool" }
            Prop { godot: "auto_height"; sml: "autoHeight"; type: "bool" }
            Prop { godot: "auto_width"; sml: "autoWidth"; type: "bool" }
            Prop { godot: "fixed_column_width"; sml: "fixedColumnWidth"; type: "int" }
            Prop { godot: "icon_mode"; sml: "iconMode"; type: "int" }
            Prop { godot: "icon_scale"; sml: "iconScale"; type: "float" }
            Prop { godot: "item_count"; sml: "itemCount"; type: "int" }
            Prop { godot: "max_columns"; sml: "maxColumns"; type: "int" }
            Prop { godot: "max_text_lines"; sml: "maxTextLines"; type: "int" }
            Prop { godot: "same_column_width"; sml: "sameColumnWidth"; type: "bool" }
            Prop { godot: "scroll_hint_mode"; sml: "scrollHintMode"; type: "int" }
            Prop { godot: "select_mode"; sml: "selectMode"; type: "int" }
            Prop { godot: "text_overrun_behavior"; sml: "textOverrunBehavior"; type: "int" }
            Prop { godot: "tile_scroll_hint"; sml: "tileScrollHint"; type: "bool" }
            Prop { godot: "wraparound_items"; sml: "wraparoundItems"; type: "bool" }
        }

        Events {
            Event { godot: "empty_clicked"; sms: "emptyClicked"; params: "Vector2 atPosition, int mouseButtonIndex" }
            Event { godot: "item_activated"; sms: "itemActivated"; params: "int index" }
            Event { godot: "item_clicked"; sms: "itemClicked"; params: "int index, Vector2 atPosition, int mouseButtonIndex" }
            Event { godot: "item_selected"; sms: "itemSelected"; params: "int index" }
            Event { godot: "multi_selected"; sms: "multiSelected"; params: "int index, bool selected" }
        }

        PseudoChildren {
            Item { props: "id,text,icon,selected,disabled,tooltip" }
        }
    }

    Type {
        name: "Label"
        parent: "Control"
        collection: true

        Properties {
            Prop { godot: "autowrap_mode"; sml: "autowrapMode"; type: "int" }
            Prop { godot: "autowrap_trim_flags"; sml: "autowrapTrimFlags"; type: "int" }
            Prop { godot: "clip_text"; sml: "clipText"; type: "bool" }
            Prop { godot: "ellipsis_char"; sml: "ellipsisChar"; type: "string" }
            Prop { godot: "horizontal_alignment"; sml: "horizontalAlignment"; type: "int" }
            Prop { godot: "justification_flags"; sml: "justificationFlags"; type: "int" }
            Prop { godot: "language"; sml: "language"; type: "string" }
            Prop { godot: "lines_skipped"; sml: "linesSkipped"; type: "int" }
            Prop { godot: "max_lines_visible"; sml: "maxLinesVisible"; type: "int" }
            Prop { godot: "paragraph_separator"; sml: "paragraphSeparator"; type: "string" }
            Prop { godot: "structured_text_bidi_override"; sml: "structuredTextBidiOverride"; type: "int" }
            Prop { godot: "text"; sml: "text"; type: "string" }
            Prop { godot: "text_direction"; sml: "textDirection"; type: "int" }
            Prop { godot: "text_overrun_behavior"; sml: "textOverrunBehavior"; type: "int" }
            Prop { godot: "uppercase"; sml: "uppercase"; type: "bool" }
            Prop { godot: "vertical_alignment"; sml: "verticalAlignment"; type: "int" }
            Prop { godot: "visible_characters"; sml: "visibleCharacters"; type: "int" }
            Prop { godot: "visible_characters_behavior"; sml: "visibleCharactersBehavior"; type: "int" }
            Prop { godot: "visible_ratio"; sml: "visibleRatio"; type: "float" }
        }

        Events {
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "LayerHost"
        parent: "Control"
        collection: true

        Properties {
        }

        Events {
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "LightOccluder2DEditor"
        parent: "AbstractPolygon2DEditor"
        collection: true

        Properties {
        }

        Events {
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "Line2DEditor"
        parent: "AbstractPolygon2DEditor"
        collection: true

        Properties {
        }

        Events {
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "LineEdit"
        parent: "Control"
        collection: true

        Properties {
            Prop { godot: "alignment"; sml: "alignment"; type: "int" }
            Prop { godot: "backspace_deletes_composite_character_enabled"; sml: "backspaceDeletesCompositeCharacterEnabled"; type: "bool" }
            Prop { godot: "caret_blink"; sml: "caretBlink"; type: "bool" }
            Prop { godot: "caret_blink_interval"; sml: "caretBlinkInterval"; type: "float" }
            Prop { godot: "caret_column"; sml: "caretColumn"; type: "int" }
            Prop { godot: "caret_force_displayed"; sml: "caretForceDisplayed"; type: "bool" }
            Prop { godot: "caret_mid_grapheme"; sml: "caretMidGrapheme"; type: "bool" }
            Prop { godot: "clear_button_enabled"; sml: "clearButtonEnabled"; type: "bool" }
            Prop { godot: "context_menu_enabled"; sml: "contextMenuEnabled"; type: "bool" }
            Prop { godot: "deselect_on_focus_loss_enabled"; sml: "deselectOnFocusLossEnabled"; type: "bool" }
            Prop { godot: "drag_and_drop_selection_enabled"; sml: "dragAndDropSelectionEnabled"; type: "bool" }
            Prop { godot: "draw_control_chars"; sml: "drawControlChars"; type: "bool" }
            Prop { godot: "editable"; sml: "editable"; type: "bool" }
            Prop { godot: "emoji_menu_enabled"; sml: "emojiMenuEnabled"; type: "bool" }
            Prop { godot: "expand_to_text_length"; sml: "expandToTextLength"; type: "bool" }
            Prop { godot: "flat"; sml: "flat"; type: "bool" }
            Prop { godot: "icon_expand_mode"; sml: "iconExpandMode"; type: "int" }
            Prop { godot: "keep_editing_on_text_submit"; sml: "keepEditingOnTextSubmit"; type: "bool" }
            Prop { godot: "language"; sml: "language"; type: "string" }
            Prop { godot: "max_length"; sml: "maxLength"; type: "int" }
            Prop { godot: "middle_mouse_paste_enabled"; sml: "middleMousePasteEnabled"; type: "bool" }
            Prop { godot: "placeholder_text"; sml: "placeholderText"; type: "string" }
            Prop { godot: "right_icon_scale"; sml: "rightIconScale"; type: "float" }
            Prop { godot: "secret"; sml: "secret"; type: "bool" }
            Prop { godot: "secret_character"; sml: "secretCharacter"; type: "string" }
            Prop { godot: "select_all_on_focus"; sml: "selectAllOnFocus"; type: "bool" }
            Prop { godot: "selecting_enabled"; sml: "selectingEnabled"; type: "bool" }
            Prop { godot: "shortcut_keys_enabled"; sml: "shortcutKeysEnabled"; type: "bool" }
            Prop { godot: "structured_text_bidi_override"; sml: "structuredTextBidiOverride"; type: "int" }
            Prop { godot: "text"; sml: "text"; type: "string" }
            Prop { godot: "text_direction"; sml: "textDirection"; type: "int" }
            Prop { godot: "virtual_keyboard_enabled"; sml: "virtualKeyboardEnabled"; type: "bool" }
            Prop { godot: "virtual_keyboard_show_on_focus"; sml: "virtualKeyboardShowOnFocus"; type: "bool" }
            Prop { godot: "virtual_keyboard_type"; sml: "virtualKeyboardType"; type: "int" }
        }

        Events {
            Event { godot: "editing_toggled"; sms: "editingToggled"; params: "bool toggledOn" }
            Event { godot: "text_change_rejected"; sms: "textChangeRejected"; params: "string rejectedSubstring" }
            Event { godot: "text_changed"; sms: "textChanged"; params: "string newText" }
            Event { godot: "text_submitted"; sms: "textSubmitted"; params: "string newText" }
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "LinkButton"
        parent: "BaseButton"
        collection: true

        Properties {
            Prop { godot: "ellipsis_char"; sml: "ellipsisChar"; type: "string" }
            Prop { godot: "language"; sml: "language"; type: "string" }
            Prop { godot: "structured_text_bidi_override"; sml: "structuredTextBidiOverride"; type: "int" }
            Prop { godot: "text"; sml: "text"; type: "string" }
            Prop { godot: "text_direction"; sml: "textDirection"; type: "int" }
            Prop { godot: "text_overrun_behavior"; sml: "textOverrunBehavior"; type: "int" }
            Prop { godot: "underline"; sml: "underline"; type: "int" }
            Prop { godot: "uri"; sml: "uri"; type: "string" }
        }

        Events {
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "LocalizationEditor"
        parent: "VBoxContainer"
        collection: true

        Properties {
        }

        Events {
            Event { godot: "localization_changed"; sms: "localizationChanged"; params: "—" }
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "MarginContainer"
        parent: "Container"
        collection: true

        Properties {
        }

        Events {
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "MenuBar"
        parent: "Control"
        collection: true

        Properties {
            Prop { godot: "flat"; sml: "flat"; type: "bool" }
            Prop { godot: "language"; sml: "language"; type: "string" }
            Prop { godot: "prefer_global_menu"; sml: "preferGlobalMenu"; type: "bool" }
            Prop { godot: "start_index"; sml: "startIndex"; type: "int" }
            Prop { godot: "switch_on_hover"; sml: "switchOnHover"; type: "bool" }
            Prop { godot: "text_direction"; sml: "textDirection"; type: "int" }
        }

        Events {
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "MenuButton"
        parent: "Button"
        collection: true

        Properties {
            Prop { godot: "item_count"; sml: "itemCount"; type: "int" }
            Prop { godot: "switch_on_hover"; sml: "switchOnHover"; type: "bool" }
        }

        Events {
            Event { godot: "about_to_popup"; sms: "aboutToPopup"; params: "—" }
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "MeshInstance3DEditor"
        parent: "Control"
        collection: true

        Properties {
        }

        Events {
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "MeshLibraryEditor"
        parent: "Control"
        collection: true

        Properties {
        }

        Events {
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "MultiMeshEditor"
        parent: "Control"
        collection: true

        Properties {
        }

        Events {
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "NavigationLink2DEditor"
        parent: "Control"
        collection: true

        Properties {
        }

        Events {
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "NavigationObstacle2DEditor"
        parent: "AbstractPolygon2DEditor"
        collection: true

        Properties {
        }

        Events {
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "NavigationRegion2DEditor"
        parent: "AbstractPolygon2DEditor"
        collection: true

        Properties {
        }

        Events {
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "NavigationRegion3DEditor"
        parent: "Control"
        collection: true

        Properties {
        }

        Events {
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "NinePatchRect"
        parent: "Control"
        collection: true

        Properties {
            Prop { godot: "axis_stretch_horizontal"; sml: "axisStretchHorizontal"; type: "int" }
            Prop { godot: "axis_stretch_vertical"; sml: "axisStretchVertical"; type: "int" }
            Prop { godot: "draw_center"; sml: "drawCenter"; type: "bool" }
            Prop { godot: "patch_margin_bottom"; sml: "patchMarginBottom"; type: "int" }
            Prop { godot: "patch_margin_left"; sml: "patchMarginLeft"; type: "int" }
            Prop { godot: "patch_margin_right"; sml: "patchMarginRight"; type: "int" }
            Prop { godot: "patch_margin_top"; sml: "patchMarginTop"; type: "int" }
        }

        Events {
            Event { godot: "texture_changed"; sms: "textureChanged"; params: "—" }
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "Node"
        parent: "Object"

        Properties {
            Prop { godot: "auto_translate_mode"; sml: "autoTranslateMode"; type: "int" }
            Prop { godot: "editor_description"; sml: "editorDescription"; type: "string" }
            Prop { godot: "physics_interpolation_mode"; sml: "physicsInterpolationMode"; type: "int" }
            Prop { godot: "process_mode"; sml: "processMode"; type: "int" }
            Prop { godot: "process_physics_priority"; sml: "processPhysicsPriority"; type: "int" }
            Prop { godot: "process_priority"; sml: "processPriority"; type: "int" }
            Prop { godot: "process_thread_group"; sml: "processThreadGroup"; type: "int" }
            Prop { godot: "process_thread_group_order"; sml: "processThreadGroupOrder"; type: "int" }
            Prop { godot: "process_thread_messages"; sml: "processThreadMessages"; type: "int" }
        }

        Events {
            Event { godot: "child_entered_tree"; sms: "childEnteredTree"; params: "Object node" }
            Event { godot: "child_exiting_tree"; sms: "childExitingTree"; params: "Object node" }
            Event { godot: "child_order_changed"; sms: "childOrderChanged"; params: "—" }
            Event { godot: "editor_description_changed"; sms: "editorDescriptionChanged"; params: "Object node" }
            Event { godot: "editor_state_changed"; sms: "editorStateChanged"; params: "—" }
            Event { godot: "ready"; sms: "ready"; params: "—" }
            Event { godot: "renamed"; sms: "renamed"; params: "—" }
            Event { godot: "replacing_by"; sms: "replacingBy"; params: "Object node" }
            Event { godot: "tree_entered"; sms: "treeEntered"; params: "—" }
            Event { godot: "tree_exited"; sms: "treeExited"; params: "—" }
            Event { godot: "tree_exiting"; sms: "treeExiting"; params: "—" }
        }
    }

    Type {
        name: "Node3DEditor"
        parent: "VBoxContainer"
        collection: true

        Properties {
        }

        Events {
            Event { godot: "item_group_status_changed"; sms: "itemGroupStatusChanged"; params: "—" }
            Event { godot: "item_lock_status_changed"; sms: "itemLockStatusChanged"; params: "—" }
            Event { godot: "transform_key_request"; sms: "transformKeyRequest"; params: "—" }
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "Node3DEditorViewport"
        parent: "Control"
        collection: true

        Properties {
        }

        Events {
            Event { godot: "clicked"; sms: "clicked"; params: "—" }
            Event { godot: "toggle_maximize_view"; sms: "toggleMaximizeView"; params: "Object viewport" }
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "Node3DEditorViewportContainer"
        parent: "Container"
        collection: true

        Properties {
        }

        Events {
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "Object"

        Properties {
        }

        Events {
            Event { godot: "property_list_changed"; sms: "propertyListChanged"; params: "—" }
            Event { godot: "script_changed"; sms: "scriptChanged"; params: "—" }
        }
    }

    Type {
        name: "ObjectDBProfilerPanel"
        parent: "Control"
        collection: true

        Properties {
        }

        Events {
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "OpenXRBindingModifierEditor"
        parent: "PanelContainer"
        collection: true

        Properties {
        }

        Events {
            Event { godot: "binding_modifier_removed"; sms: "bindingModifierRemoved"; params: "Object bindingModifierEditor" }
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "OpenXRInteractionProfileEditor"
        parent: "OpenXRInteractionProfileEditorBase"
        collection: true

        Properties {
        }

        Events {
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "OpenXRInteractionProfileEditorBase"
        parent: "HBoxContainer"
        collection: true

        Properties {
        }

        Events {
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "OptionButton"
        parent: "Button"
        collection: true

        Properties {
            Prop { godot: "allow_reselect"; sml: "allowReselect"; type: "bool" }
            Prop { godot: "fit_to_longest_item"; sml: "fitToLongestItem"; type: "bool" }
            Prop { godot: "item_count"; sml: "itemCount"; type: "int" }
            Prop { godot: "selected"; sml: "selected"; type: "int" }
        }

        Events {
            Event { godot: "item_focused"; sms: "itemFocused"; params: "int index" }
            Event { godot: "item_selected"; sms: "itemSelected"; params: "int index" }
        }

        PseudoChildren {
            Item { props: "id,text,icon,disabled,selected" }
        }
    }

    Type {
        name: "Panel"
        parent: "Control"
        collection: true

        Properties {
        }

        Events {
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "PanelContainer"
        parent: "Container"
        collection: true

        Properties {
        }

        Events {
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "Path2DEditor"
        parent: "HBoxContainer"
        collection: true

        Properties {
        }

        Events {
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "Polygon2DEditor"
        parent: "AbstractPolygon2DEditor"
        collection: true

        Properties {
        }

        Events {
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "Polygon3DEditor"
        parent: "HBoxContainer"
        collection: true

        Properties {
        }

        Events {
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "Popup"
        parent: "Window"

        Properties {
        }

        Events {
            Event { godot: "popup_hide"; sms: "popupHide"; params: "—" }
        }
    }

    Type {
        name: "PopupMenu"
        parent: "Popup"
        collection: true

        Properties {
            Prop { godot: "allow_search"; sml: "allowSearch"; type: "bool" }
            Prop { godot: "hide_on_checkable_item_selection"; sml: "hideOnCheckableItemSelection"; type: "bool" }
            Prop { godot: "hide_on_item_selection"; sml: "hideOnItemSelection"; type: "bool" }
            Prop { godot: "hide_on_state_item_selection"; sml: "hideOnStateItemSelection"; type: "bool" }
            Prop { godot: "item_count"; sml: "itemCount"; type: "int" }
            Prop { godot: "prefer_native_menu"; sml: "preferNativeMenu"; type: "bool" }
            Prop { godot: "shrink_height"; sml: "shrinkHeight"; type: "bool" }
            Prop { godot: "shrink_width"; sml: "shrinkWidth"; type: "bool" }
            Prop { godot: "submenu_popup_delay"; sml: "submenuPopupDelay"; type: "float" }
            Prop { godot: "system_menu_id"; sml: "systemMenuId"; type: "int" }
        }

        Events {
            Event { godot: "id_focused"; sms: "idFocused"; params: "int id" }
            Event { godot: "id_pressed"; sms: "idPressed"; params: "int id" }
            Event { godot: "index_pressed"; sms: "indexPressed"; params: "int index" }
            Event { godot: "menu_changed"; sms: "menuChanged"; params: "—" }
        }

        PseudoChildren {
            Item { props: "id,text,disabled" }
            CheckItem { props: "id,text,checked,disabled" }
            Separator { props: "" }
        }
    }

    Type {
        name: "ProgressBar"
        parent: "Range"
        collection: true

        Properties {
            Prop { godot: "editor_preview_indeterminate"; sml: "editorPreviewIndeterminate"; type: "bool" }
            Prop { godot: "fill_mode"; sml: "fillMode"; type: "int" }
            Prop { godot: "indeterminate"; sml: "indeterminate"; type: "bool" }
            Prop { godot: "show_percentage"; sml: "showPercentage"; type: "bool" }
        }

        Events {
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "ProgressDialog"
        parent: "CenterContainer"
        collection: true

        Properties {
        }

        Events {
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "ProjectExportTextureFormatError"
        parent: "HBoxContainer"
        collection: true

        Properties {
        }

        Events {
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "QuickOpenResultContainer"
        parent: "VBoxContainer"
        collection: true

        Properties {
        }

        Events {
            Event { godot: "result_clicked"; sms: "resultClicked"; params: "bool doubleClick" }
            Event { godot: "selection_changed"; sms: "selectionChanged"; params: "—" }
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "Range"
        parent: "Control"
        collection: true

        Properties {
            Prop { godot: "allow_greater"; sml: "allowGreater"; type: "bool" }
            Prop { godot: "allow_lesser"; sml: "allowLesser"; type: "bool" }
            Prop { godot: "exp_edit"; sml: "expEdit"; type: "bool" }
            Prop { godot: "max_value"; sml: "maxValue"; type: "float" }
            Prop { godot: "min_value"; sml: "minValue"; type: "float" }
            Prop { godot: "page"; sml: "page"; type: "float" }
            Prop { godot: "rounded"; sml: "rounded"; type: "bool" }
            Prop { godot: "step"; sml: "step"; type: "float" }
            Prop { godot: "value"; sml: "value"; type: "float" }
        }

        Events {
            Event { godot: "changed"; sms: "changed"; params: "—" }
            Event { godot: "value_changed"; sms: "valueChanged"; params: "float value" }
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "ReferenceRect"
        parent: "Control"
        collection: true

        Properties {
            Prop { godot: "border_color"; sml: "borderColor"; type: "Color" }
            Prop { godot: "border_width"; sml: "borderWidth"; type: "float" }
            Prop { godot: "editor_only"; sml: "editorOnly"; type: "bool" }
        }

        Events {
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "ReplicationEditor"
        parent: "VBoxContainer"
        collection: true

        Properties {
        }

        Events {
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "ResourcePreloaderEditor"
        parent: "EditorDock"
        collection: true

        Properties {
        }

        Events {
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "RichTextLabel"
        parent: "Control"
        collection: true

        Properties {
            Prop { godot: "autowrap_mode"; sml: "autowrapMode"; type: "int" }
            Prop { godot: "autowrap_trim_flags"; sml: "autowrapTrimFlags"; type: "int" }
            Prop { godot: "bbcode_enabled"; sml: "bbcodeEnabled"; type: "bool" }
            Prop { godot: "context_menu_enabled"; sml: "contextMenuEnabled"; type: "bool" }
            Prop { godot: "deselect_on_focus_loss_enabled"; sml: "deselectOnFocusLossEnabled"; type: "bool" }
            Prop { godot: "drag_and_drop_selection_enabled"; sml: "dragAndDropSelectionEnabled"; type: "bool" }
            Prop { godot: "fit_content"; sml: "fitContent"; type: "bool" }
            Prop { godot: "hint_underlined"; sml: "hintUnderlined"; type: "bool" }
            Prop { godot: "horizontal_alignment"; sml: "horizontalAlignment"; type: "int" }
            Prop { godot: "justification_flags"; sml: "justificationFlags"; type: "int" }
            Prop { godot: "language"; sml: "language"; type: "string" }
            Prop { godot: "meta_underlined"; sml: "metaUnderlined"; type: "bool" }
            Prop { godot: "progress_bar_delay"; sml: "progressBarDelay"; type: "int" }
            Prop { godot: "scroll_active"; sml: "scrollActive"; type: "bool" }
            Prop { godot: "scroll_following"; sml: "scrollFollowing"; type: "bool" }
            Prop { godot: "scroll_following_visible_characters"; sml: "scrollFollowingVisibleCharacters"; type: "bool" }
            Prop { godot: "selection_enabled"; sml: "selectionEnabled"; type: "bool" }
            Prop { godot: "shortcut_keys_enabled"; sml: "shortcutKeysEnabled"; type: "bool" }
            Prop { godot: "structured_text_bidi_override"; sml: "structuredTextBidiOverride"; type: "int" }
            Prop { godot: "tab_size"; sml: "tabSize"; type: "int" }
            Prop { godot: "text"; sml: "text"; type: "string" }
            Prop { godot: "text_direction"; sml: "textDirection"; type: "int" }
            Prop { godot: "threaded"; sml: "threaded"; type: "bool" }
            Prop { godot: "vertical_alignment"; sml: "verticalAlignment"; type: "int" }
            Prop { godot: "visible_characters"; sml: "visibleCharacters"; type: "int" }
            Prop { godot: "visible_characters_behavior"; sml: "visibleCharactersBehavior"; type: "int" }
            Prop { godot: "visible_ratio"; sml: "visibleRatio"; type: "float" }
        }

        Events {
            Event { godot: "finished"; sms: "finished"; params: "—" }
            Event { godot: "meta_clicked"; sms: "metaClicked"; params: "Variant meta" }
            Event { godot: "meta_hover_ended"; sms: "metaHoverEnded"; params: "Variant meta" }
            Event { godot: "meta_hover_started"; sms: "metaHoverStarted"; params: "Variant meta" }
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "SceneTreeDock"
        parent: "EditorDock"
        collection: true

        Properties {
        }

        Events {
            Event { godot: "add_node_used"; sms: "addNodeUsed"; params: "—" }
            Event { godot: "node_created"; sms: "nodeCreated"; params: "Object node" }
            Event { godot: "remote_tree_selected"; sms: "remoteTreeSelected"; params: "—" }
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "SceneTreeEditor"
        parent: "Control"
        collection: true

        Properties {
        }

        Events {
            Event { godot: "files_dropped"; sms: "filesDropped"; params: "Variant files, Variant toPath, int type" }
            Event { godot: "node_changed"; sms: "nodeChanged"; params: "—" }
            Event { godot: "node_prerename"; sms: "nodePrerename"; params: "—" }
            Event { godot: "node_renamed"; sms: "nodeRenamed"; params: "—" }
            Event { godot: "node_selected"; sms: "nodeSelected"; params: "—" }
            Event { godot: "nodes_dragged"; sms: "nodesDragged"; params: "—" }
            Event { godot: "nodes_rearranged"; sms: "nodesRearranged"; params: "Variant paths, Variant toPath, int type" }
            Event { godot: "open"; sms: "open"; params: "—" }
            Event { godot: "open_script"; sms: "openScript"; params: "—" }
            Event { godot: "rmb_pressed"; sms: "rmbPressed"; params: "Vector2 position" }
            Event { godot: "script_dropped"; sms: "scriptDropped"; params: "string file, Variant toPath" }
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "ScreenSelect"
        parent: "Button"
        collection: true

        Properties {
        }

        Events {
            Event { godot: "request_open_in_screen"; sms: "requestOpenInScreen"; params: "int screen" }
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "ScriptEditor"
        parent: "PanelContainer"
        collection: true

        Properties {
        }

        Events {
            Event { godot: "editor_script_changed"; sms: "editorScriptChanged"; params: "Object script" }
            Event { godot: "script_close"; sms: "scriptClose"; params: "Object script" }
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "ScriptEditorBase"
        parent: "VBoxContainer"
        collection: true

        Properties {
        }

        Events {
            Event { godot: "edited_script_changed"; sms: "editedScriptChanged"; params: "—" }
            Event { godot: "go_to_help"; sms: "goToHelp"; params: "string what" }
            Event { godot: "go_to_method"; sms: "goToMethod"; params: "Object script, string method" }
            Event { godot: "name_changed"; sms: "nameChanged"; params: "—" }
            Event { godot: "replace_in_files_requested"; sms: "replaceInFilesRequested"; params: "string text" }
            Event { godot: "request_help"; sms: "requestHelp"; params: "string topic" }
            Event { godot: "request_open_script_at_line"; sms: "requestOpenScriptAtLine"; params: "Object script, int line" }
            Event { godot: "request_save_history"; sms: "requestSaveHistory"; params: "—" }
            Event { godot: "request_save_previous_state"; sms: "requestSavePreviousState"; params: "Variant state" }
            Event { godot: "search_in_files_requested"; sms: "searchInFilesRequested"; params: "string text" }
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "ScriptEditorDebugger"
        parent: "MarginContainer"
        collection: true

        Properties {
        }

        Events {
            Event { godot: "breaked"; sms: "breaked"; params: "bool reallydid, bool canDebug, string reason, bool hasStackdump" }
            Event { godot: "breakpoint_selected"; sms: "breakpointSelected"; params: "Object , int line" }
            Event { godot: "clear_breakpoints"; sms: "clearBreakpoints"; params: "—" }
            Event { godot: "clear_execution"; sms: "clearExecution"; params: "Object " }
            Event { godot: "debug_data"; sms: "debugData"; params: "string msg, Variant data" }
            Event { godot: "embed_shortcut_requested"; sms: "embedShortcutRequested"; params: "int embedShortcutAction" }
            Event { godot: "error_selected"; sms: "errorSelected"; params: "int error" }
            Event { godot: "errors_cleared"; sms: "errorsCleared"; params: "—" }
            Event { godot: "output"; sms: "output"; params: "string msg, int level" }
            Event { godot: "remote_object_property_updated"; sms: "remoteObjectPropertyUpdated"; params: "int id, string property" }
            Event { godot: "remote_objects_requested"; sms: "remoteObjectsRequested"; params: "Variant ids" }
            Event { godot: "remote_objects_updated"; sms: "remoteObjectsUpdated"; params: "Object remoteObjects" }
            Event { godot: "remote_tree_clear_selection_requested"; sms: "remoteTreeClearSelectionRequested"; params: "—" }
            Event { godot: "remote_tree_select_requested"; sms: "remoteTreeSelectRequested"; params: "Variant ids" }
            Event { godot: "remote_tree_updated"; sms: "remoteTreeUpdated"; params: "—" }
            Event { godot: "remote_window_title_changed"; sms: "remoteWindowTitleChanged"; params: "string title" }
            Event { godot: "set_breakpoint"; sms: "setBreakpoint"; params: "Object , int line, bool enabled" }
            Event { godot: "set_execution"; sms: "setExecution"; params: "Object , int line" }
            Event { godot: "stack_dump"; sms: "stackDump"; params: "Variant stackDump" }
            Event { godot: "stack_frame_selected"; sms: "stackFrameSelected"; params: "int frame" }
            Event { godot: "stack_frame_var"; sms: "stackFrameVar"; params: "Variant data" }
            Event { godot: "stack_frame_vars"; sms: "stackFrameVars"; params: "int numVars" }
            Event { godot: "started"; sms: "started"; params: "—" }
            Event { godot: "stop_requested"; sms: "stopRequested"; params: "—" }
            Event { godot: "stopped"; sms: "stopped"; params: "—" }
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "ScriptTextEditor"
        parent: "ScriptEditorBase"
        collection: true

        Properties {
        }

        Events {
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "ScrollBar"
        parent: "Range"
        collection: true

        Properties {
            Prop { godot: "custom_step"; sml: "customStep"; type: "float" }
        }

        Events {
            Event { godot: "scrolling"; sms: "scrolling"; params: "—" }
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "ScrollContainer"
        parent: "Container"
        collection: true

        Properties {
            Prop { godot: "draw_focus_border"; sml: "drawFocusBorder"; type: "bool" }
            Prop { godot: "follow_focus"; sml: "followFocus"; type: "bool" }
            Prop { godot: "horizontal_scroll_mode"; sml: "horizontalScrollMode"; type: "int" }
            Prop { godot: "scroll_deadzone"; sml: "scrollDeadzone"; type: "int" }
            Prop { godot: "scroll_hint_mode"; sml: "scrollHintMode"; type: "int" }
            Prop { godot: "scroll_horizontal"; sml: "scrollHorizontal"; type: "int" }
            Prop { godot: "scroll_horizontal_custom_step"; sml: "scrollHorizontalCustomStep"; type: "float" }
            Prop { godot: "scroll_vertical"; sml: "scrollVertical"; type: "int" }
            Prop { godot: "scroll_vertical_custom_step"; sml: "scrollVerticalCustomStep"; type: "float" }
            Prop { godot: "tile_scroll_hint"; sml: "tileScrollHint"; type: "bool" }
            Prop { godot: "vertical_scroll_mode"; sml: "verticalScrollMode"; type: "int" }
        }

        Events {
            Event { godot: "scroll_ended"; sms: "scrollEnded"; params: "—" }
            Event { godot: "scroll_started"; sms: "scrollStarted"; params: "—" }
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "SectionedInspector"
        parent: "HSplitContainer"
        collection: true

        Properties {
        }

        Events {
            Event { godot: "category_changed"; sms: "categoryChanged"; params: "string newCategory" }
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "Separator"
        parent: "Control"
        collection: true

        Properties {
        }

        Events {
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "ShaderFileEditor"
        parent: "EditorDock"
        collection: true

        Properties {
        }

        Events {
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "ShaderGlobalsEditor"
        parent: "VBoxContainer"
        collection: true

        Properties {
        }

        Events {
            Event { godot: "globals_changed"; sms: "globalsChanged"; params: "—" }
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "SignalsDock"
        parent: "EditorDock"
        collection: true

        Properties {
        }

        Events {
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "SizeFlagPresetPicker"
        parent: "ControlEditorPresetPicker"
        collection: true

        Properties {
        }

        Events {
            Event { godot: "expand_flag_toggled"; sms: "expandFlagToggled"; params: "bool expandFlag" }
            Event { godot: "size_flags_selected"; sms: "sizeFlagsSelected"; params: "int sizeFlags" }
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "Skeleton2DEditor"
        parent: "Control"
        collection: true

        Properties {
        }

        Events {
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "Slider"
        parent: "Range"
        collection: true

        Properties {
            Prop { godot: "editable"; sml: "editable"; type: "bool" }
            Prop { godot: "scrollable"; sml: "scrollable"; type: "bool" }
            Prop { godot: "tick_count"; sml: "tickCount"; type: "int" }
            Prop { godot: "ticks_on_borders"; sml: "ticksOnBorders"; type: "bool" }
            Prop { godot: "ticks_position"; sml: "ticksPosition"; type: "int" }
        }

        Events {
            Event { godot: "drag_ended"; sms: "dragEnded"; params: "bool valueChanged" }
            Event { godot: "drag_started"; sms: "dragStarted"; params: "—" }
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "SnapshotClassView"
        parent: "SnapshotView"
        collection: true

        Properties {
        }

        Events {
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "SnapshotNodeView"
        parent: "SnapshotView"
        collection: true

        Properties {
        }

        Events {
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "SnapshotObjectView"
        parent: "SnapshotView"
        collection: true

        Properties {
        }

        Events {
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "SnapshotRefCountedView"
        parent: "SnapshotView"
        collection: true

        Properties {
        }

        Events {
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "SnapshotSummaryView"
        parent: "SnapshotView"
        collection: true

        Properties {
        }

        Events {
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "SnapshotView"
        parent: "Control"
        collection: true

        Properties {
        }

        Events {
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "SpinBox"
        parent: "Range"
        collection: true

        Properties {
            Prop { godot: "alignment"; sml: "alignment"; type: "int" }
            Prop { godot: "custom_arrow_round"; sml: "customArrowRound"; type: "bool" }
            Prop { godot: "custom_arrow_step"; sml: "customArrowStep"; type: "float" }
            Prop { godot: "editable"; sml: "editable"; type: "bool" }
            Prop { godot: "prefix"; sml: "prefix"; type: "string" }
            Prop { godot: "select_all_on_focus"; sml: "selectAllOnFocus"; type: "bool" }
            Prop { godot: "suffix"; sml: "suffix"; type: "string" }
            Prop { godot: "update_on_text_changed"; sml: "updateOnTextChanged"; type: "bool" }
        }

        Events {
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "SpinBoxLineEdit"
        parent: "LineEdit"
        collection: true

        Properties {
        }

        Events {
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "SplitContainer"
        parent: "Container"
        collection: true

        Properties {
            Prop { godot: "collapsed"; sml: "collapsed"; type: "bool" }
            Prop { godot: "drag_area_highlight_in_editor"; sml: "dragAreaHighlightInEditor"; type: "bool" }
            Prop { godot: "drag_area_margin_begin"; sml: "dragAreaMarginBegin"; type: "int" }
            Prop { godot: "drag_area_margin_end"; sml: "dragAreaMarginEnd"; type: "int" }
            Prop { godot: "drag_area_offset"; sml: "dragAreaOffset"; type: "int" }
            Prop { godot: "dragger_visibility"; sml: "draggerVisibility"; type: "int" }
            Prop { godot: "dragging_enabled"; sml: "draggingEnabled"; type: "bool" }
            Prop { godot: "touch_dragger_enabled"; sml: "touchDraggerEnabled"; type: "bool" }
            Prop { godot: "vertical"; sml: "vertical"; type: "bool" }
        }

        Events {
            Event { godot: "drag_ended"; sms: "dragEnded"; params: "—" }
            Event { godot: "drag_started"; sms: "dragStarted"; params: "—" }
            Event { godot: "dragged"; sms: "dragged"; params: "int offset" }
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "SplitContainerDragger"
        parent: "Control"
        collection: true

        Properties {
        }

        Events {
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "Sprite2DEditor"
        parent: "Control"
        collection: true

        Properties {
        }

        Events {
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "SpriteFramesEditor"
        parent: "EditorDock"
        collection: true

        Properties {
        }

        Events {
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "SubViewportContainer"
        parent: "Container"
        collection: true

        Properties {
            Prop { godot: "mouse_target"; sml: "mouseTarget"; type: "bool" }
            Prop { godot: "stretch"; sml: "stretch"; type: "bool" }
            Prop { godot: "stretch_shrink"; sml: "stretchShrink"; type: "int" }
        }

        Events {
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "SwitchSeparator"
        parent: "MarginContainer"
        collection: true

        Properties {
        }

        Events {
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "TabBar"
        parent: "Control"
        collection: true

        Properties {
            Prop { godot: "clip_tabs"; sml: "clipTabs"; type: "bool" }
            Prop { godot: "close_with_middle_mouse"; sml: "closeWithMiddleMouse"; type: "bool" }
            Prop { godot: "current_tab"; sml: "currentTab"; type: "int" }
            Prop { godot: "deselect_enabled"; sml: "deselectEnabled"; type: "bool" }
            Prop { godot: "drag_to_rearrange_enabled"; sml: "dragToRearrangeEnabled"; type: "bool" }
            Prop { godot: "max_tab_width"; sml: "maxTabWidth"; type: "int" }
            Prop { godot: "scroll_to_selected"; sml: "scrollToSelected"; type: "bool" }
            Prop { godot: "scrolling_enabled"; sml: "scrollingEnabled"; type: "bool" }
            Prop { godot: "select_with_rmb"; sml: "selectWithRmb"; type: "bool" }
            Prop { godot: "switch_on_drag_hover"; sml: "switchOnDragHover"; type: "bool" }
            Prop { godot: "tab_alignment"; sml: "tabAlignment"; type: "int" }
            Prop { godot: "tab_close_display_policy"; sml: "tabCloseDisplayPolicy"; type: "int" }
            Prop { godot: "tab_count"; sml: "tabCount"; type: "int" }
            Prop { godot: "tabs_rearrange_group"; sml: "tabsRearrangeGroup"; type: "int" }
        }

        Events {
            Event { godot: "active_tab_rearranged"; sms: "activeTabRearranged"; params: "int idxTo" }
            Event { godot: "tab_button_pressed"; sms: "tabButtonPressed"; params: "int tab" }
            Event { godot: "tab_changed"; sms: "tabChanged"; params: "int tab" }
            Event { godot: "tab_clicked"; sms: "tabClicked"; params: "int tab" }
            Event { godot: "tab_close_pressed"; sms: "tabClosePressed"; params: "int tab" }
            Event { godot: "tab_hovered"; sms: "tabHovered"; params: "int tab" }
            Event { godot: "tab_rmb_clicked"; sms: "tabRmbClicked"; params: "int tab" }
            Event { godot: "tab_selected"; sms: "tabSelected"; params: "int tab" }
        }

        PseudoChildren {
            Tab { props: "id,title,icon,disabled,hidden,selected" }
        }
    }

    Type {
        name: "TabContainer"
        parent: "Container"
        collection: true

        Properties {
            Prop { godot: "all_tabs_in_front"; sml: "allTabsInFront"; type: "bool" }
            Prop { godot: "clip_tabs"; sml: "clipTabs"; type: "bool" }
            Prop { godot: "current_tab"; sml: "currentTab"; type: "int" }
            Prop { godot: "deselect_enabled"; sml: "deselectEnabled"; type: "bool" }
            Prop { godot: "drag_to_rearrange_enabled"; sml: "dragToRearrangeEnabled"; type: "bool" }
            Prop { godot: "switch_on_drag_hover"; sml: "switchOnDragHover"; type: "bool" }
            Prop { godot: "tab_alignment"; sml: "tabAlignment"; type: "int" }
            Prop { godot: "tab_focus_mode"; sml: "tabFocusMode"; type: "int" }
            Prop { godot: "tabs_position"; sml: "tabsPosition"; type: "int" }
            Prop { godot: "tabs_rearrange_group"; sml: "tabsRearrangeGroup"; type: "int" }
            Prop { godot: "tabs_visible"; sml: "tabsVisible"; type: "bool" }
            Prop { godot: "use_hidden_tabs_for_min_size"; sml: "useHiddenTabsForMinSize"; type: "bool" }
        }

        Events {
            Event { godot: "active_tab_rearranged"; sms: "activeTabRearranged"; params: "int idxTo" }
            Event { godot: "pre_popup_pressed"; sms: "prePopupPressed"; params: "—" }
            Event { godot: "tab_button_pressed"; sms: "tabButtonPressed"; params: "int tab" }
            Event { godot: "tab_changed"; sms: "tabChanged"; params: "int tab" }
            Event { godot: "tab_clicked"; sms: "tabClicked"; params: "int tab" }
            Event { godot: "tab_hovered"; sms: "tabHovered"; params: "int tab" }
            Event { godot: "tab_selected"; sms: "tabSelected"; params: "int tab" }
        }

        ChildContext {
            # These properties are valid on TabContainer children (pages)
            Prop { sml: "tabTitle"; type: "string"; default: "" }
            Prop { sml: "tabIcon"; type: "string"; default: "" }
            Prop { sml: "tabDisabled"; type: "bool"; default: "false" }
            Prop { sml: "tabHidden"; type: "bool"; default: "false" }
        }
    }

    Type {
        name: "TextEdit"
        parent: "Control"
        collection: true

        Properties {
            Prop { godot: "autowrap_mode"; sml: "autowrapMode"; type: "int" }
            Prop { godot: "backspace_deletes_composite_character_enabled"; sml: "backspaceDeletesCompositeCharacterEnabled"; type: "bool" }
            Prop { godot: "caret_blink"; sml: "caretBlink"; type: "bool" }
            Prop { godot: "caret_blink_interval"; sml: "caretBlinkInterval"; type: "float" }
            Prop { godot: "caret_draw_when_editable_disabled"; sml: "caretDrawWhenEditableDisabled"; type: "bool" }
            Prop { godot: "caret_mid_grapheme"; sml: "caretMidGrapheme"; type: "bool" }
            Prop { godot: "caret_move_on_right_click"; sml: "caretMoveOnRightClick"; type: "bool" }
            Prop { godot: "caret_multiple"; sml: "caretMultiple"; type: "bool" }
            Prop { godot: "caret_type"; sml: "caretType"; type: "int" }
            Prop { godot: "context_menu_enabled"; sml: "contextMenuEnabled"; type: "bool" }
            Prop { godot: "custom_word_separators"; sml: "customWordSeparators"; type: "string" }
            Prop { godot: "deselect_on_focus_loss_enabled"; sml: "deselectOnFocusLossEnabled"; type: "bool" }
            Prop { godot: "drag_and_drop_selection_enabled"; sml: "dragAndDropSelectionEnabled"; type: "bool" }
            Prop { godot: "draw_control_chars"; sml: "drawControlChars"; type: "bool" }
            Prop { godot: "draw_spaces"; sml: "drawSpaces"; type: "bool" }
            Prop { godot: "draw_tabs"; sml: "drawTabs"; type: "bool" }
            Prop { godot: "editable"; sml: "editable"; type: "bool" }
            Prop { godot: "emoji_menu_enabled"; sml: "emojiMenuEnabled"; type: "bool" }
            Prop { godot: "empty_selection_clipboard_enabled"; sml: "emptySelectionClipboardEnabled"; type: "bool" }
            Prop { godot: "highlight_all_occurrences"; sml: "highlightAllOccurrences"; type: "bool" }
            Prop { godot: "highlight_current_line"; sml: "highlightCurrentLine"; type: "bool" }
            Prop { godot: "indent_wrapped_lines"; sml: "indentWrappedLines"; type: "bool" }
            Prop { godot: "language"; sml: "language"; type: "string" }
            Prop { godot: "middle_mouse_paste_enabled"; sml: "middleMousePasteEnabled"; type: "bool" }
            Prop { godot: "minimap_draw"; sml: "minimapDraw"; type: "bool" }
            Prop { godot: "minimap_width"; sml: "minimapWidth"; type: "int" }
            Prop { godot: "placeholder_text"; sml: "placeholderText"; type: "string" }
            Prop { godot: "scroll_fit_content_height"; sml: "scrollFitContentHeight"; type: "bool" }
            Prop { godot: "scroll_fit_content_width"; sml: "scrollFitContentWidth"; type: "bool" }
            Prop { godot: "scroll_horizontal"; sml: "scrollHorizontal"; type: "int" }
            Prop { godot: "scroll_past_end_of_file"; sml: "scrollPastEndOfFile"; type: "bool" }
            Prop { godot: "scroll_smooth"; sml: "scrollSmooth"; type: "bool" }
            Prop { godot: "scroll_v_scroll_speed"; sml: "scrollVScrollSpeed"; type: "float" }
            Prop { godot: "scroll_vertical"; sml: "scrollVertical"; type: "float" }
            Prop { godot: "selecting_enabled"; sml: "selectingEnabled"; type: "bool" }
            Prop { godot: "shortcut_keys_enabled"; sml: "shortcutKeysEnabled"; type: "bool" }
            Prop { godot: "structured_text_bidi_override"; sml: "structuredTextBidiOverride"; type: "int" }
            Prop { godot: "tab_input_mode"; sml: "tabInputMode"; type: "bool" }
            Prop { godot: "text"; sml: "text"; type: "string" }
            Prop { godot: "text_direction"; sml: "textDirection"; type: "int" }
            Prop { godot: "use_custom_word_separators"; sml: "useCustomWordSeparators"; type: "bool" }
            Prop { godot: "use_default_word_separators"; sml: "useDefaultWordSeparators"; type: "bool" }
            Prop { godot: "virtual_keyboard_enabled"; sml: "virtualKeyboardEnabled"; type: "bool" }
            Prop { godot: "virtual_keyboard_show_on_focus"; sml: "virtualKeyboardShowOnFocus"; type: "bool" }
            Prop { godot: "wrap_mode"; sml: "wrapMode"; type: "int" }
        }

        Events {
            Event { godot: "caret_changed"; sms: "caretChanged"; params: "—" }
            Event { godot: "gutter_added"; sms: "gutterAdded"; params: "—" }
            Event { godot: "gutter_clicked"; sms: "gutterClicked"; params: "int line, int gutter" }
            Event { godot: "gutter_removed"; sms: "gutterRemoved"; params: "—" }
            Event { godot: "lines_edited_from"; sms: "linesEditedFrom"; params: "int fromLine, int toLine" }
            Event { godot: "text_changed"; sms: "textChanged"; params: "—" }
            Event { godot: "text_set"; sms: "textSet"; params: "—" }
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "TextEditor"
        parent: "ScriptEditorBase"
        collection: true

        Properties {
        }

        Events {
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "TextureButton"
        parent: "BaseButton"
        collection: true

        Properties {
            Prop { godot: "flip_h"; sml: "flipH"; type: "bool" }
            Prop { godot: "flip_v"; sml: "flipV"; type: "bool" }
            Prop { godot: "ignore_texture_size"; sml: "ignoreTextureSize"; type: "bool" }
            Prop { godot: "stretch_mode"; sml: "stretchMode"; type: "int" }
        }

        Events {
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "TextureProgressBar"
        parent: "Range"
        collection: true

        Properties {
            Prop { godot: "fill_mode"; sml: "fillMode"; type: "int" }
            Prop { godot: "nine_patch_stretch"; sml: "ninePatchStretch"; type: "bool" }
            Prop { godot: "radial_center_offset"; sml: "radialCenterOffset"; type: "Vector2" }
            Prop { godot: "radial_fill_degrees"; sml: "radialFillDegrees"; type: "float" }
            Prop { godot: "radial_initial_angle"; sml: "radialInitialAngle"; type: "float" }
            Prop { godot: "stretch_margin_bottom"; sml: "stretchMarginBottom"; type: "int" }
            Prop { godot: "stretch_margin_left"; sml: "stretchMarginLeft"; type: "int" }
            Prop { godot: "stretch_margin_right"; sml: "stretchMarginRight"; type: "int" }
            Prop { godot: "stretch_margin_top"; sml: "stretchMarginTop"; type: "int" }
            Prop { godot: "texture_progress_offset"; sml: "textureProgressOffset"; type: "Vector2" }
            Prop { godot: "tint_over"; sml: "tintOver"; type: "Color" }
            Prop { godot: "tint_progress"; sml: "tintProgress"; type: "Color" }
            Prop { godot: "tint_under"; sml: "tintUnder"; type: "Color" }
        }

        Events {
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "TextureRect"
        parent: "Control"
        collection: true

        Properties {
            Prop { godot: "expand_mode"; sml: "expandMode"; type: "int" }
            Prop { godot: "flip_h"; sml: "flipH"; type: "bool" }
            Prop { godot: "flip_v"; sml: "flipV"; type: "bool" }
            Prop { godot: "stretch_mode"; sml: "stretchMode"; type: "int" }
        }

        Events {
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "ThemeEditor"
        parent: "EditorDock"
        collection: true

        Properties {
        }

        Events {
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "ThemeEditorPreview"
        parent: "VBoxContainer"
        collection: true

        Properties {
        }

        Events {
            Event { godot: "control_picked"; sms: "controlPicked"; params: "string className" }
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "ThemeItemImportTree"
        parent: "VBoxContainer"
        collection: true

        Properties {
        }

        Events {
            Event { godot: "items_imported"; sms: "itemsImported"; params: "—" }
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "ThemeTypeEditor"
        parent: "MarginContainer"
        collection: true

        Properties {
        }

        Events {
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "TileAtlasView"
        parent: "Control"
        collection: true

        Properties {
        }

        Events {
            Event { godot: "transform_changed"; sms: "transformChanged"; params: "float zoom, Vector2 scroll" }
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "TileMapLayerEditor"
        parent: "EditorDock"
        collection: true

        Properties {
        }

        Events {
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "TileSetAtlasSourceEditor"
        parent: "HSplitContainer"
        collection: true

        Properties {
        }

        Events {
            Event { godot: "source_id_changed"; sms: "sourceIdChanged"; params: "int sourceId" }
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "TileSetEditor"
        parent: "EditorDock"
        collection: true

        Properties {
        }

        Events {
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "TileSetScenesCollectionSourceEditor"
        parent: "HBoxContainer"
        collection: true

        Properties {
        }

        Events {
            Event { godot: "source_id_changed"; sms: "sourceIdChanged"; params: "int sourceId" }
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "TileSetSourceItemList"
        parent: "ItemList"
        collection: true

        Properties {
        }

        Events {
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "Tree"
        parent: "Control"
        collection: true

        Properties {
            Prop { godot: "allow_reselect"; sml: "allowReselect"; type: "bool" }
            Prop { godot: "allow_rmb_select"; sml: "allowRmbSelect"; type: "bool" }
            Prop { godot: "allow_search"; sml: "allowSearch"; type: "bool" }
            Prop { godot: "auto_tooltip"; sml: "autoTooltip"; type: "bool" }
            Prop { godot: "column_titles_visible"; sml: "columnTitlesVisible"; type: "bool" }
            Prop { godot: "columns"; sml: "columns"; type: "int" }
            Prop { godot: "drop_mode_flags"; sml: "dropModeFlags"; type: "int" }
            Prop { godot: "enable_drag_unfolding"; sml: "enableDragUnfolding"; type: "bool" }
            Prop { godot: "enable_recursive_folding"; sml: "enableRecursiveFolding"; type: "bool" }
            Prop { godot: "hide_folding"; sml: "hideFolding"; type: "bool" }
            Prop { godot: "hide_root"; sml: "hideRoot"; type: "bool" }
            Prop { godot: "scroll_hint_mode"; sml: "scrollHintMode"; type: "int" }
            Prop { godot: "scroll_horizontal_enabled"; sml: "scrollHorizontalEnabled"; type: "bool" }
            Prop { godot: "scroll_vertical_enabled"; sml: "scrollVerticalEnabled"; type: "bool" }
            Prop { godot: "select_mode"; sml: "selectMode"; type: "int" }
            Prop { godot: "tile_scroll_hint"; sml: "tileScrollHint"; type: "bool" }
        }

        Events {
            Event { godot: "button_clicked"; sms: "buttonClicked"; params: "Object item, int column, int id, int mouseButtonIndex" }
            Event { godot: "cell_selected"; sms: "cellSelected"; params: "—" }
            Event { godot: "check_propagated_to_item"; sms: "checkPropagatedToItem"; params: "Object item, int column" }
            Event { godot: "column_title_clicked"; sms: "columnTitleClicked"; params: "int column, int mouseButtonIndex" }
            Event { godot: "custom_item_clicked"; sms: "customItemClicked"; params: "int mouseButtonIndex" }
            Event { godot: "custom_popup_edited"; sms: "customPopupEdited"; params: "bool arrowClicked" }
            Event { godot: "empty_clicked"; sms: "emptyClicked"; params: "Vector2 clickPosition, int mouseButtonIndex" }
            Event { godot: "item_activated"; sms: "itemActivated"; params: "—" }
            Event { godot: "item_collapsed"; sms: "itemCollapsed"; params: "Object item" }
            Event { godot: "item_edited"; sms: "itemEdited"; params: "—" }
            Event { godot: "item_icon_double_clicked"; sms: "itemIconDoubleClicked"; params: "—" }
            Event { godot: "item_mouse_selected"; sms: "itemMouseSelected"; params: "Vector2 mousePosition, int mouseButtonIndex" }
            Event { godot: "item_selected"; sms: "itemSelected"; params: "—" }
            Event { godot: "multi_selected"; sms: "multiSelected"; params: "Object item, int column, bool selected" }
            Event { godot: "nothing_selected"; sms: "nothingSelected"; params: "—" }
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "VBoxContainer"
        parent: "BoxContainer"
        collection: true

        Properties {
        }

        Events {
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "VFlowContainer"
        parent: "FlowContainer"
        collection: true

        Properties {
        }

        Events {
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "VScrollBar"
        parent: "ScrollBar"
        collection: true

        Properties {
        }

        Events {
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "VSeparator"
        parent: "Separator"
        collection: true

        Properties {
        }

        Events {
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "VSlider"
        parent: "Slider"
        collection: true

        Properties {
        }

        Events {
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "VSplitContainer"
        parent: "SplitContainer"
        collection: true

        Properties {
        }

        Events {
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "VideoStreamPlayer"
        parent: "Control"
        collection: true

        Properties {
            Prop { godot: "audio_track"; sml: "audioTrack"; type: "int" }
            Prop { godot: "autoplay"; sml: "autoplay"; type: "bool" }
            Prop { godot: "buffering_msec"; sml: "bufferingMsec"; type: "int" }
            Prop { godot: "expand"; sml: "expand"; type: "bool" }
            Prop { godot: "loop"; sml: "loop"; type: "bool" }
            Prop { godot: "paused"; sml: "paused"; type: "bool" }
            Prop { godot: "speed_scale"; sml: "speedScale"; type: "float" }
            Prop { godot: "volume_db"; sml: "volumeDb"; type: "float" }
        }

        Events {
            Event { godot: "finished"; sms: "finished"; params: "—" }
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "Viewport"
        parent: "Node"

        Properties {
            Prop { godot: "anisotropic_filtering_level"; sml: "anisotropicFilteringLevel"; type: "int" }
            Prop { godot: "audio_listener_enable_2d"; sml: "audioListenerEnable2d"; type: "bool" }
            Prop { godot: "audio_listener_enable_3d"; sml: "audioListenerEnable3d"; type: "bool" }
            Prop { godot: "canvas_cull_mask"; sml: "canvasCullMask"; type: "int" }
            Prop { godot: "canvas_item_default_texture_filter"; sml: "canvasItemDefaultTextureFilter"; type: "int" }
            Prop { godot: "canvas_item_default_texture_repeat"; sml: "canvasItemDefaultTextureRepeat"; type: "int" }
            Prop { godot: "debug_draw"; sml: "debugDraw"; type: "int" }
            Prop { godot: "disable_3d"; sml: "disable3d"; type: "bool" }
            Prop { godot: "fsr_sharpness"; sml: "fsrSharpness"; type: "float" }
            Prop { godot: "gui_disable_input"; sml: "guiDisableInput"; type: "bool" }
            Prop { godot: "gui_drag_threshold"; sml: "guiDragThreshold"; type: "int" }
            Prop { godot: "gui_embed_subwindows"; sml: "guiEmbedSubwindows"; type: "bool" }
            Prop { godot: "gui_snap_controls_to_pixels"; sml: "guiSnapControlsToPixels"; type: "bool" }
            Prop { godot: "handle_input_locally"; sml: "handleInputLocally"; type: "bool" }
            Prop { godot: "mesh_lod_threshold"; sml: "meshLodThreshold"; type: "float" }
            Prop { godot: "msaa_2d"; sml: "msaa2d"; type: "int" }
            Prop { godot: "msaa_3d"; sml: "msaa3d"; type: "int" }
            Prop { godot: "oversampling"; sml: "oversampling"; type: "bool" }
            Prop { godot: "oversampling_override"; sml: "oversamplingOverride"; type: "float" }
            Prop { godot: "own_world_3d"; sml: "ownWorld3d"; type: "bool" }
            Prop { godot: "physics_object_picking"; sml: "physicsObjectPicking"; type: "bool" }
            Prop { godot: "physics_object_picking_first_only"; sml: "physicsObjectPickingFirstOnly"; type: "bool" }
            Prop { godot: "physics_object_picking_sort"; sml: "physicsObjectPickingSort"; type: "bool" }
            Prop { godot: "positional_shadow_atlas_16_bits"; sml: "positionalShadowAtlas16Bits"; type: "bool" }
            Prop { godot: "positional_shadow_atlas_quad_0"; sml: "positionalShadowAtlasQuad0"; type: "int" }
            Prop { godot: "positional_shadow_atlas_quad_1"; sml: "positionalShadowAtlasQuad1"; type: "int" }
            Prop { godot: "positional_shadow_atlas_quad_2"; sml: "positionalShadowAtlasQuad2"; type: "int" }
            Prop { godot: "positional_shadow_atlas_quad_3"; sml: "positionalShadowAtlasQuad3"; type: "int" }
            Prop { godot: "positional_shadow_atlas_size"; sml: "positionalShadowAtlasSize"; type: "int" }
            Prop { godot: "scaling_3d_mode"; sml: "scaling3dMode"; type: "int" }
            Prop { godot: "scaling_3d_scale"; sml: "scaling3dScale"; type: "float" }
            Prop { godot: "screen_space_aa"; sml: "screenSpaceAa"; type: "int" }
            Prop { godot: "sdf_oversize"; sml: "sdfOversize"; type: "int" }
            Prop { godot: "sdf_scale"; sml: "sdfScale"; type: "int" }
            Prop { godot: "snap_2d_transforms_to_pixel"; sml: "snap2dTransformsToPixel"; type: "bool" }
            Prop { godot: "snap_2d_vertices_to_pixel"; sml: "snap2dVerticesToPixel"; type: "bool" }
            Prop { godot: "texture_mipmap_bias"; sml: "textureMipmapBias"; type: "float" }
            Prop { godot: "transparent_bg"; sml: "transparentBg"; type: "bool" }
            Prop { godot: "use_debanding"; sml: "useDebanding"; type: "bool" }
            Prop { godot: "use_hdr_2d"; sml: "useHdr2d"; type: "bool" }
            Prop { godot: "use_occlusion_culling"; sml: "useOcclusionCulling"; type: "bool" }
            Prop { godot: "use_taa"; sml: "useTaa"; type: "bool" }
            Prop { godot: "use_xr"; sml: "useXr"; type: "bool" }
            Prop { godot: "vrs_mode"; sml: "vrsMode"; type: "int" }
            Prop { godot: "vrs_update_mode"; sml: "vrsUpdateMode"; type: "int" }
        }

        Events {
            Event { godot: "gui_focus_changed"; sms: "guiFocusChanged"; params: "Object node" }
            Event { godot: "size_changed"; sms: "sizeChanged"; params: "—" }
        }
    }

    Type {
        name: "ViewportNavigationControl"
        parent: "Control"
        collection: true

        Properties {
        }

        Events {
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "ViewportRotationControl"
        parent: "Control"
        collection: true

        Properties {
        }

        Events {
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "Window"
        parent: "Viewport"

        Properties {
            Prop { godot: "accessibility_description"; sml: "accessibilityDescription"; type: "string" }
            Prop { godot: "accessibility_name"; sml: "accessibilityName"; type: "string" }
            Prop { godot: "always_on_top"; sml: "alwaysOnTop"; type: "bool" }
            Prop { godot: "borderless"; sml: "borderless"; type: "bool" }
            Prop { godot: "content_scale_aspect"; sml: "contentScaleAspect"; type: "int" }
            Prop { godot: "content_scale_factor"; sml: "contentScaleFactor"; type: "float" }
            Prop { godot: "content_scale_mode"; sml: "contentScaleMode"; type: "int" }
            Prop { godot: "content_scale_stretch"; sml: "contentScaleStretch"; type: "int" }
            Prop { godot: "current_screen"; sml: "currentScreen"; type: "int" }
            Prop { godot: "exclude_from_capture"; sml: "excludeFromCapture"; type: "bool" }
            Prop { godot: "exclusive"; sml: "exclusive"; type: "bool" }
            Prop { godot: "extend_to_title"; sml: "extendToTitle"; type: "bool" }
            Prop { godot: "force_native"; sml: "forceNative"; type: "bool" }
            Prop { godot: "initial_position"; sml: "initialPosition"; type: "int" }
            Prop { godot: "keep_title_visible"; sml: "keepTitleVisible"; type: "bool" }
            Prop { godot: "maximize_disabled"; sml: "maximizeDisabled"; type: "bool" }
            Prop { godot: "minimize_disabled"; sml: "minimizeDisabled"; type: "bool" }
            Prop { godot: "mode"; sml: "mode"; type: "int" }
            Prop { godot: "mouse_passthrough"; sml: "mousePassthrough"; type: "bool" }
            Prop { godot: "popup_window"; sml: "popupWindow"; type: "bool" }
            Prop { godot: "popup_wm_hint"; sml: "popupWmHint"; type: "bool" }
            Prop { godot: "sharp_corners"; sml: "sharpCorners"; type: "bool" }
            Prop { godot: "theme_type_variation"; sml: "themeTypeVariation"; type: "string" }
            Prop { godot: "title"; sml: "title"; type: "string" }
            Prop { godot: "transient"; sml: "transient"; type: "bool" }
            Prop { godot: "transient_to_focused"; sml: "transientToFocused"; type: "bool" }
            Prop { godot: "transparent"; sml: "transparent"; type: "bool" }
            Prop { godot: "unfocusable"; sml: "unfocusable"; type: "bool" }
            Prop { godot: "unresizable"; sml: "unresizable"; type: "bool" }
            Prop { godot: "visible"; sml: "visible"; type: "bool" }
            Prop { godot: "wrap_controls"; sml: "wrapControls"; type: "bool" }
        }

        Events {
            Event { godot: "about_to_popup"; sms: "aboutToPopup"; params: "—" }
            Event { godot: "close_requested"; sms: "closeRequested"; params: "—" }
            Event { godot: "dpi_changed"; sms: "dpiChanged"; params: "—" }
            Event { godot: "files_dropped"; sms: "filesDropped"; params: "Variant files" }
            Event { godot: "focus_entered"; sms: "focusEntered"; params: "—" }
            Event { godot: "focus_exited"; sms: "focusExited"; params: "—" }
            Event { godot: "go_back_requested"; sms: "goBackRequested"; params: "—" }
            Event { godot: "mouse_entered"; sms: "mouseEntered"; params: "—" }
            Event { godot: "mouse_exited"; sms: "mouseExited"; params: "—" }
            Event { godot: "nonclient_window_input"; sms: "nonclientWindowInput"; params: "Object event" }
            Event { godot: "theme_changed"; sms: "themeChanged"; params: "—" }
            Event { godot: "title_changed"; sms: "titleChanged"; params: "—" }
            Event { godot: "titlebar_changed"; sms: "titlebarChanged"; params: "—" }
            Event { godot: "visibility_changed"; sms: "visibilityChanged"; params: "—" }
            Event { godot: "window_input"; sms: "windowInput"; params: "Object event" }
        }
    }

    Type {
        name: "WindowWrapper"
        parent: "MarginContainer"
        collection: true

        Properties {
        }

        Events {
            Event { godot: "window_close_requested"; sms: "windowCloseRequested"; params: "—" }
            Event { godot: "window_size_changed"; sms: "windowSizeChanged"; params: "—" }
            Event { godot: "window_visibility_changed"; sms: "windowVisibilityChanged"; params: "bool visible" }
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

}
