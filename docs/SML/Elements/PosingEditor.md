# PosingEditor

## Inheritance

[PosingEditor](PosingEditor.md) → [SubViewportContainer](SubViewportContainer.md) → [Container](Container.md) → [Control](Control.md) → [CanvasItem](CanvasItem.md) → [Node](Node.md) → [Object](Object.md)

## Properties

This page lists **only properties declared by `PosingEditor`**.
Inherited properties are documented in: [SubViewportContainer](SubViewportContainer.md)

| Godot Property | SML Property | Type | Default |
|-|-|-|-|
| — | id | identifier | — |
| — | src | string(url) | "" |
| — | showBoneTree | bool | false |
| — | normalizeNames | bool | true |

## Events

This page lists **only signals declared by `PosingEditor`**.
Inherited signals are documented in: [SubViewportContainer](SubViewportContainer.md)

| Godot Signal | SMS Event | Params |
|-|-|-|

## Actions

This page lists **only actions supported by the runtime** for `PosingEditor`.
Inherited actions are documented in: [SubViewportContainer](SubViewportContainer.md)

| Action | SMS Call | Params | Returns |
|-|-|-|-|
| resetPose | `<id>.resetPose()` | — | void |
| setJointSpheresVisible | `<id>.setJointSpheresVisible(visible)` | bool visible | void |
| setBoneTree | `<id>.setBoneTree(treeId)` | string treeId | void |
| setModelSource | `<id>.setModelSource(src)` | string src | void |
| loadProject | `<id>.loadProject(path)` | string path | void |
| saveProject | `<id>.saveProject(path)` | string path | void |
| addSceneProp | `<id>.addSceneProp(path, posX, posY, posZ)` | string path, float posX, float posY, float posZ | int |
| removeSceneProp | `<id>.removeSceneProp(index)` | int index | void |
| getScenePropCount | `<id>.getScenePropCount()` | — | int |
| getScenePropPath | `<id>.getScenePropPath(index)` | int index | string |
| getScenePropName | `<id>.getScenePropName(index)` | int index | string |
| getScenePropPos | `<id>.getScenePropPos(index)` | int index | string |
| setScenePropPos | `<id>.setScenePropPos(index, x, y, z)` | int index, float x, float y, float z | void |
| getScenePropRot | `<id>.getScenePropRot(index)` | int index | string |
| setScenePropRot | `<id>.setScenePropRot(index, x, y, z)` | int index, float x, float y, float z | void |
| setMode | `<id>.setMode(mode)` | string mode | void |
| setEditMode | `<id>.setEditMode(mode)` | string mode | void |
