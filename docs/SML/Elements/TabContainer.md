# TabContainer

## Inheritance

[TabContainer](TabContainer.md) → [Container](Container.md) → [Control](Control.md) → [CanvasItem](CanvasItem.md) → [Node](Node.md) → [Object](Object.md)

## Collection Items

This control appears to manage internal **items** (collection-style API).
Items are typically not represented as child nodes/properties in Godot.
In SML, this will be represented via **pseudo child elements** (documented per control).

## Properties

This page lists **only properties declared by `TabContainer`**.
Inherited properties are documented in: [Container](Container.md)

| Godot Property | SML Property | Type | Default |
|-|-|-|-|
| all_tabs_in_front | allTabsInFront | bool | — |
| clip_tabs | clipTabs | bool | — |
| current_tab | currentTab | int | — |
| deselect_enabled | deselectEnabled | bool | — |
| drag_to_rearrange_enabled | dragToRearrangeEnabled | bool | — |
| switch_on_drag_hover | switchOnDragHover | bool | — |
| tab_alignment | tabAlignment | int | — |
| tab_focus_mode | tabFocusMode | int | — |
| tabs_position | tabsPosition | int | — |
| tabs_rearrange_group | tabsRearrangeGroup | int | — |
| tabs_visible | tabsVisible | bool | — |
| use_hidden_tabs_for_min_size | useHiddenTabsForMinSize | bool | — |

## Events

This page lists **only signals declared by `TabContainer`**.
Inherited signals are documented in: [Container](Container.md)

| Godot Signal | SMS Event | Params |
|-|-|-|
| active_tab_rearranged | `on <id>.activeTabRearranged(idxTo) { ... }` | int idxTo |
| pre_popup_pressed | `on <id>.prePopupPressed() { ... }` | — |
| tab_button_pressed | `on <id>.tabButtonPressed(tab) { ... }` | int tab |
| tab_changed | `on <id>.tabChanged(tab) { ... }` | int tab |
| tab_clicked | `on <id>.tabClicked(tab) { ... }` | int tab |
| tab_hovered | `on <id>.tabHovered(tab) { ... }` | int tab |
| tab_selected | `on <id>.tabSelected(tab) { ... }` | int tab |

## Child Properties (Context)

When a `Control` is used as a **direct child** of `TabContainer`, the following additional SML properties are supported.
These properties do **not** belong to the child control itself; they are interpreted by the parent `TabContainer`.

| SML Property | Type | Default | Description |
|-|-|-|-|
| tabTitle | string | "" | Title of the tab for this child page. |
| tabIcon | string | "" | Optional icon resource/path for the tab (if supported by runtime). |
| tabDisabled | bool | false | Disables selecting this tab. |
| tabHidden | bool | false | Hides this tab from the tab bar. |

### Example

```sml
TabContainer { id: tabs
    Panel { tabTitle: "Home" }
    Panel { tabTitle: "Settings"; tabDisabled: false }
}
```
