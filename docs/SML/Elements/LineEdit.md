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
