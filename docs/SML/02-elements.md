

# SML Nodes (Controls)

SML supports **all Godot UI nodes that inherit from `Control`** (the “green nodes” in the Godot Create Node dialog) **plus** `Window` and common dialog windows for completeness.

## Key Rule

- **SML node name = Godot class name**
- If a class exists in Godot and inherits `Control`, it can be instantiated from SML.
- Additionally, `Window`-based UI (including dialogs) is supported even though it is not a `Control`.

Example:

```sml
Button {
    text: "OK"
}
```

Creates a Godot `Button`.

## What is documented where

Because the supported set is **the full `Control` class family**, the detailed reference is generated from the runtime’s actual class database after the refactor.

- This file provides the **overview + grouping**.
- The complete, per-control list of **properties + events (including inherited)** is provided in:
  - `docs/sms-reference.sml` (authoritative reference used for IntelliSense)

## Groups

### Base UI

- Control
- CanvasItem (inherited base, not instantiable as a “green node”)

### Common Controls

- Button
- Label
- TextureRect
- ColorRect
- HSlider / VSlider
- ProgressBar
- CheckBox
- CheckButton
- OptionButton
- SpinBox
- LineEdit
- TextEdit
- RichTextLabel

### Containers

Containers are also `Control` nodes and therefore supported.

- Container
- HBoxContainer
- VBoxContainer
- GridContainer
- CenterContainer
- MarginContainer
- PanelContainer
- ScrollContainer
- SplitContainer (and HSplitContainer / VSplitContainer where applicable)
- TabContainer

### Viewport / Embedding

- SubViewportContainer

### Windows

For completeness, SML also supports window-based UI.

- Window

### Menus and Popups

- Popup
- PopupPanel
- PopupMenu
- MenuButton

> **Item-like structures:** Some controls (notably `PopupMenu`) contain logical items that are not separate nodes.
> Their “item events” are documented under the emitting control in `docs/sms-reference.sml`.

### Lists / Trees

- ItemList
- Tree

### Ranges

- Range
- ScrollBar
- ScrollContainer (also listed under Containers)

### Union Rule for Properties

Several properties support both a vector form and scalar form (similar to C unions):

```sml
Window {
    size: 1024, 768
}
```

is equivalent to:

```sml
Window {
    width: 1024
    height: 768
}
```

and:

```sml
Window {
    pos: 448, 156
}
```

is equivalent to:

```sml
Window {
    x: 448
    y: 156
}
```

This rule applies globally to all SML controls wherever vector pairs are used.

## Notes

- If a node is a `Control` in Godot, it is supported as an SML node (plus `Window` and dialog windows).
- The **authoritative** list of what is actually supported (after refactoring) is the reference file generated from the runtime.

Next: generate `docs/sms-reference.sml` from the runtime-supported class set (Controls, inherited properties, inherited events, and event parameter lists).