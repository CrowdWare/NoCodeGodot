# RichTextLabel

## Inheritance

[RichTextLabel](RichTextLabel.md) → [Control](Control.md) → [CanvasItem](CanvasItem.md) → [Node](Node.md) → [Object](Object.md)

## Collection Items

This control appears to manage internal **items** (collection-style API).
Items are typically not represented as child nodes/properties in Godot.
In SML, this will be represented via **pseudo child elements** (documented per control).

## Properties

This page lists **only properties declared by `RichTextLabel`**.
Inherited properties are documented in: [Control](Control.md)

| Godot Property | SML Property | Type | Default |
|-|-|-|-|
| autowrap_mode | autowrapMode | int | — |
| autowrap_trim_flags | autowrapTrimFlags | int | — |
| bbcode_enabled | bbcodeEnabled | bool | — |
| context_menu_enabled | contextMenuEnabled | bool | — |
| deselect_on_focus_loss_enabled | deselectOnFocusLossEnabled | bool | — |
| drag_and_drop_selection_enabled | dragAndDropSelectionEnabled | bool | — |
| fit_content | fitContent | bool | — |
| hint_underlined | hintUnderlined | bool | — |
| horizontal_alignment | horizontalAlignment | int | — |
| justification_flags | justificationFlags | int | — |
| language | language | string | — |
| meta_underlined | metaUnderlined | bool | — |
| progress_bar_delay | progressBarDelay | int | — |
| scroll_active | scrollActive | bool | — |
| scroll_following | scrollFollowing | bool | — |
| scroll_following_visible_characters | scrollFollowingVisibleCharacters | bool | — |
| selection_enabled | selectionEnabled | bool | — |
| shortcut_keys_enabled | shortcutKeysEnabled | bool | — |
| structured_text_bidi_override | structuredTextBidiOverride | int | — |
| tab_size | tabSize | int | — |
| text | text | string | — |
| text_direction | textDirection | int | — |
| threaded | threaded | bool | — |
| vertical_alignment | verticalAlignment | int | — |
| visible_characters | visibleCharacters | int | — |
| visible_characters_behavior | visibleCharactersBehavior | int | — |
| visible_ratio | visibleRatio | float | — |

## Events

This page lists **only signals declared by `RichTextLabel`**.
Inherited signals are documented in: [Control](Control.md)

| Godot Signal | SMS Event | Params |
|-|-|-|
| finished | `on <id>.finished() { ... }` | — |
| meta_clicked | `on <id>.metaClicked(meta) { ... }` | Variant meta |
| meta_hover_ended | `on <id>.metaHoverEnded(meta) { ... }` | Variant meta |
| meta_hover_started | `on <id>.metaHoverStarted(meta) { ... }` | Variant meta |
