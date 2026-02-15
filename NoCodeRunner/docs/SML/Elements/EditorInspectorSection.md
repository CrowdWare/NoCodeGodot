# EditorInspectorSection

## Inheritance

[EditorInspectorSection](EditorInspectorSection.md) → [Container](Container.md) → [Control](Control.md) → [CanvasItem](CanvasItem.md) → [Node](Node.md) → [Object](Object.md)

## Collection Items

This control appears to manage internal **items** (collection-style API).
Items are typically not represented as child nodes/properties in Godot.
In SML, this will be represented via **pseudo child elements** (documented per control).

## Derived Classes

### Direct subclasses

- [EditorInspectorArray](EditorInspectorArray.md)

## Properties

This page lists **only properties declared by `EditorInspectorSection`**.
Inherited properties are documented in: [Container](Container.md)

| Godot Property | SML Property | Type | Default |
|-|-|-|-|

## Events

This page lists **only signals declared by `EditorInspectorSection`**.
Inherited signals are documented in: [Container](Container.md)

| Godot Signal | SMS Event | Params |
|-|-|-|
| property_keyed | `on <id>.propertyKeyed(property) { ... }` | Variant property |
| section_toggled_by_user | `on <id>.sectionToggledByUser(property, value) { ... }` | Variant property, bool value |
