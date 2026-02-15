# EditorDebuggerNode

## Inheritance

[EditorDebuggerNode](EditorDebuggerNode.md) → [EditorDock](EditorDock.md) → [MarginContainer](MarginContainer.md) → [Container](Container.md) → [Control](Control.md) → [CanvasItem](CanvasItem.md) → [Node](Node.md) → [Object](Object.md)

## Collection Items

This control appears to manage internal **items** (collection-style API).
Items are typically not represented as child nodes/properties in Godot.
In SML, this will be represented via **pseudo child elements** (documented per control).

## Properties

This page lists **only properties declared by `EditorDebuggerNode`**.
Inherited properties are documented in: [EditorDock](EditorDock.md)

| Godot Property | SML Property | Type | Default |
|-|-|-|-|

## Events

This page lists **only signals declared by `EditorDebuggerNode`**.
Inherited signals are documented in: [EditorDock](EditorDock.md)

| Godot Signal | SMS Event | Params |
|-|-|-|
| breaked | `on <id>.breaked(reallydid, canDebug) { ... }` | bool reallydid, bool canDebug |
| breakpoint_set_in_tree | `on <id>.breakpointSetInTree(, line, enabled, debugger) { ... }` | Object , int line, bool enabled, int debugger |
| breakpoint_toggled | `on <id>.breakpointToggled(path, line, enabled) { ... }` | string path, int line, bool enabled |
| breakpoints_cleared_in_tree | `on <id>.breakpointsClearedInTree(debugger) { ... }` | int debugger |
| clear_execution | `on <id>.clearExecution() { ... }` | Object  |
| goto_script_line | `on <id>.gotoScriptLine() { ... }` | — |
| set_execution | `on <id>.setExecution(, line) { ... }` | Object , int line |

## SML Items (TODO)

This control appears to manage internal items, but a dedicated SML pseudo-child specification has not been defined yet.
Use the generated signals and the `collection: true` marker in `sms-reference.sml` as implementation hints.
