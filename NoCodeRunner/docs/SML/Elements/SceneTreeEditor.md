# SceneTreeEditor

## Inheritance

[SceneTreeEditor](SceneTreeEditor.md) → [Control](Control.md) → [CanvasItem](CanvasItem.md) → [Node](Node.md) → [Object](Object.md)

## Collection Items

This control appears to manage internal **items** (collection-style API).
Items are typically not represented as child nodes/properties in Godot.
In SML, this will be represented via **pseudo child elements** (documented per control).

## Properties

This page lists **only properties declared by `SceneTreeEditor`**.
Inherited properties are documented in: [Control](Control.md)

| Godot Property | SML Property | Type | Default |
|-|-|-|-|

## Events

This page lists **only signals declared by `SceneTreeEditor`**.
Inherited signals are documented in: [Control](Control.md)

| Godot Signal | SMS Event | Params |
|-|-|-|
| files_dropped | `on <id>.filesDropped(files, toPath, type) { ... }` | Variant files, Variant toPath, int type |
| node_changed | `on <id>.nodeChanged() { ... }` | — |
| node_prerename | `on <id>.nodePrerename() { ... }` | — |
| node_renamed | `on <id>.nodeRenamed() { ... }` | — |
| node_selected | `on <id>.nodeSelected() { ... }` | — |
| nodes_dragged | `on <id>.nodesDragged() { ... }` | — |
| nodes_rearranged | `on <id>.nodesRearranged(paths, toPath, type) { ... }` | Variant paths, Variant toPath, int type |
| open | `on <id>.open() { ... }` | — |
| open_script | `on <id>.openScript() { ... }` | — |
| rmb_pressed | `on <id>.rmbPressed(position) { ... }` | Vector2 position |
| script_dropped | `on <id>.scriptDropped(file, toPath) { ... }` | string file, Variant toPath |

## SML Items (TODO)

This control appears to manage internal items, but a dedicated SML pseudo-child specification has not been defined yet.
Use the generated signals and the `collection: true` marker in `sms-reference.sml` as implementation hints.
