# Tree

## Inheritance

[Tree](Tree.md) → [Control](Control.md) → [CanvasItem](CanvasItem.md) → [Node](Node.md) → [Object](Object.md)

## Collection Items

This control appears to manage internal **items** (collection-style API).
Items are typically not represented as child nodes/properties in Godot.
In SML, this will be represented via **pseudo child elements** (documented per control).

## Properties

This page lists **only properties declared by `Tree`**.
Inherited properties are documented in: [Control](Control.md)

| Godot Property | SML Property | Type | Default |
|-|-|-|-|
| allow_reselect | allowReselect | bool | — |
| allow_rmb_select | allowRmbSelect | bool | — |
| allow_search | allowSearch | bool | — |
| auto_tooltip | autoTooltip | bool | — |
| column_titles_visible | columnTitlesVisible | bool | — |
| columns | columns | int | — |
| drop_mode_flags | dropModeFlags | int | — |
| enable_drag_unfolding | enableDragUnfolding | bool | — |
| enable_recursive_folding | enableRecursiveFolding | bool | — |
| hide_folding | hideFolding | bool | — |
| hide_root | hideRoot | bool | — |
| scroll_hint_mode | scrollHintMode | int | — |
| scroll_horizontal_enabled | scrollHorizontalEnabled | bool | — |
| scroll_vertical_enabled | scrollVerticalEnabled | bool | — |
| select_mode | selectMode | int | — |
| tile_scroll_hint | tileScrollHint | bool | — |

## Events

This page lists **only signals declared by `Tree`**.
Inherited signals are documented in: [Control](Control.md)

| Godot Signal | SMS Event | Params |
|-|-|-|
| button_clicked | `on <id>.buttonClicked(item, column, id, mouseButtonIndex) { ... }` | Object item, int column, int id, int mouseButtonIndex |
| cell_selected | `on <id>.cellSelected() { ... }` | — |
| check_propagated_to_item | `on <id>.checkPropagatedToItem(item, column) { ... }` | Object item, int column |
| column_title_clicked | `on <id>.columnTitleClicked(column, mouseButtonIndex) { ... }` | int column, int mouseButtonIndex |
| custom_item_clicked | `on <id>.customItemClicked(mouseButtonIndex) { ... }` | int mouseButtonIndex |
| custom_popup_edited | `on <id>.customPopupEdited(arrowClicked) { ... }` | bool arrowClicked |
| empty_clicked | `on <id>.emptyClicked(clickPosition, mouseButtonIndex) { ... }` | Vector2 clickPosition, int mouseButtonIndex |
| item_activated | `on <id>.itemActivated() { ... }` | — |
| item_collapsed | `on <id>.itemCollapsed(item) { ... }` | Object item |
| item_edited | `on <id>.itemEdited() { ... }` | — |
| item_icon_double_clicked | `on <id>.itemIconDoubleClicked() { ... }` | — |
| item_mouse_selected | `on <id>.itemMouseSelected(mousePosition, mouseButtonIndex) { ... }` | Vector2 mousePosition, int mouseButtonIndex |
| item_selected | `on <id>.itemSelected() { ... }` | — |
| multi_selected | `on <id>.multiSelected(item, column, selected) { ... }` | Object item, int column, bool selected |
| nothing_selected | `on <id>.nothingSelected() { ... }` | — |

## SML Items (TODO)

This control appears to manage internal items, but a dedicated SML pseudo-child specification has not been defined yet.
Use the generated signals and the `collection: true` marker in `sms-reference.sml` as implementation hints.
