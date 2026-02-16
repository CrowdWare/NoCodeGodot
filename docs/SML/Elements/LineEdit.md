# LineEdit

## Inheritance

[LineEdit](LineEdit.md) → [Control](Control.md) → [CanvasItem](CanvasItem.md) → [Node](Node.md) → [Object](Object.md)

## Properties

This page lists **only properties declared by `LineEdit`**.
Inherited properties are documented in: [Control](Control.md)

| Godot Property | SML Property | Type | Default |
|-|-|-|-|
| alignment | alignment | int | — |
| backspace_deletes_composite_character_enabled | backspaceDeletesCompositeCharacterEnabled | bool | — |
| caret_blink | caretBlink | bool | — |
| caret_blink_interval | caretBlinkInterval | float | — |
| caret_column | caretColumn | int | — |
| caret_force_displayed | caretForceDisplayed | bool | — |
| caret_mid_grapheme | caretMidGrapheme | bool | — |
| clear_button_enabled | clearButtonEnabled | bool | — |
| context_menu_enabled | contextMenuEnabled | bool | — |
| deselect_on_focus_loss_enabled | deselectOnFocusLossEnabled | bool | — |
| drag_and_drop_selection_enabled | dragAndDropSelectionEnabled | bool | — |
| draw_control_chars | drawControlChars | bool | — |
| editable | editable | bool | — |
| emoji_menu_enabled | emojiMenuEnabled | bool | — |
| expand_to_text_length | expandToTextLength | bool | — |
| flat | flat | bool | — |
| icon_expand_mode | iconExpandMode | int | — |
| keep_editing_on_text_submit | keepEditingOnTextSubmit | bool | — |
| language | language | string | — |
| max_length | maxLength | int | — |
| middle_mouse_paste_enabled | middleMousePasteEnabled | bool | — |
| placeholder_text | placeholderText | string | — |
| right_icon_scale | rightIconScale | float | — |
| secret | secret | bool | — |
| secret_character | secretCharacter | string | — |
| select_all_on_focus | selectAllOnFocus | bool | — |
| selecting_enabled | selectingEnabled | bool | — |
| shortcut_keys_enabled | shortcutKeysEnabled | bool | — |
| structured_text_bidi_override | structuredTextBidiOverride | int | — |
| text | text | string | — |
| text_direction | textDirection | int | — |
| virtual_keyboard_enabled | virtualKeyboardEnabled | bool | — |
| virtual_keyboard_show_on_focus | virtualKeyboardShowOnFocus | bool | — |
| virtual_keyboard_type | virtualKeyboardType | int | — |

## Events

This page lists **only signals declared by `LineEdit`**.
Inherited signals are documented in: [Control](Control.md)

| Godot Signal | SMS Event | Params |
|-|-|-|
| editing_toggled | `on <id>.editingToggled(toggledOn) { ... }` | bool toggledOn |
| text_change_rejected | `on <id>.textChangeRejected(rejectedSubstring) { ... }` | string rejectedSubstring |
| text_changed | `on <id>.textChanged(newText) { ... }` | string newText |
| text_submitted | `on <id>.textSubmitted(newText) { ... }` | string newText |

## Runtime Actions

This page lists **callable methods declared by `LineEdit`**.
Inherited actions are documented in: [Control](Control.md)

| Godot Method | SMS Call | Params | Returns |
|-|-|-|-|
| apply_ime | `<id>.applyIme()` | — | void |
| cancel_ime | `<id>.cancelIme()` | — | void |
| clear | `<id>.clear()` | — | void |
| delete_char_at_caret | `<id>.deleteCharAtCaret()` | — | void |
| delete_text | `<id>.deleteText(fromColumn, toColumn)` | int fromColumn, int toColumn | void |
| deselect | `<id>.deselect()` | — | void |
| edit | `<id>.edit(hideFocus)` | bool hideFocus | void |
| get_horizontal_alignment | `<id>.getHorizontalAlignment()` | — | int |
| get_menu | `<id>.getMenu()` | — | Object |
| get_next_composite_character_column | `<id>.getNextCompositeCharacterColumn(column)` | int column | int |
| get_placeholder | `<id>.getPlaceholder()` | — | string |
| get_previous_composite_character_column | `<id>.getPreviousCompositeCharacterColumn(column)` | int column | int |
| get_scroll_offset | `<id>.getScrollOffset()` | — | float |
| get_selected_text | `<id>.getSelectedText()` | — | string |
| get_selection_from_column | `<id>.getSelectionFromColumn()` | — | int |
| get_selection_to_column | `<id>.getSelectionToColumn()` | — | int |
| has_ime_text | `<id>.hasImeText()` | — | bool |
| has_redo | `<id>.hasRedo()` | — | bool |
| has_selection | `<id>.hasSelection()` | — | bool |
| has_undo | `<id>.hasUndo()` | — | bool |
| insert_text_at_caret | `<id>.insertTextAtCaret(text)` | string text | void |
| is_caret_blink_enabled | `<id>.isCaretBlinkEnabled()` | — | bool |
| is_caret_mid_grapheme_enabled | `<id>.isCaretMidGraphemeEnabled()` | — | bool |
| is_editing | `<id>.isEditing()` | — | bool |
| is_editing_kept_on_text_submit | `<id>.isEditingKeptOnTextSubmit()` | — | bool |
| is_expand_to_text_length_enabled | `<id>.isExpandToTextLengthEnabled()` | — | bool |
| is_menu_visible | `<id>.isMenuVisible()` | — | bool |
| menu_option | `<id>.menuOption(option)` | int option | void |
| select | `<id>.select(from, to)` | int from, int to | void |
| select_all | `<id>.selectAll()` | — | void |
| set_caret_blink_enabled | `<id>.setCaretBlinkEnabled(enabled)` | bool enabled | void |
| set_caret_mid_grapheme_enabled | `<id>.setCaretMidGraphemeEnabled(enabled)` | bool enabled | void |
| set_expand_to_text_length_enabled | `<id>.setExpandToTextLengthEnabled(enabled)` | bool enabled | void |
| set_horizontal_alignment | `<id>.setHorizontalAlignment(alignment)` | int alignment | void |
| set_placeholder | `<id>.setPlaceholder(text)` | string text | void |
| unedit | `<id>.unedit()` | — | void |
