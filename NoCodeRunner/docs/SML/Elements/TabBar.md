# TabBar

## Inheritance

[TabBar](TabBar.md) → [Control](Control.md) → [CanvasItem](CanvasItem.md) → [Node](Node.md) → [Object](Object.md)

## Collection Items

This control appears to manage internal **items** (collection-style API).
Items are typically not represented as child nodes/properties in Godot.
In SML, this will be represented via **pseudo child elements** (documented per control).

## Properties

This page lists **only properties declared by `TabBar`**.
Inherited properties are documented in: [Control](Control.md)

| Godot Property | SML Property | Type | Default |
|-|-|-|-|
| clip_tabs | clipTabs | bool | — |
| close_with_middle_mouse | closeWithMiddleMouse | bool | — |
| current_tab | currentTab | int | — |
| deselect_enabled | deselectEnabled | bool | — |
| drag_to_rearrange_enabled | dragToRearrangeEnabled | bool | — |
| max_tab_width | maxTabWidth | int | — |
| scroll_to_selected | scrollToSelected | bool | — |
| scrolling_enabled | scrollingEnabled | bool | — |
| select_with_rmb | selectWithRmb | bool | — |
| switch_on_drag_hover | switchOnDragHover | bool | — |
| tab_alignment | tabAlignment | int | — |
| tab_close_display_policy | tabCloseDisplayPolicy | int | — |
| tab_count | tabCount | int | — |
| tabs_rearrange_group | tabsRearrangeGroup | int | — |

## Events

This page lists **only signals declared by `TabBar`**.
Inherited signals are documented in: [Control](Control.md)

| Godot Signal | SMS Event | Params |
|-|-|-|
| active_tab_rearranged | `on <id>.activeTabRearranged(idxTo) { ... }` | int idxTo |
| tab_button_pressed | `on <id>.tabButtonPressed(tab) { ... }` | int tab |
| tab_changed | `on <id>.tabChanged(tab) { ... }` | int tab |
| tab_clicked | `on <id>.tabClicked(tab) { ... }` | int tab |
| tab_close_pressed | `on <id>.tabClosePressed(tab) { ... }` | int tab |
| tab_hovered | `on <id>.tabHovered(tab) { ... }` | int tab |
| tab_rmb_clicked | `on <id>.tabRmbClicked(tab) { ... }` | int tab |
| tab_selected | `on <id>.tabSelected(tab) { ... }` | int tab |
