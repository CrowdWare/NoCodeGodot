# SML → Godot User Guide

This document explains how SML maps to Godot controls and how events work after the runtime simplification refactor.

The goal of the new system is simple:
SML directly describes Godot UI without translation layers.

---

## 1. Control Mapping

Every SML control name matches a Godot control class name.

Example:

```sml
Button {
    text: "Save"
}
```

Creates a Godot `Button` node.

There is no custom wrapper and no renaming layer.
SML acts as a serialization format for Godot scenes.

---

## 2. Layout System

SML uses the native Godot layout system only.

Supported layout concepts:
- Anchors
- SizeFlags
- Containers (HBoxContainer, VBoxContainer, TabContainer, etc.)

There are no layout modes like `app` or `document` anymore.

If a property exists in Godot, it can be used directly in SML using the same name and type.

### 2.1 Canonical layout properties and aliases

Canonical layout properties are:
- `size`
- `position`

Supported aliases:
- `width` / `height` (partial override of `size`)
- `x` / `y` / `left` / `top` / `pos` (partial override of `position`)

Conflict rule:
- **last write wins (source order)**.

Example:

```sml
PanelContainer {
    size: 300, 400
    width: 500
}
```

Result:
- width = 500
- height = 400

### 2.2 Runtime default rules (explicit policy)

Some layout defaults are intentionally applied by runtime policy when no explicit SML value exists.
These defaults are generated from specs (not hardcoded ad-hoc in runtime code):

- `autoFillMaxSizeTypes` (for controls that should expand by default)
- `menuBarDefaults` (position/anchors/min-height/z-order for app menu bars)

Source of truth:
- `tools/specs/layout_defaults.gd`
- generated to `ForgeRunner/Generated/SchemaLayoutDefaults.cs`

Precedence:
- explicit SML value always wins over runtime defaults
- aliases resolve by source order (last write wins), then defaults apply only for missing values

---

## 3. IDs

Controls may define an explicit id:

```sml
Button { id: save }
```

If no id is provided, the runtime generates an internal id automatically.

IDs affect how events are written in SMS.

---

## 4. Event System

Events belong to the Godot control that emits the signal.

Example:
- `TabContainer.tab_changed(int index)` belongs to `TabContainer`
- not to the individual tab pages

The runtime resolves events in two styles.

---

### 4.1 ID-based Events (preferred)

If a control has an id:

```sml
Button { id: save }
```

You can write:

```sms
on save.clicked() {
    log.info("Save clicked")
}
```

The id provides the context, so no parameters are needed.

---

### 4.2 Container Events (no id provided)

If elements do not have explicit ids, events are handled at container level.

```sms
on fileMenu.idPressed(id) {
    log.info(id)
}
```

Used for dynamic or list-like UI.

---

### 4.3 Item-Sugar Events

Some controls emit events for logical items that are not real nodes (e.g. PopupMenu items).

Container event:

```sms
on popup.idPressed(id) { }
```

If the SML item has an explicit id, a sugar syntax is available:

```sms
on open.clicked() { }
```

The engine internally resolves:

Godot signal → element id → SMS handler

---

## 5. Event Resolution Rules

1. If an element has an id → fire `<id>.<event>()`
2. If no id exists → fire container event with parameters
3. For collection controls → both styles are supported
4. Godot signal names are normalized and never exposed directly

---

## 6. Example

```sml
TabContainer { id: tabs
    TabPage { id: general }
    TabPage { id: advanced }
}
```

```sms
on tabs.tabChanged(index) {
    log.info(index)
}
```

Correct: `tabChanged` belongs to `TabContainer`, not `TabPage`.