# DockingContainer

## Inheritance

[DockingContainer](DockingContainer.md) → [PanelContainer](PanelContainer.md) → [Container](Container.md) → [Control](Control.md) → [CanvasItem](CanvasItem.md) → [Node](Node.md) → [Object](Object.md)

## Properties

This page lists **only properties declared by `DockingContainer`**.
Inherited properties are documented in: [PanelContainer](PanelContainer.md)

| Godot Property | SML Property | Type | Default |
|-|-|-|-|
| — | id | identifier | — |
| — | dockSide | enum | center |
| — | fixedWidth | int | 240 |
| — | minFixedWidth | int | 140 |
| — | fixedHeight | int | — |
| — | minFixedHeight | int | 80 |
| — | heightPercent | float | 50 |
| — | flex | bool | false |
| — | closeable | bool | true |
| — | dragToRearrangeEnabled | bool | true |
| — | tabsRearrangeGroup | int | 1 |

> Automatically creates an internal TabContainer.
> Direct child controls become tabs; use context property 'title' on each child to define tab captions.
> dockSide supports: farLeft, farLeftBottom, left, leftBottom, center, right, rightBottom, farRight, farRightBottom.
> For split columns (top/bottom), height can be controlled via fixedHeight (px) or heightPercent (0..100).
> Priority: fixedHeight > heightPercent > automatic 50/50 fallback.
> Use enum syntax without quotes, e.g. dockSide: left.
> dragToRearrangeEnabled: false excludes this container from docking move targets (kebab menu).
> A container is not listed as move target for itself (same dock slot is filtered).

### Examples

```sml
DockingContainer {
    id: leftDock
    dockSide: left
    fixedWidth: 280
    dragToRearrangeEnabled: true
    tabsRearrangeGroup: 1
}

DockingContainer {
    id: leftBottomDock
    dockSide: leftBottom
    fixedWidth: 280
    dragToRearrangeEnabled: true
    tabsRearrangeGroup: 1
}
```

## Events

This page lists **only signals declared by `DockingContainer`**.
Inherited signals are documented in: [PanelContainer](PanelContainer.md)

| Godot Signal | SMS Event | Params |
|-|-|-|
