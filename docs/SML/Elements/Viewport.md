# Viewport

## Inheritance

[Viewport](Viewport.md) → [Node](Node.md) → [Object](Object.md)

## Derived Classes

### Direct subclasses

- [SubViewport](SubViewport.md)
- [Window](Window.md)

## Properties

This page lists **only properties declared by `Viewport`**.
Inherited properties are documented in: [Node](Node.md)

| Godot Property | SML Property | Type | Default |
|-|-|-|-|
| anisotropic_filtering_level | anisotropicFilteringLevel | int | — |
| audio_listener_enable_2d | audioListenerEnable2d | bool | — |
| audio_listener_enable_3d | audioListenerEnable3d | bool | — |
| canvas_cull_mask | canvasCullMask | int | — |
| canvas_item_default_texture_filter | canvasItemDefaultTextureFilter | int | — |
| canvas_item_default_texture_repeat | canvasItemDefaultTextureRepeat | int | — |
| debug_draw | debugDraw | int | — |
| disable_3d | disable3d | bool | — |
| fsr_sharpness | fsrSharpness | float | — |
| gui_disable_input | guiDisableInput | bool | — |
| gui_drag_threshold | guiDragThreshold | int | — |
| gui_embed_subwindows | guiEmbedSubwindows | bool | — |
| gui_snap_controls_to_pixels | guiSnapControlsToPixels | bool | — |
| handle_input_locally | handleInputLocally | bool | — |
| mesh_lod_threshold | meshLodThreshold | float | — |
| msaa_2d | msaa2d | int | — |
| msaa_3d | msaa3d | int | — |
| oversampling | oversampling | bool | — |
| oversampling_override | oversamplingOverride | float | — |
| own_world_3d | ownWorld3d | bool | — |
| physics_object_picking | physicsObjectPicking | bool | — |
| physics_object_picking_first_only | physicsObjectPickingFirstOnly | bool | — |
| physics_object_picking_sort | physicsObjectPickingSort | bool | — |
| positional_shadow_atlas_16_bits | positionalShadowAtlas16Bits | bool | — |
| positional_shadow_atlas_quad_0 | positionalShadowAtlasQuad0 | int | — |
| positional_shadow_atlas_quad_1 | positionalShadowAtlasQuad1 | int | — |
| positional_shadow_atlas_quad_2 | positionalShadowAtlasQuad2 | int | — |
| positional_shadow_atlas_quad_3 | positionalShadowAtlasQuad3 | int | — |
| positional_shadow_atlas_size | positionalShadowAtlasSize | int | — |
| scaling_3d_mode | scaling3dMode | int | — |
| scaling_3d_scale | scaling3dScale | float | — |
| screen_space_aa | screenSpaceAa | int | — |
| sdf_oversize | sdfOversize | int | — |
| sdf_scale | sdfScale | int | — |
| snap_2d_transforms_to_pixel | snap2dTransformsToPixel | bool | — |
| snap_2d_vertices_to_pixel | snap2dVerticesToPixel | bool | — |
| texture_mipmap_bias | textureMipmapBias | float | — |
| transparent_bg | transparentBg | bool | — |
| use_debanding | useDebanding | bool | — |
| use_hdr_2d | useHdr2d | bool | — |
| use_occlusion_culling | useOcclusionCulling | bool | — |
| use_taa | useTaa | bool | — |
| use_xr | useXr | bool | — |
| vrs_mode | vrsMode | int | — |
| vrs_update_mode | vrsUpdateMode | int | — |

## Events

This page lists **only signals declared by `Viewport`**.
Inherited signals are documented in: [Node](Node.md)

| Godot Signal | SMS Event | Params |
|-|-|-|
| gui_focus_changed | `on <id>.guiFocusChanged(node) { ... }` | Object node |
| size_changed | `on <id>.sizeChanged() { ... }` | — |
