# ColorPicker

## Inheritance

[ColorPicker](ColorPicker.md) → [VBoxContainer](VBoxContainer.md) → [BoxContainer](BoxContainer.md) → [Container](Container.md) → [Control](Control.md) → [CanvasItem](CanvasItem.md) → [Node](Node.md) → [Object](Object.md)

## Collection Items

This control appears to manage internal **items** (collection-style API).
Items are typically not represented as child nodes/properties in Godot.
In SML, this will be represented via **pseudo child elements** (documented per control).

## Properties

This page lists **only properties declared by `ColorPicker`**.
Inherited properties are documented in: [VBoxContainer](VBoxContainer.md)

| Godot Property | SML Property | Type | Default |
|-|-|-|-|
| can_add_swatches | canAddSwatches | bool | — |
| color | color | Color | — |
| color_mode | colorMode | int | — |
| color_modes_visible | colorModesVisible | bool | — |
| deferred_mode | deferredMode | bool | — |
| edit_alpha | editAlpha | bool | — |
| edit_intensity | editIntensity | bool | — |
| hex_visible | hexVisible | bool | — |
| picker_shape | pickerShape | int | — |
| presets_visible | presetsVisible | bool | — |
| sampler_visible | samplerVisible | bool | — |
| sliders_visible | slidersVisible | bool | — |

## Events

This page lists **only signals declared by `ColorPicker`**.
Inherited signals are documented in: [VBoxContainer](VBoxContainer.md)

| Godot Signal | SMS Event | Params |
|-|-|-|
| color_changed | `on <id>.colorChanged(color) { ... }` | Color color |
| preset_added | `on <id>.presetAdded(color) { ... }` | Color color |
| preset_removed | `on <id>.presetRemoved(color) { ... }` | Color color |
