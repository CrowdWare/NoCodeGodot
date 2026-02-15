# EditorAudioBus

## Inheritance

[EditorAudioBus](EditorAudioBus.md) → [PanelContainer](PanelContainer.md) → [Container](Container.md) → [Control](Control.md) → [CanvasItem](CanvasItem.md) → [Node](Node.md) → [Object](Object.md)

## Collection Items

This control appears to manage internal **items** (collection-style API).
Items are typically not represented as child nodes/properties in Godot.
In SML, this will be represented via **pseudo child elements** (documented per control).

## Properties

This page lists **only properties declared by `EditorAudioBus`**.
Inherited properties are documented in: [PanelContainer](PanelContainer.md)

| Godot Property | SML Property | Type | Default |
|-|-|-|-|

## Events

This page lists **only signals declared by `EditorAudioBus`**.
Inherited signals are documented in: [PanelContainer](PanelContainer.md)

| Godot Signal | SMS Event | Params |
|-|-|-|
| delete_request | `on <id>.deleteRequest() { ... }` | — |
| drop_end_request | `on <id>.dropEndRequest() { ... }` | — |
| dropped | `on <id>.dropped() { ... }` | — |
| duplicate_request | `on <id>.duplicateRequest() { ... }` | — |
| vol_reset_request | `on <id>.volResetRequest() { ... }` | — |
