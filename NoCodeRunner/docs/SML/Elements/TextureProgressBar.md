# TextureProgressBar

## Inheritance

[TextureProgressBar](TextureProgressBar.md) → [Range](Range.md) → [Control](Control.md) → [CanvasItem](CanvasItem.md) → [Node](Node.md) → [Object](Object.md)

## Collection Items

This control appears to manage internal **items** (collection-style API).
Items are typically not represented as child nodes/properties in Godot.
In SML, this will be represented via **pseudo child elements** (documented per control).

## Properties

This page lists **only properties declared by `TextureProgressBar`**.
Inherited properties are documented in: [Range](Range.md)

| Godot Property | SML Property | Type | Default |
|-|-|-|-|
| fill_mode | fillMode | int | — |
| nine_patch_stretch | ninePatchStretch | bool | — |
| radial_center_offset | radialCenterOffset | Vector2 | — |
| radial_fill_degrees | radialFillDegrees | float | — |
| radial_initial_angle | radialInitialAngle | float | — |
| stretch_margin_bottom | stretchMarginBottom | int | — |
| stretch_margin_left | stretchMarginLeft | int | — |
| stretch_margin_right | stretchMarginRight | int | — |
| stretch_margin_top | stretchMarginTop | int | — |
| texture_progress_offset | textureProgressOffset | Vector2 | — |
| tint_over | tintOver | Color | — |
| tint_progress | tintProgress | Color | — |
| tint_under | tintUnder | Color | — |

## Events

This page lists **only signals declared by `TextureProgressBar`**.
Inherited signals are documented in: [Range](Range.md)

| Godot Signal | SMS Event | Params |
|-|-|-|

## SML Items (TODO)

This control appears to manage internal items, but a dedicated SML pseudo-child specification has not been defined yet.
Use the generated signals and the `collection: true` marker in `sms-reference.sml` as implementation hints.
