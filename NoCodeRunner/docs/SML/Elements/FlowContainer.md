# FlowContainer

## Inheritance

[FlowContainer](FlowContainer.md) → [Container](Container.md) → [Control](Control.md) → [CanvasItem](CanvasItem.md) → [Node](Node.md) → [Object](Object.md)

## Collection Items

This control appears to manage internal **items** (collection-style API).
Items are typically not represented as child nodes/properties in Godot.
In SML, this will be represented via **pseudo child elements** (documented per control).

## Derived Classes

### Direct subclasses

- [HFlowContainer](HFlowContainer.md)
- [VFlowContainer](VFlowContainer.md)

## Properties

This page lists **only properties declared by `FlowContainer`**.
Inherited properties are documented in: [Container](Container.md)

| Godot Property | SML Property | Type | Default |
|-|-|-|-|
| alignment | alignment | int | — |
| last_wrap_alignment | lastWrapAlignment | int | — |
| reverse_fill | reverseFill | bool | — |
| vertical | vertical | bool | — |

## Events

This page lists **only signals declared by `FlowContainer`**.
Inherited signals are documented in: [Container](Container.md)

| Godot Signal | SMS Event | Params |
|-|-|-|

## SML Items (TODO)

This control appears to manage internal items, but a dedicated SML pseudo-child specification has not been defined yet.
Use the generated signals and the `collection: true` marker in `sms-reference.sml` as implementation hints.
