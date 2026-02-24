# Label

## Inheritance

[Label](Label.md) → [Control](Control.md) → [CanvasItem](CanvasItem.md) → [Node](Node.md) → [Object](Object.md)

## Properties

This page lists **only properties declared by `Label`**.
Inherited properties are documented in: [Control](Control.md)

| Godot Property | SML Property | Type | Default |
|-|-|-|-|
| autowrap_mode | autowrapMode | int | — |
| autowrap_trim_flags | autowrapTrimFlags | int | — |
| clip_text | clipText | bool | — |
| ellipsis_char | ellipsisChar | string | — |
| horizontal_alignment | horizontalAlignment | int | — |
| justification_flags | justificationFlags | int | — |
| language | language | string | — |
| lines_skipped | linesSkipped | int | — |
| max_lines_visible | maxLinesVisible | int | — |
| paragraph_separator | paragraphSeparator | string | — |
| structured_text_bidi_override | structuredTextBidiOverride | int | — |
| text | text | string | — |
| text_direction | textDirection | int | — |
| text_overrun_behavior | textOverrunBehavior | int | — |
| uppercase | uppercase | bool | — |
| vertical_alignment | verticalAlignment | int | — |
| visible_characters | visibleCharacters | int | — |
| visible_characters_behavior | visibleCharactersBehavior | int | — |
| visible_ratio | visibleRatio | float | — |

## Events

This page lists **only signals declared by `Label`**.
Inherited signals are documented in: [Control](Control.md)

| Godot Signal | SMS Event | Params |
|-|-|-|

## Runtime Actions

This page lists **callable methods declared by `Label`**.
Inherited actions are documented in: [Control](Control.md)

| Godot Method | SMS Call | Params | Returns |
|-|-|-|-|
| get_character_bounds | `<id>.getCharacterBounds(pos)` | int pos | Variant |
| get_line_count | `<id>.getLineCount()` | — | int |
| get_line_height | `<id>.getLineHeight(line)` | int line | int |
| get_total_character_count | `<id>.getTotalCharacterCount()` | — | int |
| get_visible_line_count | `<id>.getVisibleLineCount()` | — | int |
| is_clipping_text | `<id>.isClippingText()` | — | bool |

## Attached Properties

These properties are declared by a parent provider and set on this element using the qualified syntax `<providerId>.property: value` or `ProviderType.property: value`.

### Provided by `TabContainer`

| Attached Property | Type | Description |
|-|-|-|
| title | string | Tab title read by the parent TabContainer. Use attached property syntax: `<containerId>.title: "Caption"` or `TabContainer.title: "Caption"`. |

### Provided by `DockingContainer`

| Attached Property | Type | Description |
|-|-|-|
| title | string | Tab title read by the parent DockingContainer. Use attached property syntax: `<containerId>.title: "Caption"` or `DockingContainer.title: "Caption"`. |

