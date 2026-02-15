# ItemList

## Inheritance

[ItemList](ItemList.md) → [Control](Control.md) → [CanvasItem](CanvasItem.md) → [Node](Node.md) → [Object](Object.md)

## Collection Items

This control appears to manage internal **items** (collection-style API).
Items are typically not represented as child nodes/properties in Godot.
In SML, this will be represented via **pseudo child elements** (documented per control).

## Derived Classes

### Direct subclasses

- [FileSystemList](FileSystemList.md)
- [TileSetSourceItemList](TileSetSourceItemList.md)

## Properties

This page lists **only properties declared by `ItemList`**.
Inherited properties are documented in: [Control](Control.md)

| Godot Property | SML Property | Type | Default |
|-|-|-|-|
| allow_reselect | allowReselect | bool | — |
| allow_rmb_select | allowRmbSelect | bool | — |
| allow_search | allowSearch | bool | — |
| auto_height | autoHeight | bool | — |
| auto_width | autoWidth | bool | — |
| fixed_column_width | fixedColumnWidth | int | — |
| icon_mode | iconMode | int | — |
| icon_scale | iconScale | float | — |
| item_count | itemCount | int | — |
| max_columns | maxColumns | int | — |
| max_text_lines | maxTextLines | int | — |
| same_column_width | sameColumnWidth | bool | — |
| scroll_hint_mode | scrollHintMode | int | — |
| select_mode | selectMode | int | — |
| text_overrun_behavior | textOverrunBehavior | int | — |
| tile_scroll_hint | tileScrollHint | bool | — |
| wraparound_items | wraparoundItems | bool | — |

## Events

This page lists **only signals declared by `ItemList`**.
Inherited signals are documented in: [Control](Control.md)

| Godot Signal | SMS Event | Params |
|-|-|-|
| empty_clicked | `on <id>.emptyClicked(atPosition, mouseButtonIndex) { ... }` | Vector2 atPosition, int mouseButtonIndex |
| item_activated | `on <id>.itemActivated(index) { ... }` | int index |
| item_clicked | `on <id>.itemClicked(index, atPosition, mouseButtonIndex) { ... }` | int index, Vector2 atPosition, int mouseButtonIndex |
| item_selected | `on <id>.itemSelected(index) { ... }` | int index |
| multi_selected | `on <id>.multiSelected(index, selected) { ... }` | int index, bool selected |
