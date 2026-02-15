# EditorResourcePicker

## Inheritance

[EditorResourcePicker](EditorResourcePicker.md) → [HBoxContainer](HBoxContainer.md) → [BoxContainer](BoxContainer.md) → [Container](Container.md) → [Control](Control.md) → [CanvasItem](CanvasItem.md) → [Node](Node.md) → [Object](Object.md)

## Collection Items

This control appears to manage internal **items** (collection-style API).
Items are typically not represented as child nodes/properties in Godot.
In SML, this will be represented via **pseudo child elements** (documented per control).

## Derived Classes

### Direct subclasses

- [EditorScriptPicker](EditorScriptPicker.md)

## Properties

This page lists **only properties declared by `EditorResourcePicker`**.
Inherited properties are documented in: [HBoxContainer](HBoxContainer.md)

| Godot Property | SML Property | Type | Default |
|-|-|-|-|
| base_type | baseType | string | — |
| editable | editable | bool | — |
| toggle_mode | toggleMode | bool | — |

## Events

This page lists **only signals declared by `EditorResourcePicker`**.
Inherited signals are documented in: [HBoxContainer](HBoxContainer.md)

| Godot Signal | SMS Event | Params |
|-|-|-|
| resource_changed | `on <id>.resourceChanged(resource) { ... }` | Object resource |
| resource_selected | `on <id>.resourceSelected(resource, inspect) { ... }` | Object resource, bool inspect |

## SML Items (TODO)

This control appears to manage internal items, but a dedicated SML pseudo-child specification has not been defined yet.
Use the generated signals and the `collection: true` marker in `sms-reference.sml` as implementation hints.
