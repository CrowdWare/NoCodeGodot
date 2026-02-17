Reference {
    generator: "generate_sml_element_docs.gd"
    propertyFilter: "inspector-editable + supported scalar types"
    naming: "snake_case -> lowerCamelCase"

    Type {
        name: "AcceptDialog"
        parent: "Window"

        Properties {
            Prop { godot: "dialog_autowrap"; sml: "dialogAutowrap"; type: "bool" }
            Prop { godot: "dialog_close_on_escape"; sml: "dialogCloseOnEscape"; type: "bool" }
            Prop { godot: "dialog_hide_on_ok"; sml: "dialogHideOnOk"; type: "bool" }
            Prop { godot: "dialog_text"; sml: "dialogText"; type: "string" }
            Prop { godot: "ok_button_text"; sml: "okButtonText"; type: "string" }
        }

        Events {
            Event { godot: "canceled"; sms: "canceled"; params: "—" }
            Event { godot: "confirmed"; sms: "confirmed"; params: "—" }
            Event { godot: "custom_action"; sms: "customAction"; params: "Variant action" }
        }

        Actions {
        }
    }

    Type {
        name: "AspectRatioContainer"
        parent: "Container"

        Properties {
            Prop { godot: "alignment_horizontal"; sml: "alignmentHorizontal"; type: "int" }
            Prop { godot: "alignment_vertical"; sml: "alignmentVertical"; type: "int" }
            Prop { godot: "ratio"; sml: "ratio"; type: "float" }
            Prop { godot: "stretch_mode"; sml: "stretchMode"; type: "int" }
        }

        Events {
        }

        Actions {
        }
    }

    Type {
        name: "BaseButton"
        parent: "Control"

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

        Actions {
        }
    }

    Type {
        name: "BoxContainer"
        parent: "Container"

        Properties {
            Prop { godot: "alignment"; sml: "alignment"; type: "int" }
            Prop { godot: "vertical"; sml: "vertical"; type: "bool" }
        }

        Events {
        }

        Actions {
        }
    }

    Type {
        name: "Button"
        parent: "BaseButton"

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

        Actions {
        }
    }

    Type {
        name: "CenterContainer"
        parent: "Container"

        Properties {
            Prop { godot: "use_top_left"; sml: "useTopLeft"; type: "bool" }
        }

        Events {
        }

        Actions {
        }
    }

    Type {
        name: "CheckBox"
        parent: "Button"

        Properties {
        }

        Events {
        }

        Actions {
        }
    }

    Type {
        name: "CheckButton"
        parent: "Button"

        Properties {
        }

        Events {
        }

        Actions {
        }
    }

    Type {
        name: "CodeEdit"
        parent: "TextEdit"

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

        Actions {
        }
    }

    Type {
        name: "ColorPicker"
        parent: "VBoxContainer"

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

        Actions {
        }
    }

    Type {
        name: "ColorPickerButton"
        parent: "Button"

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

        Actions {
        }
    }

    Type {
        name: "ColorRect"
        parent: "Control"

        Properties {
            Prop { godot: "color"; sml: "color"; type: "Color" }
        }

        Events {
        }

        Actions {
        }
    }

    Type {
        name: "ConfirmationDialog"
        parent: "AcceptDialog"

        Properties {
            Prop { godot: "cancel_button_text"; sml: "cancelButtonText"; type: "string" }
        }

        Events {
        }

        Actions {
        }
    }

    Type {
        name: "Container"
        parent: "Control"

        Properties {
        }

        Events {
            Event { godot: "pre_sort_children"; sms: "preSortChildren"; params: "—" }
            Event { godot: "sort_children"; sms: "sortChildren"; params: "—" }
        }

        Actions {
        }
    }

    Type {
        name: "Control"
        parent: "CanvasItem"

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

        Actions {
        }
    }

    Type {
        name: "FileDialog"
        parent: "ConfirmationDialog"

        Properties {
            Prop { godot: "access"; sml: "access"; type: "int" }
            Prop { godot: "deleting_enabled"; sml: "deletingEnabled"; type: "bool" }
            Prop { godot: "display_mode"; sml: "displayMode"; type: "int" }
            Prop { godot: "favorites_enabled"; sml: "favoritesEnabled"; type: "bool" }
            Prop { godot: "file_filter_toggle_enabled"; sml: "fileFilterToggleEnabled"; type: "bool" }
            Prop { godot: "file_mode"; sml: "fileMode"; type: "int" }
            Prop { godot: "file_sort_options_enabled"; sml: "fileSortOptionsEnabled"; type: "bool" }
            Prop { godot: "filename_filter"; sml: "filenameFilter"; type: "string" }
            Prop { godot: "folder_creation_enabled"; sml: "folderCreationEnabled"; type: "bool" }
            Prop { godot: "hidden_files_toggle_enabled"; sml: "hiddenFilesToggleEnabled"; type: "bool" }
            Prop { godot: "layout_toggle_enabled"; sml: "layoutToggleEnabled"; type: "bool" }
            Prop { godot: "mode_overrides_title"; sml: "modeOverridesTitle"; type: "bool" }
            Prop { godot: "option_count"; sml: "optionCount"; type: "int" }
            Prop { godot: "overwrite_warning_enabled"; sml: "overwriteWarningEnabled"; type: "bool" }
            Prop { godot: "recent_list_enabled"; sml: "recentListEnabled"; type: "bool" }
            Prop { godot: "root_subfolder"; sml: "rootSubfolder"; type: "string" }
            Prop { godot: "show_hidden_files"; sml: "showHiddenFiles"; type: "bool" }
            Prop { godot: "use_native_dialog"; sml: "useNativeDialog"; type: "bool" }
        }

        Events {
            Event { godot: "dir_selected"; sms: "dirSelected"; params: "string dir" }
            Event { godot: "file_selected"; sms: "fileSelected"; params: "string path" }
            Event { godot: "filename_filter_changed"; sms: "filenameFilterChanged"; params: "string filter" }
            Event { godot: "files_selected"; sms: "filesSelected"; params: "Variant paths" }
        }

        Actions {
        }
    }

    Type {
        name: "FileSystemDock"
        parent: "EditorDock"

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

        Actions {
        }
    }

    Type {
        name: "FlowContainer"
        parent: "Container"

        Properties {
            Prop { godot: "alignment"; sml: "alignment"; type: "int" }
            Prop { godot: "last_wrap_alignment"; sml: "lastWrapAlignment"; type: "int" }
            Prop { godot: "reverse_fill"; sml: "reverseFill"; type: "bool" }
            Prop { godot: "vertical"; sml: "vertical"; type: "bool" }
        }

        Events {
        }

        Actions {
        }
    }

    Type {
        name: "FoldableContainer"
        parent: "Container"

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

        Actions {
        }
    }

    Type {
        name: "GraphEdit"
        parent: "Control"

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

        Actions {
        }
    }

    Type {
        name: "GraphElement"
        parent: "Container"

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

        Actions {
        }
    }

    Type {
        name: "GraphFrame"
        parent: "GraphElement"

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

        Actions {
        }
    }

    Type {
        name: "GraphNode"
        parent: "GraphElement"

        Properties {
            Prop { godot: "ignore_invalid_connection_type"; sml: "ignoreInvalidConnectionType"; type: "bool" }
            Prop { godot: "slots_focus_mode"; sml: "slotsFocusMode"; type: "int" }
            Prop { godot: "title"; sml: "title"; type: "string" }
        }

        Events {
            Event { godot: "slot_sizes_changed"; sms: "slotSizesChanged"; params: "—" }
            Event { godot: "slot_updated"; sms: "slotUpdated"; params: "int slotIndex" }
        }

        Actions {
        }
    }

    Type {
        name: "GridContainer"
        parent: "Container"

        Properties {
            Prop { godot: "columns"; sml: "columns"; type: "int" }
        }

        Events {
        }

        Actions {
        }
    }

    Type {
        name: "HBoxContainer"
        parent: "BoxContainer"

        Properties {
        }

        Events {
        }

        Actions {
        }
    }

    Type {
        name: "HFlowContainer"
        parent: "FlowContainer"

        Properties {
        }

        Events {
        }

        Actions {
        }
    }

    Type {
        name: "HScrollBar"
        parent: "ScrollBar"

        Properties {
        }

        Events {
        }

        Actions {
        }
    }

    Type {
        name: "HSeparator"
        parent: "Separator"

        Properties {
        }

        Events {
        }

        Actions {
        }
    }

    Type {
        name: "HSlider"
        parent: "Slider"

        Properties {
        }

        Events {
        }

        Actions {
        }
    }

    Type {
        name: "HSplitContainer"
        parent: "SplitContainer"

        Properties {
        }

        Events {
        }

        Actions {
        }
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

        Actions {
        }

        PseudoChildren {
            Item { props: "id,text,icon,selected,disabled,tooltip" }
        }
    }

    Type {
        name: "Label"
        parent: "Control"

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

        Actions {
        }
    }

    Type {
        name: "LineEdit"
        parent: "Control"

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

        Actions {
        }
    }

    Type {
        name: "LinkButton"
        parent: "BaseButton"

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

        Actions {
        }
    }

    Type {
        name: "MarginContainer"
        parent: "Container"

        Properties {
        }

        Events {
        }

        Actions {
        }
    }

    Type {
        name: "Markdown"
        parent: "VBoxContainer"

        Properties {
            Prop { sml: "id"; type: "identifier"; default: "—" }
            Prop { sml: "padding"; type: "padding"; default: "0" }
            Prop { sml: "text"; type: "string"; default: """" }
            Prop { sml: "src"; type: "string"; default: """" }
        }

        Events {
        }

        Actions {
        }
    }

    Type {
        name: "MenuBar"
        parent: "Control"

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

        Actions {
        }
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

        Actions {
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "NinePatchRect"
        parent: "Control"

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

        Actions {
        }
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

        Actions {
        }

        PseudoChildren {
            Item { props: "id,text,icon,disabled,selected" }
        }
    }

    Type {
        name: "Panel"
        parent: "Control"

        Properties {
        }

        Events {
        }

        Actions {
        }
    }

    Type {
        name: "PanelContainer"
        parent: "Container"

        Properties {
        }

        Events {
        }

        Actions {
        }
    }

    Type {
        name: "Popup"
        parent: "Window"

        Properties {
        }

        Events {
            Event { godot: "popup_hide"; sms: "popupHide"; params: "—" }
        }

        Actions {
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

        Actions {
        }

        PseudoChildren {
            Item { props: "id,text,disabled" }
            CheckItem { props: "id,text,checked,disabled" }
            Separator { props: "" }
        }
    }

    Type {
        name: "PopupPanel"
        parent: "Popup"

        Properties {
        }

        Events {
        }

        Actions {
        }
    }

    Type {
        name: "ProgressBar"
        parent: "Range"

        Properties {
            Prop { godot: "editor_preview_indeterminate"; sml: "editorPreviewIndeterminate"; type: "bool" }
            Prop { godot: "fill_mode"; sml: "fillMode"; type: "int" }
            Prop { godot: "indeterminate"; sml: "indeterminate"; type: "bool" }
            Prop { godot: "show_percentage"; sml: "showPercentage"; type: "bool" }
        }

        Events {
        }

        Actions {
        }
    }

    Type {
        name: "Range"
        parent: "Control"

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

        Actions {
        }
    }

    Type {
        name: "ReferenceRect"
        parent: "Control"

        Properties {
            Prop { godot: "border_color"; sml: "borderColor"; type: "Color" }
            Prop { godot: "border_width"; sml: "borderWidth"; type: "float" }
            Prop { godot: "editor_only"; sml: "editorOnly"; type: "bool" }
        }

        Events {
        }

        Actions {
        }
    }

    Type {
        name: "RichTextLabel"
        parent: "Control"

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

        Actions {
        }
    }

    Type {
        name: "ScriptEditor"
        parent: "PanelContainer"

        Properties {
        }

        Events {
            Event { godot: "editor_script_changed"; sms: "editorScriptChanged"; params: "Object script" }
            Event { godot: "script_close"; sms: "scriptClose"; params: "Object script" }
        }

        Actions {
        }
    }

    Type {
        name: "ScriptEditorBase"
        parent: "VBoxContainer"

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

        Actions {
        }
    }

    Type {
        name: "ScrollBar"
        parent: "Range"

        Properties {
            Prop { godot: "custom_step"; sml: "customStep"; type: "float" }
        }

        Events {
            Event { godot: "scrolling"; sms: "scrolling"; params: "—" }
        }

        Actions {
        }
    }

    Type {
        name: "ScrollContainer"
        parent: "Container"

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

        Actions {
        }
    }

    Type {
        name: "Separator"
        parent: "Control"

        Properties {
        }

        Events {
        }

        Actions {
        }
    }

    Type {
        name: "Slider"
        parent: "Range"

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

        Actions {
        }
    }

    Type {
        name: "SpinBox"
        parent: "Range"

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

        Actions {
        }
    }

    Type {
        name: "SplitContainer"
        parent: "Container"

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

        Actions {
        }
    }

    Type {
        name: "SubViewport"
        parent: "Viewport"

        Properties {
            Prop { godot: "render_target_clear_mode"; sml: "renderTargetClearMode"; type: "int" }
            Prop { godot: "render_target_update_mode"; sml: "renderTargetUpdateMode"; type: "int" }
            Prop { godot: "size_2d_override_stretch"; sml: "size2dOverrideStretch"; type: "bool" }
        }

        Events {
        }

        Actions {
        }
    }

    Type {
        name: "SubViewportContainer"
        parent: "Container"

        Properties {
            Prop { godot: "mouse_target"; sml: "mouseTarget"; type: "bool" }
            Prop { godot: "stretch"; sml: "stretch"; type: "bool" }
            Prop { godot: "stretch_shrink"; sml: "stretchShrink"; type: "int" }
        }

        Events {
        }

        Actions {
        }
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

        Actions {
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

        Actions {
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

        Actions {
        }
    }

    Type {
        name: "TextureButton"
        parent: "BaseButton"

        Properties {
            Prop { godot: "flip_h"; sml: "flipH"; type: "bool" }
            Prop { godot: "flip_v"; sml: "flipV"; type: "bool" }
            Prop { godot: "ignore_texture_size"; sml: "ignoreTextureSize"; type: "bool" }
            Prop { godot: "stretch_mode"; sml: "stretchMode"; type: "int" }
        }

        Events {
        }

        Actions {
        }
    }

    Type {
        name: "TextureProgressBar"
        parent: "Range"

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

        Actions {
        }
    }

    Type {
        name: "TextureRect"
        parent: "Control"

        Properties {
            Prop { godot: "expand_mode"; sml: "expandMode"; type: "int" }
            Prop { godot: "flip_h"; sml: "flipH"; type: "bool" }
            Prop { godot: "flip_v"; sml: "flipV"; type: "bool" }
            Prop { godot: "stretch_mode"; sml: "stretchMode"; type: "int" }
        }

        Events {
        }

        Actions {
        }
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

        Actions {
        }

        # TODO: Define PseudoChildren schema for this collection control
    }

    Type {
        name: "VBoxContainer"
        parent: "BoxContainer"

        Properties {
        }

        Events {
        }

        Actions {
        }
    }

    Type {
        name: "VFlowContainer"
        parent: "FlowContainer"

        Properties {
        }

        Events {
        }

        Actions {
        }
    }

    Type {
        name: "VScrollBar"
        parent: "ScrollBar"

        Properties {
        }

        Events {
        }

        Actions {
        }
    }

    Type {
        name: "VSeparator"
        parent: "Separator"

        Properties {
        }

        Events {
        }

        Actions {
        }
    }

    Type {
        name: "VSlider"
        parent: "Slider"

        Properties {
        }

        Events {
        }

        Actions {
        }
    }

    Type {
        name: "VSplitContainer"
        parent: "SplitContainer"

        Properties {
        }

        Events {
        }

        Actions {
        }
    }

    Type {
        name: "VideoStreamPlayer"
        parent: "Control"

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

        Actions {
        }
    }

    Type {
        name: "Viewport3D"
        parent: "SubViewportContainer"

        Properties {
            Prop { sml: "id"; type: "identifier"; default: "—" }
            Prop { sml: "model"; type: "string(url)"; default: """" }
            Prop { sml: "modelSource"; type: "string(url)"; default: """" }
            Prop { sml: "animation"; type: "string(url)"; default: """" }
            Prop { sml: "animationSource"; type: "string(url)"; default: """" }
            Prop { sml: "playAnimation"; type: "int"; default: "0" }
            Prop { sml: "playFirstAnimation"; type: "bool"; default: "false" }
            Prop { sml: "autoplayAnimation"; type: "bool"; default: "false" }
            Prop { sml: "defaultAnimation"; type: "string"; default: """" }
            Prop { sml: "playLoop"; type: "bool"; default: "false" }
            Prop { sml: "cameraDistance"; type: "int"; default: "0" }
            Prop { sml: "lightEnergy"; type: "int"; default: "0" }
        }

        Events {
        }

        Actions {
            Action { sms: "playAnimation"; params: "int index"; returns: "void" }
            Action { sms: "stopAnimation"; params: "—"; returns: "void" }
            Action { sms: "rewind"; params: "—"; returns: "void" }
            Action { sms: "setFrame"; params: "int frame"; returns: "void" }
            Action { sms: "zoomIn"; params: "—"; returns: "void" }
            Action { sms: "zoomOut"; params: "—"; returns: "void" }
        }
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

        Actions {
        }
    }

}
