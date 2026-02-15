# PopupMenu

## Inheritance

[PopupMenu](PopupMenu.md) → [Popup](Popup.md) → [Window](Window.md) → [Viewport](Viewport.md) → [Node](Node.md) → [Object](Object.md)

## Properties

This page lists **only properties declared by `PopupMenu`**.
Inherited properties are documented in: [Popup](Popup.md)

| Godot Property | SML Property | Type | Default |
|-|-|-|-|
| allow_search | allowSearch | bool | — |
| hide_on_checkable_item_selection | hideOnCheckableItemSelection | bool | — |
| hide_on_item_selection | hideOnItemSelection | bool | — |
| hide_on_state_item_selection | hideOnStateItemSelection | bool | — |
| item_count | itemCount | int | — |
| prefer_native_menu | preferNativeMenu | bool | — |
| shrink_height | shrinkHeight | bool | — |
| shrink_width | shrinkWidth | bool | — |
| submenu_popup_delay | submenuPopupDelay | float | — |
| system_menu_id | systemMenuId | int | — |

## Events

This page lists **only signals declared by `PopupMenu`**.
Inherited signals are documented in: [Popup](Popup.md)

| Godot Signal | SMS Event | Params |
|-|-|-|
| id_focused | `on <id>.idFocused(id) { ... }` | int id |
| id_pressed | `on <id>.idPressed(id) { ... }` | int id |
| index_pressed | `on <id>.indexPressed(index) { ... }` | int index |
| menu_changed | `on <id>.menuChanged() { ... }` | — |

## SML Items

`PopupMenu` items are defined as **SML child nodes** (pseudo elements).
The runtime converts them to Godot menu items internally.

### Supported item elements

- `Item`
- `CheckItem`
- `Separator`

### Item properties (SML)

| Property | Type | Default | Notes |
|-|-|-|-|
| id | identifier | — | Optional. Enables id-based event sugar (`on <id>.pressed() { ... }`). |
| text | string | "" | Display text. |
| checked | bool | false | Only for `CheckItem`. |
| disabled | bool | false | Optional. |

### Example

```sml
PopupMenu { id: fileMenu
    Item { id: open; text: "Open" }
    Item { id: save; text: "Save" }
    Separator { }
    CheckItem { id: autosave; text: "Auto Save"; checked: true }
}
```

### SMS Event Examples

```sms
// With explicit item ids:
on open.pressed() { ... }
on autosave.pressed() { ... }

// Without item ids (container fallback):
on fileMenu.idPressed(id) { ... }
```
