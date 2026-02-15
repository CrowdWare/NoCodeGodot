# EditorInspector

## Inheritance

[EditorInspector](EditorInspector.md) → [ScrollContainer](ScrollContainer.md) → [Container](Container.md) → [Control](Control.md) → [CanvasItem](CanvasItem.md) → [Node](Node.md) → [Object](Object.md)

## Collection Items

This control appears to manage internal **items** (collection-style API).
Items are typically not represented as child nodes/properties in Godot.
In SML, this will be represented via **pseudo child elements** (documented per control).

## Derived Classes

### Direct subclasses

- [EditorDebuggerInspector](EditorDebuggerInspector.md)

## Properties

This page lists **only properties declared by `EditorInspector`**.
Inherited properties are documented in: [ScrollContainer](ScrollContainer.md)

| Godot Property | SML Property | Type | Default |
|-|-|-|-|

## Events

This page lists **only signals declared by `EditorInspector`**.
Inherited signals are documented in: [ScrollContainer](ScrollContainer.md)

| Godot Signal | SMS Event | Params |
|-|-|-|
| edited_object_changed | `on <id>.editedObjectChanged() { ... }` | — |
| object_id_selected | `on <id>.objectIdSelected(id) { ... }` | int id |
| property_deleted | `on <id>.propertyDeleted(property) { ... }` | string property |
| property_edited | `on <id>.propertyEdited(property) { ... }` | string property |
| property_keyed | `on <id>.propertyKeyed(property, value, advance) { ... }` | string property, Variant value, bool advance |
| property_selected | `on <id>.propertySelected(property) { ... }` | string property |
| property_toggled | `on <id>.propertyToggled(property, checked) { ... }` | string property, bool checked |
| resource_selected | `on <id>.resourceSelected(resource, path) { ... }` | Object resource, string path |
| restart_requested | `on <id>.restartRequested() { ... }` | — |
