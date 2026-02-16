# Viewport3D

## Inheritance

[Viewport3D](Viewport3D.md) → [SubViewportContainer](SubViewportContainer.md) → [Container](Container.md) → [Control](Control.md) → [CanvasItem](CanvasItem.md) → [Node](Node.md) → [Object](Object.md)

## Properties

This page lists **only properties declared by `Viewport3D`**.
Inherited properties are documented in: [SubViewportContainer](SubViewportContainer.md)

| Godot Property | SML Property | Type | Default |
|-|-|-|-|
| id | id | identifier | — |
| model | model | string (url) | "" |
| modelSource | modelSource | string (url) | "" |
| animation | animation | string (url) | "" |
| animationSource | animationSource | string (url) | "" |
| playAnimation | playAnimation | int | 0 |
| playFirstAnimation | playFirstAnimation | bool | false |
| autoplayAnimation | autoplayAnimation | bool | false |
| defaultAnimation | defaultAnimation | string | "" |
| playLoop | playLoop | bool | false |
| cameraDistance | cameraDistance | int | 0 |
| lightEnergy | lightEnergy | int | 0 |

> Note: `model` and `modelSource` are aliases. Same for `animation` and `animationSource`.
> `id` is used to target camera/animation actions from SMS.

### Examples

```sml
Viewport3D {
    id: heroView
    model: "res:/assets/models/Idle.glb"
    playFirstAnimation: true
}
```

## Events

This page lists **only signals declared by `Viewport3D`**.
Inherited signals are documented in: [SubViewportContainer](SubViewportContainer.md)

| Godot Signal | SMS Event | Params |
|-|-|-|
