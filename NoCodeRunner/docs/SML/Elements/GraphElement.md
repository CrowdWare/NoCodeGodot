# GraphElement

## Inheritance

[GraphElement](GraphElement.md) → [Container](Container.md) → [Control](Control.md) → [CanvasItem](CanvasItem.md) → [Node](Node.md) → [Object](Object.md)

## Collection Items

This control appears to manage internal **items** (collection-style API).
Items are typically not represented as child nodes/properties in Godot.
In SML, this will be represented via **pseudo child elements** (documented per control).

## Derived Classes

### Direct subclasses

- [GraphFrame](GraphFrame.md)
- [GraphNode](GraphNode.md)

## Properties

This page lists **only properties declared by `GraphElement`**.
Inherited properties are documented in: [Container](Container.md)

| Godot Property | SML Property | Type | Default |
|-|-|-|-|
| draggable | draggable | bool | — |
| position_offset | positionOffset | Vector2 | — |
| resizable | resizable | bool | — |
| scaling_menus | scalingMenus | bool | — |
| selectable | selectable | bool | — |
| selected | selected | bool | — |

## Events

This page lists **only signals declared by `GraphElement`**.
Inherited signals are documented in: [Container](Container.md)

| Godot Signal | SMS Event | Params |
|-|-|-|
| delete_request | `on <id>.deleteRequest() { ... }` | — |
| dragged | `on <id>.dragged(from, to) { ... }` | Vector2 from, Vector2 to |
| node_deselected | `on <id>.nodeDeselected() { ... }` | — |
| node_selected | `on <id>.nodeSelected() { ... }` | — |
| position_offset_changed | `on <id>.positionOffsetChanged() { ... }` | — |
| raise_request | `on <id>.raiseRequest() { ... }` | — |
| resize_end | `on <id>.resizeEnd(newSize) { ... }` | Vector2 newSize |
| resize_request | `on <id>.resizeRequest(newSize) { ... }` | Vector2 newSize |

## SML Items (TODO)

This control appears to manage internal items, but a dedicated SML pseudo-child specification has not been defined yet.
Use the generated signals and the `collection: true` marker in `sms-reference.sml` as implementation hints.
