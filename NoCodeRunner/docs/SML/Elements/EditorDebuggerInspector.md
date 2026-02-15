# EditorDebuggerInspector

## Inheritance

[EditorDebuggerInspector](EditorDebuggerInspector.md) → [EditorInspector](EditorInspector.md) → [ScrollContainer](ScrollContainer.md) → [Container](Container.md) → [Control](Control.md) → [CanvasItem](CanvasItem.md) → [Node](Node.md) → [Object](Object.md)

## Collection Items

This control appears to manage internal **items** (collection-style API).
Items are typically not represented as child nodes/properties in Godot.
In SML, this will be represented via **pseudo child elements** (documented per control).

## Properties

This page lists **only properties declared by `EditorDebuggerInspector`**.
Inherited properties are documented in: [EditorInspector](EditorInspector.md)

| Godot Property | SML Property | Type | Default |
|-|-|-|-|

## Events

This page lists **only signals declared by `EditorDebuggerInspector`**.
Inherited signals are documented in: [EditorInspector](EditorInspector.md)

| Godot Signal | SMS Event | Params |
|-|-|-|
| object_property_updated | `on <id>.objectPropertyUpdated(id, property) { ... }` | int id, string property |
| object_selected | `on <id>.objectSelected(id) { ... }` | int id |
| objects_edited | `on <id>.objectsEdited(ids, property, , field) { ... }` | Variant ids, string property, Object , string field |

## SML Items (TODO)

This control appears to manage internal items, but a dedicated SML pseudo-child specification has not been defined yet.
Use the generated signals and the `collection: true` marker in `sms-reference.sml` as implementation hints.
