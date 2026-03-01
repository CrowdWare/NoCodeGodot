# Timeline

## Inheritance

[Timeline](Timeline.md) → [Control](Control.md) → [CanvasItem](CanvasItem.md) → [Node](Node.md) → [Object](Object.md)

## Properties

This page lists **only properties declared by `Timeline`**.
Inherited properties are documented in: [Control](Control.md)

| Godot Property | SML Property | Type | Default |
|-|-|-|-|
| — | id | identifier | — |
| — | fps | int | 24 |
| — | totalFrames | int | 120 |

## Events

This page lists **only signals declared by `Timeline`**.
Inherited signals are documented in: [Control](Control.md)

| Godot Signal | SMS Event | Params |
|-|-|-|

## Actions

This page lists **only actions supported by the runtime** for `Timeline`.
Inherited actions are documented in: [Control](Control.md)

| Action | SMS Call | Params | Returns |
|-|-|-|-|
| play | `<id>.play()` | — | void |
| stop | `<id>.stop()` | — | void |
| setKeyframe | `<id>.setKeyframe(frame, poseData)` | int frame, object poseData | void |
| removeKeyframe | `<id>.removeKeyframe(frame)` | int frame | void |
| getPoseAt | `<id>.getPoseAt(frame)` | int frame | object |
| getKeyframeCount | `<id>.getKeyframeCount()` | — | int |
| getKeyframeFrameAt | `<id>.getKeyframeFrameAt(index)` | int index | int |
| getKeyframeBoneCount | `<id>.getKeyframeBoneCount(frame)` | int frame | int |
| getKeyframeBoneName | `<id>.getKeyframeBoneName(frame, boneIndex)` | int frame, int boneIndex | string |
| clearAllKeyframes | `<id>.clearAllKeyframes()` | — | void |
