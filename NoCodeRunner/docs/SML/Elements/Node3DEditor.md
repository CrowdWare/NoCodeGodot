# Node3DEditor

## Inheritance

[Node3DEditor](Node3DEditor.md) → [VBoxContainer](VBoxContainer.md) → [BoxContainer](BoxContainer.md) → [Container](Container.md) → [Control](Control.md) → [CanvasItem](CanvasItem.md) → [Node](Node.md) → [Object](Object.md)

## Collection Items

This control appears to manage internal **items** (collection-style API).
Items are typically not represented as child nodes/properties in Godot.
In SML, this will be represented via **pseudo child elements** (documented per control).

## Properties

This page lists **only properties declared by `Node3DEditor`**.
Inherited properties are documented in: [VBoxContainer](VBoxContainer.md)

| Godot Property | SML Property | Type | Default |
|-|-|-|-|

## Events

This page lists **only signals declared by `Node3DEditor`**.
Inherited signals are documented in: [VBoxContainer](VBoxContainer.md)

| Godot Signal | SMS Event | Params |
|-|-|-|
| item_group_status_changed | `on <id>.itemGroupStatusChanged() { ... }` | — |
| item_lock_status_changed | `on <id>.itemLockStatusChanged() { ... }` | — |
| transform_key_request | `on <id>.transformKeyRequest() { ... }` | — |

## SML Items (TODO)

This control appears to manage internal items, but a dedicated SML pseudo-child specification has not been defined yet.
Use the generated signals and the `collection: true` marker in `sms-reference.sml` as implementation hints.
