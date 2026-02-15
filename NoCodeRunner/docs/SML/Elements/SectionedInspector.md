# SectionedInspector

## Inheritance

[SectionedInspector](SectionedInspector.md) → [HSplitContainer](HSplitContainer.md) → [SplitContainer](SplitContainer.md) → [Container](Container.md) → [Control](Control.md) → [CanvasItem](CanvasItem.md) → [Node](Node.md) → [Object](Object.md)

## Collection Items

This control appears to manage internal **items** (collection-style API).
Items are typically not represented as child nodes/properties in Godot.
In SML, this will be represented via **pseudo child elements** (documented per control).

## Properties

This page lists **only properties declared by `SectionedInspector`**.
Inherited properties are documented in: [HSplitContainer](HSplitContainer.md)

| Godot Property | SML Property | Type | Default |
|-|-|-|-|

## Events

This page lists **only signals declared by `SectionedInspector`**.
Inherited signals are documented in: [HSplitContainer](HSplitContainer.md)

| Godot Signal | SMS Event | Params |
|-|-|-|
| category_changed | `on <id>.categoryChanged(newCategory) { ... }` | string newCategory |

## SML Items (TODO)

This control appears to manage internal items, but a dedicated SML pseudo-child specification has not been defined yet.
Use the generated signals and the `collection: true` marker in `sms-reference.sml` as implementation hints.
