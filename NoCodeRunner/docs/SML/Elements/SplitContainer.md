# SplitContainer

## Inheritance

[SplitContainer](SplitContainer.md) → [Container](Container.md) → [Control](Control.md) → [CanvasItem](CanvasItem.md) → [Node](Node.md) → [Object](Object.md)

## Collection Items

This control appears to manage internal **items** (collection-style API).
Items are typically not represented as child nodes/properties in Godot.
In SML, this will be represented via **pseudo child elements** (documented per control).

## Derived Classes

### Direct subclasses

- [DockSplitContainer](DockSplitContainer.md)
- [HSplitContainer](HSplitContainer.md)
- [VSplitContainer](VSplitContainer.md)

## Properties

This page lists **only properties declared by `SplitContainer`**.
Inherited properties are documented in: [Container](Container.md)

| Godot Property | SML Property | Type | Default |
|-|-|-|-|
| collapsed | collapsed | bool | — |
| drag_area_highlight_in_editor | dragAreaHighlightInEditor | bool | — |
| drag_area_margin_begin | dragAreaMarginBegin | int | — |
| drag_area_margin_end | dragAreaMarginEnd | int | — |
| drag_area_offset | dragAreaOffset | int | — |
| dragger_visibility | draggerVisibility | int | — |
| dragging_enabled | draggingEnabled | bool | — |
| touch_dragger_enabled | touchDraggerEnabled | bool | — |
| vertical | vertical | bool | — |

## Events

This page lists **only signals declared by `SplitContainer`**.
Inherited signals are documented in: [Container](Container.md)

| Godot Signal | SMS Event | Params |
|-|-|-|
| drag_ended | `on <id>.dragEnded() { ... }` | — |
| drag_started | `on <id>.dragStarted() { ... }` | — |
| dragged | `on <id>.dragged(offset) { ... }` | int offset |

## SML Items (TODO)

This control appears to manage internal items, but a dedicated SML pseudo-child specification has not been defined yet.
Use the generated signals and the `collection: true` marker in `sms-reference.sml` as implementation hints.
