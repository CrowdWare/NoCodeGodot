# ScrollContainer

## Inheritance

[ScrollContainer](ScrollContainer.md) → [Container](Container.md) → [Control](Control.md) → [CanvasItem](CanvasItem.md) → [Node](Node.md) → [Object](Object.md)

## Collection Items

This control appears to manage internal **items** (collection-style API).
Items are typically not represented as child nodes/properties in Godot.
In SML, this will be represented via **pseudo child elements** (documented per control).

## Derived Classes

### Direct subclasses

- [EditorInspector](EditorInspector.md)

## Properties

This page lists **only properties declared by `ScrollContainer`**.
Inherited properties are documented in: [Container](Container.md)

| Godot Property | SML Property | Type | Default |
|-|-|-|-|
| draw_focus_border | drawFocusBorder | bool | — |
| follow_focus | followFocus | bool | — |
| horizontal_scroll_mode | horizontalScrollMode | int | — |
| scroll_deadzone | scrollDeadzone | int | — |
| scroll_hint_mode | scrollHintMode | int | — |
| scroll_horizontal | scrollHorizontal | int | — |
| scroll_horizontal_custom_step | scrollHorizontalCustomStep | float | — |
| scroll_vertical | scrollVertical | int | — |
| scroll_vertical_custom_step | scrollVerticalCustomStep | float | — |
| tile_scroll_hint | tileScrollHint | bool | — |
| vertical_scroll_mode | verticalScrollMode | int | — |

## Events

This page lists **only signals declared by `ScrollContainer`**.
Inherited signals are documented in: [Container](Container.md)

| Godot Signal | SMS Event | Params |
|-|-|-|
| scroll_ended | `on <id>.scrollEnded() { ... }` | — |
| scroll_started | `on <id>.scrollStarted() { ... }` | — |

## SML Items (TODO)

This control appears to manage internal items, but a dedicated SML pseudo-child specification has not been defined yet.
Use the generated signals and the `collection: true` marker in `sms-reference.sml` as implementation hints.
