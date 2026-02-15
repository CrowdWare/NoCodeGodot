# ScriptEditor

## Inheritance

[ScriptEditor](ScriptEditor.md) → [PanelContainer](PanelContainer.md) → [Container](Container.md) → [Control](Control.md) → [CanvasItem](CanvasItem.md) → [Node](Node.md) → [Object](Object.md)

## Collection Items

This control appears to manage internal **items** (collection-style API).
Items are typically not represented as child nodes/properties in Godot.
In SML, this will be represented via **pseudo child elements** (documented per control).

## Properties

This page lists **only properties declared by `ScriptEditor`**.
Inherited properties are documented in: [PanelContainer](PanelContainer.md)

| Godot Property | SML Property | Type | Default |
|-|-|-|-|

## Events

This page lists **only signals declared by `ScriptEditor`**.
Inherited signals are documented in: [PanelContainer](PanelContainer.md)

| Godot Signal | SMS Event | Params |
|-|-|-|
| editor_script_changed | `on <id>.editorScriptChanged(script) { ... }` | Object script |
| script_close | `on <id>.scriptClose(script) { ... }` | Object script |
