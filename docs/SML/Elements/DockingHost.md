# DockingHost

## Inheritance

[DockingHost](DockingHost.md) → [Container](Container.md) → [Control](Control.md) → [CanvasItem](CanvasItem.md) → [Node](Node.md) → [Object](Object.md)

## Properties

This page lists **only properties declared by `DockingHost`**.
Inherited properties are documented in: [Container](Container.md)

| Godot Property | SML Property | Type | Default |
|-|-|-|-|
| — | id | identifier | — |
| — | gap | int | 0 |
| — | anchors | string | "" |
| — | x | int | 0 |
| — | y | int | 0 |
| — | width | int | 0 |
| — | height | int | 0 |

> Layouts multiple DockingContainer children using dockSide/fixedWidth/flex semantics.
> Typically used as a full-rect region inside a Window.

### Examples

```sml
DockingHost {
    id: mainDockHost
    anchors: left | top | right | bottom
    gap: 8
}
```

## Events

This page lists **only signals declared by `DockingHost`**.
Inherited signals are documented in: [Container](Container.md)

| Godot Signal | SMS Event | Params |
|-|-|-|
