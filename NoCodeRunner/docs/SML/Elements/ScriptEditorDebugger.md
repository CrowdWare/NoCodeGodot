# ScriptEditorDebugger

## Inheritance

[ScriptEditorDebugger](ScriptEditorDebugger.md) → [MarginContainer](MarginContainer.md) → [Container](Container.md) → [Control](Control.md) → [CanvasItem](CanvasItem.md) → [Node](Node.md) → [Object](Object.md)

## Collection Items

This control appears to manage internal **items** (collection-style API).
Items are typically not represented as child nodes/properties in Godot.
In SML, this will be represented via **pseudo child elements** (documented per control).

## Properties

This page lists **only properties declared by `ScriptEditorDebugger`**.
Inherited properties are documented in: [MarginContainer](MarginContainer.md)

| Godot Property | SML Property | Type | Default |
|-|-|-|-|

## Events

This page lists **only signals declared by `ScriptEditorDebugger`**.
Inherited signals are documented in: [MarginContainer](MarginContainer.md)

| Godot Signal | SMS Event | Params |
|-|-|-|
| breaked | `on <id>.breaked(reallydid, canDebug, reason, hasStackdump) { ... }` | bool reallydid, bool canDebug, string reason, bool hasStackdump |
| breakpoint_selected | `on <id>.breakpointSelected(, line) { ... }` | Object , int line |
| clear_breakpoints | `on <id>.clearBreakpoints() { ... }` | — |
| clear_execution | `on <id>.clearExecution() { ... }` | Object  |
| debug_data | `on <id>.debugData(msg, data) { ... }` | string msg, Variant data |
| embed_shortcut_requested | `on <id>.embedShortcutRequested(embedShortcutAction) { ... }` | int embedShortcutAction |
| error_selected | `on <id>.errorSelected(error) { ... }` | int error |
| errors_cleared | `on <id>.errorsCleared() { ... }` | — |
| output | `on <id>.output(msg, level) { ... }` | string msg, int level |
| remote_object_property_updated | `on <id>.remoteObjectPropertyUpdated(id, property) { ... }` | int id, string property |
| remote_objects_requested | `on <id>.remoteObjectsRequested(ids) { ... }` | Variant ids |
| remote_objects_updated | `on <id>.remoteObjectsUpdated(remoteObjects) { ... }` | Object remoteObjects |
| remote_tree_clear_selection_requested | `on <id>.remoteTreeClearSelectionRequested() { ... }` | — |
| remote_tree_select_requested | `on <id>.remoteTreeSelectRequested(ids) { ... }` | Variant ids |
| remote_tree_updated | `on <id>.remoteTreeUpdated() { ... }` | — |
| remote_window_title_changed | `on <id>.remoteWindowTitleChanged(title) { ... }` | string title |
| set_breakpoint | `on <id>.setBreakpoint(, line, enabled) { ... }` | Object , int line, bool enabled |
| set_execution | `on <id>.setExecution(, line) { ... }` | Object , int line |
| stack_dump | `on <id>.stackDump(stackDump) { ... }` | Variant stackDump |
| stack_frame_selected | `on <id>.stackFrameSelected(frame) { ... }` | int frame |
| stack_frame_var | `on <id>.stackFrameVar(data) { ... }` | Variant data |
| stack_frame_vars | `on <id>.stackFrameVars(numVars) { ... }` | int numVars |
| started | `on <id>.started() { ... }` | — |
| stop_requested | `on <id>.stopRequested() { ... }` | — |
| stopped | `on <id>.stopped() { ... }` | — |
