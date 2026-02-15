# VideoStreamPlayer

## Inheritance

[VideoStreamPlayer](VideoStreamPlayer.md) → [Control](Control.md) → [CanvasItem](CanvasItem.md) → [Node](Node.md) → [Object](Object.md)

## Collection Items

This control appears to manage internal **items** (collection-style API).
Items are typically not represented as child nodes/properties in Godot.
In SML, this will be represented via **pseudo child elements** (documented per control).

## Properties

This page lists **only properties declared by `VideoStreamPlayer`**.
Inherited properties are documented in: [Control](Control.md)

| Godot Property | SML Property | Type | Default |
|-|-|-|-|
| audio_track | audioTrack | int | — |
| autoplay | autoplay | bool | — |
| buffering_msec | bufferingMsec | int | — |
| expand | expand | bool | — |
| loop | loop | bool | — |
| paused | paused | bool | — |
| speed_scale | speedScale | float | — |
| volume_db | volumeDb | float | — |

## Events

This page lists **only signals declared by `VideoStreamPlayer`**.
Inherited signals are documented in: [Control](Control.md)

| Godot Signal | SMS Event | Params |
|-|-|-|
| finished | `on <id>.finished() { ... }` | — |
