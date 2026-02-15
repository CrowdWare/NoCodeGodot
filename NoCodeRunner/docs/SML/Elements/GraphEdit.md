# GraphEdit

## Inheritance

[GraphEdit](GraphEdit.md) → [Control](Control.md) → [CanvasItem](CanvasItem.md) → [Node](Node.md) → [Object](Object.md)

## Collection Items

This control appears to manage internal **items** (collection-style API).
Items are typically not represented as child nodes/properties in Godot.
In SML, this will be represented via **pseudo child elements** (documented per control).

## Properties

This page lists **only properties declared by `GraphEdit`**.
Inherited properties are documented in: [Control](Control.md)

| Godot Property | SML Property | Type | Default |
|-|-|-|-|
| connection_lines_antialiased | connectionLinesAntialiased | bool | — |
| connection_lines_curvature | connectionLinesCurvature | float | — |
| connection_lines_thickness | connectionLinesThickness | float | — |
| grid_pattern | gridPattern | int | — |
| minimap_enabled | minimapEnabled | bool | — |
| minimap_opacity | minimapOpacity | float | — |
| minimap_size | minimapSize | Vector2 | — |
| panning_scheme | panningScheme | int | — |
| right_disconnects | rightDisconnects | bool | — |
| scroll_offset | scrollOffset | Vector2 | — |
| show_arrange_button | showArrangeButton | bool | — |
| show_grid | showGrid | bool | — |
| show_grid_buttons | showGridButtons | bool | — |
| show_menu | showMenu | bool | — |
| show_minimap_button | showMinimapButton | bool | — |
| show_zoom_buttons | showZoomButtons | bool | — |
| show_zoom_label | showZoomLabel | bool | — |
| snapping_distance | snappingDistance | int | — |
| snapping_enabled | snappingEnabled | bool | — |
| zoom | zoom | float | — |
| zoom_max | zoomMax | float | — |
| zoom_min | zoomMin | float | — |
| zoom_step | zoomStep | float | — |

## Events

This page lists **only signals declared by `GraphEdit`**.
Inherited signals are documented in: [Control](Control.md)

| Godot Signal | SMS Event | Params |
|-|-|-|
| begin_node_move | `on <id>.beginNodeMove() { ... }` | — |
| connection_drag_ended | `on <id>.connectionDragEnded() { ... }` | — |
| connection_drag_started | `on <id>.connectionDragStarted(fromNode, fromPort, isOutput) { ... }` | Variant fromNode, int fromPort, bool isOutput |
| connection_from_empty | `on <id>.connectionFromEmpty(toNode, toPort, releasePosition) { ... }` | Variant toNode, int toPort, Vector2 releasePosition |
| connection_request | `on <id>.connectionRequest(fromNode, fromPort, toNode, toPort) { ... }` | Variant fromNode, int fromPort, Variant toNode, int toPort |
| connection_to_empty | `on <id>.connectionToEmpty(fromNode, fromPort, releasePosition) { ... }` | Variant fromNode, int fromPort, Vector2 releasePosition |
| copy_nodes_request | `on <id>.copyNodesRequest() { ... }` | — |
| cut_nodes_request | `on <id>.cutNodesRequest() { ... }` | — |
| delete_nodes_request | `on <id>.deleteNodesRequest(nodes) { ... }` | Variant nodes |
| disconnection_request | `on <id>.disconnectionRequest(fromNode, fromPort, toNode, toPort) { ... }` | Variant fromNode, int fromPort, Variant toNode, int toPort |
| duplicate_nodes_request | `on <id>.duplicateNodesRequest() { ... }` | — |
| end_node_move | `on <id>.endNodeMove() { ... }` | — |
| frame_rect_changed | `on <id>.frameRectChanged(frame, newRect) { ... }` | Object frame, Variant newRect |
| graph_elements_linked_to_frame_request | `on <id>.graphElementsLinkedToFrameRequest(elements, frame) { ... }` | Variant elements, Variant frame |
| node_deselected | `on <id>.nodeDeselected(node) { ... }` | Object node |
| node_selected | `on <id>.nodeSelected(node) { ... }` | Object node |
| paste_nodes_request | `on <id>.pasteNodesRequest() { ... }` | — |
| popup_request | `on <id>.popupRequest(atPosition) { ... }` | Vector2 atPosition |
| scroll_offset_changed | `on <id>.scrollOffsetChanged(offset) { ... }` | Vector2 offset |

## SML Items (TODO)

This control appears to manage internal items, but a dedicated SML pseudo-child specification has not been defined yet.
Use the generated signals and the `collection: true` marker in `sms-reference.sml` as implementation hints.
