# Container

## Inheritance

[Container](Container.md) → [Control](Control.md) → [CanvasItem](CanvasItem.md) → [Node](Node.md) → [Object](Object.md)

## Collection Items

This control appears to manage internal **items** (collection-style API).
Items are typically not represented as child nodes/properties in Godot.
In SML, this will be represented via **pseudo child elements** (documented per control).

## Derived Classes

### Direct subclasses

- [AspectRatioContainer](AspectRatioContainer.md)
- [BoxContainer](BoxContainer.md)
- [CenterContainer](CenterContainer.md)
- [EditorInspectorSection](EditorInspectorSection.md)
- [EditorProperty](EditorProperty.md)
- [FlowContainer](FlowContainer.md)
- [FoldableContainer](FoldableContainer.md)
- [GraphElement](GraphElement.md)
- [GridContainer](GridContainer.md)
- [MarginContainer](MarginContainer.md)
- [Node3DEditorViewportContainer](Node3DEditorViewportContainer.md)
- [PanelContainer](PanelContainer.md)
- [ScrollContainer](ScrollContainer.md)
- [SplitContainer](SplitContainer.md)
- [SubViewportContainer](SubViewportContainer.md)
- [TabContainer](TabContainer.md)

## Properties

This page lists **only properties declared by `Container`**.
Inherited properties are documented in: [Control](Control.md)

| Godot Property | SML Property | Type | Default |
|-|-|-|-|

## Events

This page lists **only signals declared by `Container`**.
Inherited signals are documented in: [Control](Control.md)

| Godot Signal | SMS Event | Params |
|-|-|-|
| pre_sort_children | `on <id>.preSortChildren() { ... }` | — |
| sort_children | `on <id>.sortChildren() { ... }` | — |

## SML Items (TODO)

This control appears to manage internal items, but a dedicated SML pseudo-child specification has not been defined yet.
Use the generated signals and the `collection: true` marker in `sms-reference.sml` as implementation hints.
