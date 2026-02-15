# TextEdit

## Inheritance

[TextEdit](TextEdit.md) → [Control](Control.md) → [CanvasItem](CanvasItem.md) → [Node](Node.md) → [Object](Object.md)

## Collection Items

This control appears to manage internal **items** (collection-style API).
Items are typically not represented as child nodes/properties in Godot.
In SML, this will be represented via **pseudo child elements** (documented per control).

## Derived Classes

### Direct subclasses

- [CodeEdit](CodeEdit.md)

## Properties

This page lists **only properties declared by `TextEdit`**.
Inherited properties are documented in: [Control](Control.md)

| Godot Property | SML Property | Type | Default |
|-|-|-|-|
| autowrap_mode | autowrapMode | int | — |
| backspace_deletes_composite_character_enabled | backspaceDeletesCompositeCharacterEnabled | bool | — |
| caret_blink | caretBlink | bool | — |
| caret_blink_interval | caretBlinkInterval | float | — |
| caret_draw_when_editable_disabled | caretDrawWhenEditableDisabled | bool | — |
| caret_mid_grapheme | caretMidGrapheme | bool | — |
| caret_move_on_right_click | caretMoveOnRightClick | bool | — |
| caret_multiple | caretMultiple | bool | — |
| caret_type | caretType | int | — |
| context_menu_enabled | contextMenuEnabled | bool | — |
| custom_word_separators | customWordSeparators | string | — |
| deselect_on_focus_loss_enabled | deselectOnFocusLossEnabled | bool | — |
| drag_and_drop_selection_enabled | dragAndDropSelectionEnabled | bool | — |
| draw_control_chars | drawControlChars | bool | — |
| draw_spaces | drawSpaces | bool | — |
| draw_tabs | drawTabs | bool | — |
| editable | editable | bool | — |
| emoji_menu_enabled | emojiMenuEnabled | bool | — |
| empty_selection_clipboard_enabled | emptySelectionClipboardEnabled | bool | — |
| highlight_all_occurrences | highlightAllOccurrences | bool | — |
| highlight_current_line | highlightCurrentLine | bool | — |
| indent_wrapped_lines | indentWrappedLines | bool | — |
| language | language | string | — |
| middle_mouse_paste_enabled | middleMousePasteEnabled | bool | — |
| minimap_draw | minimapDraw | bool | — |
| minimap_width | minimapWidth | int | — |
| placeholder_text | placeholderText | string | — |
| scroll_fit_content_height | scrollFitContentHeight | bool | — |
| scroll_fit_content_width | scrollFitContentWidth | bool | — |
| scroll_horizontal | scrollHorizontal | int | — |
| scroll_past_end_of_file | scrollPastEndOfFile | bool | — |
| scroll_smooth | scrollSmooth | bool | — |
| scroll_v_scroll_speed | scrollVScrollSpeed | float | — |
| scroll_vertical | scrollVertical | float | — |
| selecting_enabled | selectingEnabled | bool | — |
| shortcut_keys_enabled | shortcutKeysEnabled | bool | — |
| structured_text_bidi_override | structuredTextBidiOverride | int | — |
| tab_input_mode | tabInputMode | bool | — |
| text | text | string | — |
| text_direction | textDirection | int | — |
| use_custom_word_separators | useCustomWordSeparators | bool | — |
| use_default_word_separators | useDefaultWordSeparators | bool | — |
| virtual_keyboard_enabled | virtualKeyboardEnabled | bool | — |
| virtual_keyboard_show_on_focus | virtualKeyboardShowOnFocus | bool | — |
| wrap_mode | wrapMode | int | — |

## Events

This page lists **only signals declared by `TextEdit`**.
Inherited signals are documented in: [Control](Control.md)

| Godot Signal | SMS Event | Params |
|-|-|-|
| caret_changed | `on <id>.caretChanged() { ... }` | — |
| gutter_added | `on <id>.gutterAdded() { ... }` | — |
| gutter_clicked | `on <id>.gutterClicked(line, gutter) { ... }` | int line, int gutter |
| gutter_removed | `on <id>.gutterRemoved() { ... }` | — |
| lines_edited_from | `on <id>.linesEditedFrom(fromLine, toLine) { ... }` | int fromLine, int toLine |
| text_changed | `on <id>.textChanged() { ... }` | — |
| text_set | `on <id>.textSet() { ... }` | — |

## SML Items (TODO)

This control appears to manage internal items, but a dedicated SML pseudo-child specification has not been defined yet.
Use the generated signals and the `collection: true` marker in `sms-reference.sml` as implementation hints.
