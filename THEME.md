# THEME Guide

This project uses a Godot theme file at:

- `NoCodeRunner/theme.tres`

The theme is loaded in `NoCodeRunner/Main.cs` via:

- `GD.Load<Theme>("res://theme.tres")`

---

## Quick workflow (from UI screenshot to Godot theme)

1. Pick a reference UI screenshot (for example `images/jetpack_dark.png`).
2. Define a small set of design tokens:
   - background/surface colors
   - text colors
   - accent color
   - border colors
   - corner radius and spacing
3. Map those tokens to Godot controls in `theme.tres`:
   - `Panel`, `PanelContainer`
   - `Button` states (`normal`, `hover`, `pressed`, `disabled`, `focus`)
   - `LineEdit`, `TextEdit`
   - `TabBar`, `TabContainer`
   - `Tree`, `ItemList`
   - Scrollbar accent colors
4. Run the app and compare visually.
5. Fine-tune colors, contrast, and spacing in small iterations.

---

## Where to change the accent color

If you want to switch the accent (for example blue → green), edit these entries in `NoCodeRunner/theme.tres`:

- `HScrollBar/colors/accent_color`
- `VScrollBar/colors/accent_color`
- `StyleBoxFlat_focus -> border_color`
- `StyleBoxFlat_tab_selected -> bg_color` and `border_color`
- `StyleBoxFlat_button_pressed -> bg_color` and `border_color`
- `StyleBoxFlat_input_focus -> border_color`
- `LineEdit/colors/caret_color`, `LineEdit/colors/selection_color`
- `TextEdit/colors/caret_color`, `TextEdit/colors/selection_color`
- `Tree/colors/selection_color`
- `ItemList/colors/selection_color`

Tip: keep 2-3 accent shades (base/strong/soft) and reuse them consistently.

---

## Suggested structure for future edits

When editing `theme.tres`, keep this order:

1. `sub_resource` style boxes (surfaces and control states)
2. control style assignments (`.../styles/...`)
3. control color assignments (`.../colors/...`)

This makes future adjustments faster and avoids missing related entries.

---

## Validation checklist

After changing the theme, verify:

- normal/hover/pressed/disabled button states
- focus ring visibility on dark backgrounds
- text readability and contrast
- selected tab and selected list/tree row visibility
- caret and selection colors in text inputs

If one control still looks off, search the matching `styles/*` and `colors/*` entries and adjust locally.1
