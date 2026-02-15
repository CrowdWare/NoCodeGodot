# EditorSceneTabs

## Inheritance

[EditorSceneTabs](EditorSceneTabs.md) → [MarginContainer](MarginContainer.md) → [Container](Container.md) → [Control](Control.md) → [CanvasItem](CanvasItem.md) → [Node](Node.md) → [Object](Object.md)

## Collection Items

This control appears to manage internal **items** (collection-style API).
Items are typically not represented as child nodes/properties in Godot.
In SML, this will be represented via **pseudo child elements** (documented per control).

## Properties

This page lists **only properties declared by `EditorSceneTabs`**.
Inherited properties are documented in: [MarginContainer](MarginContainer.md)

| Godot Property | SML Property | Type | Default |
|-|-|-|-|

## Events

This page lists **only signals declared by `EditorSceneTabs`**.
Inherited signals are documented in: [MarginContainer](MarginContainer.md)

| Godot Signal | SMS Event | Params |
|-|-|-|
| tab_changed | `on <id>.tabChanged(tabIndex) { ... }` | int tabIndex |
| tab_closed | `on <id>.tabClosed(tabIndex) { ... }` | int tabIndex |

## SML Items (TODO)

This control appears to manage internal items, but a dedicated SML pseudo-child specification has not been defined yet.
Use the generated signals and the `collection: true` marker in `sms-reference.sml` as implementation hints.
