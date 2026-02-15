# FileSystemDock

## Inheritance

[FileSystemDock](FileSystemDock.md) → [EditorDock](EditorDock.md) → [MarginContainer](MarginContainer.md) → [Container](Container.md) → [Control](Control.md) → [CanvasItem](CanvasItem.md) → [Node](Node.md) → [Object](Object.md)

## Collection Items

This control appears to manage internal **items** (collection-style API).
Items are typically not represented as child nodes/properties in Godot.
In SML, this will be represented via **pseudo child elements** (documented per control).

## Properties

This page lists **only properties declared by `FileSystemDock`**.
Inherited properties are documented in: [EditorDock](EditorDock.md)

| Godot Property | SML Property | Type | Default |
|-|-|-|-|

## Events

This page lists **only signals declared by `FileSystemDock`**.
Inherited signals are documented in: [EditorDock](EditorDock.md)

| Godot Signal | SMS Event | Params |
|-|-|-|
| display_mode_changed | `on <id>.displayModeChanged() { ... }` | — |
| file_removed | `on <id>.fileRemoved(file) { ... }` | string file |
| files_moved | `on <id>.filesMoved(oldFile, newFile) { ... }` | string oldFile, string newFile |
| folder_color_changed | `on <id>.folderColorChanged() { ... }` | — |
| folder_moved | `on <id>.folderMoved(oldFolder, newFolder) { ... }` | string oldFolder, string newFolder |
| folder_removed | `on <id>.folderRemoved(folder) { ... }` | string folder |
| inherit | `on <id>.inherit(file) { ... }` | string file |
| instantiate | `on <id>.instantiate(files) { ... }` | Variant files |
| resource_removed | `on <id>.resourceRemoved(resource) { ... }` | Object resource |
| selection_changed | `on <id>.selectionChanged() { ... }` | — |

## SML Items (TODO)

This control appears to manage internal items, but a dedicated SML pseudo-child specification has not been defined yet.
Use the generated signals and the `collection: true` marker in `sms-reference.sml` as implementation hints.
