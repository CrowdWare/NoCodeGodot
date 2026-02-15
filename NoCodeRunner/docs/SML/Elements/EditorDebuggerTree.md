# EditorDebuggerTree

## Inheritance

[EditorDebuggerTree](EditorDebuggerTree.md) → [Tree](Tree.md) → [Control](Control.md) → [CanvasItem](CanvasItem.md) → [Node](Node.md) → [Object](Object.md)

## Collection Items

This control appears to manage internal **items** (collection-style API).
Items are typically not represented as child nodes/properties in Godot.
In SML, this will be represented via **pseudo child elements** (documented per control).

## Properties

This page lists **only properties declared by `EditorDebuggerTree`**.
Inherited properties are documented in: [Tree](Tree.md)

| Godot Property | SML Property | Type | Default |
|-|-|-|-|

## Events

This page lists **only signals declared by `EditorDebuggerTree`**.
Inherited signals are documented in: [Tree](Tree.md)

| Godot Signal | SMS Event | Params |
|-|-|-|
| objects_selected | `on <id>.objectsSelected(objectIds, debugger) { ... }` | Variant objectIds, int debugger |
| open | `on <id>.open() { ... }` | — |
| save_node | `on <id>.saveNode(objectId, filename, debugger) { ... }` | int objectId, string filename, int debugger |
| selection_cleared | `on <id>.selectionCleared(debugger) { ... }` | int debugger |
