# Node

## Inheritance

[Node](Node.md) → [Object](Object.md)

## Derived Classes

Classes listed below inherit from `Node`.

### Direct subclasses

- [AnimationMixer](AnimationMixer.md)
- [AudioStreamPlayer](AudioStreamPlayer.md)
- [AudioStreamPreviewGenerator](AudioStreamPreviewGenerator.md)
- [CanvasItem](CanvasItem.md)
- [CanvasLayer](CanvasLayer.md)
- [DockShortcutHandler](DockShortcutHandler.md)
- [EditorExport](EditorExport.md)
- [EditorFileSystem](EditorFileSystem.md)
- [EditorImportBlendRunner](EditorImportBlendRunner.md)
- [EditorNode](EditorNode.md)
- [EditorPlugin](EditorPlugin.md)
- [EditorPropertyNameProcessor](EditorPropertyNameProcessor.md)
- [EditorResourcePreview](EditorResourcePreview.md)
- [HTTPRequest](HTTPRequest.md)
- [InstancePlaceholder](InstancePlaceholder.md)
- [MissingNode](MissingNode.md)
- [MultiplayerSpawner](MultiplayerSpawner.md)
- [MultiplayerSynchronizer](MultiplayerSynchronizer.md)
- [NavigationAgent2D](NavigationAgent2D.md)
- [NavigationAgent3D](NavigationAgent3D.md)
- [Node3D](Node3D.md)
- [ResourcePreloader](ResourcePreloader.md)
- [ShaderGlobalsOverride](ShaderGlobalsOverride.md)
- [ShortcutBin](ShortcutBin.md)
- [StatusIndicator](StatusIndicator.md)
- [Timer](Timer.md)
- [Viewport](Viewport.md)
- [WorldEnvironment](WorldEnvironment.md)

## Properties

This page lists **only properties declared by `Node`**.
Inherited properties are documented in: [Object](Object.md)

| Godot Property | SML Property | Type | Default |
|-|-|-|-|
| auto_translate_mode | autoTranslateMode | int | — |
| editor_description | editorDescription | string | — |
| physics_interpolation_mode | physicsInterpolationMode | int | — |
| process_mode | processMode | int | — |
| process_physics_priority | processPhysicsPriority | int | — |
| process_priority | processPriority | int | — |
| process_thread_group | processThreadGroup | int | — |
| process_thread_group_order | processThreadGroupOrder | int | — |
| process_thread_messages | processThreadMessages | int | — |

## Events

This page lists **only signals declared by `Node`**.
Inherited signals are documented in: [Object](Object.md)

| Godot Signal | SMS Event | Params |
|-|-|-|
| child_entered_tree | `on <id>.childEnteredTree(node) { ... }` | Object node |
| child_exiting_tree | `on <id>.childExitingTree(node) { ... }` | Object node |
| child_order_changed | `on <id>.childOrderChanged() { ... }` | — |
| editor_description_changed | `on <id>.editorDescriptionChanged(node) { ... }` | Object node |
| editor_state_changed | `on <id>.editorStateChanged() { ... }` | — |
| ready | `on <id>.ready() { ... }` | — |
| renamed | `on <id>.renamed() { ... }` | — |
| replacing_by | `on <id>.replacingBy(node) { ... }` | Object node |
| tree_entered | `on <id>.treeEntered() { ... }` | — |
| tree_exited | `on <id>.treeExited() { ... }` | — |
| tree_exiting | `on <id>.treeExiting() { ... }` | — |
