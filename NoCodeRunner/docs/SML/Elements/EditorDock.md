# EditorDock

## Inheritance

[EditorDock](EditorDock.md) → [MarginContainer](MarginContainer.md) → [Container](Container.md) → [Control](Control.md) → [CanvasItem](CanvasItem.md) → [Node](Node.md) → [Object](Object.md)

## Collection Items

This control appears to manage internal **items** (collection-style API).
Items are typically not represented as child nodes/properties in Godot.
In SML, this will be represented via **pseudo child elements** (documented per control).

## Derived Classes

### Direct subclasses

- [AnimationPlayerEditor](AnimationPlayerEditor.md)
- [AnimationTreeEditor](AnimationTreeEditor.md)
- [EditorAudioBuses](EditorAudioBuses.md)
- [EditorDebuggerNode](EditorDebuggerNode.md)
- [EditorLog](EditorLog.md)
- [FileSystemDock](FileSystemDock.md)
- [FindInFilesContainer](FindInFilesContainer.md)
- [GroupsDock](GroupsDock.md)
- [HistoryDock](HistoryDock.md)
- [ImportDock](ImportDock.md)
- [InspectorDock](InspectorDock.md)
- [ResourcePreloaderEditor](ResourcePreloaderEditor.md)
- [SceneTreeDock](SceneTreeDock.md)
- [ShaderFileEditor](ShaderFileEditor.md)
- [SignalsDock](SignalsDock.md)
- [SpriteFramesEditor](SpriteFramesEditor.md)
- [ThemeEditor](ThemeEditor.md)
- [TileMapLayerEditor](TileMapLayerEditor.md)
- [TileSetEditor](TileSetEditor.md)

## Properties

This page lists **only properties declared by `EditorDock`**.
Inherited properties are documented in: [MarginContainer](MarginContainer.md)

| Godot Property | SML Property | Type | Default |
|-|-|-|-|
| available_layouts | availableLayouts | int | — |
| closable | closable | bool | — |
| default_slot | defaultSlot | int | — |
| force_show_icon | forceShowIcon | bool | — |
| global | global | bool | — |
| layout_key | layoutKey | string | — |
| title | title | string | — |
| title_color | titleColor | Color | — |
| transient | transient | bool | — |

## Events

This page lists **only signals declared by `EditorDock`**.
Inherited signals are documented in: [MarginContainer](MarginContainer.md)

| Godot Signal | SMS Event | Params |
|-|-|-|
| _tab_style_changed | `on <id>.TabStyleChanged() { ... }` | — |
| closed | `on <id>.closed() { ... }` | — |

## SML Items (TODO)

This control appears to manage internal items, but a dedicated SML pseudo-child specification has not been defined yet.
Use the generated signals and the `collection: true` marker in `sms-reference.sml` as implementation hints.
