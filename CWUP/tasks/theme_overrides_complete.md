# Task: Complete Theme Override Coverage for All Controls

## Goal

All Godot theme overrides that make sense in an SML context should be implementable via SML properties. Currently only `color`, `fontSize`, `font`, and `spacing` are implemented. Many other useful overrides are missing.

## Background

Godot's theme override system allows per-node customization without touching the global theme. The SML mapper calls `AddTheme*Override()` methods on controls. These must be:
1. Implemented in `NodePropertyMapper.cs`
2. Documented in `tools/specs/runtime_overrides.gd` under `propertiesByType`

## Missing Overrides to Implement

### Font Color – additional controls

| Control | Godot override | SML property |
|---|---|---|
| LineEdit | `font_color` | `color` |
| SpinBox (inner LineEdit) | `font_color` | `color` |
| OptionButton | `font_color` | `color` |
| MenuButton | `font_color` | `color` |
| CheckBox / CheckButton | `font_color` | `color` |

### Font Size – additional controls

| Control | Godot override | SML property |
|---|---|---|
| LineEdit | `font_size` | `fontSize` / `fontSizePx` |
| OptionButton | `font_size` | `fontSize` |
| MenuButton | `font_size` | `fontSize` |
| CodeEdit | `font_size` | `fontSize` |

### Font – additional controls

| Control | Godot override | SML property |
|---|---|---|
| LineEdit | `font` | `font` / `fontSource` |
| OptionButton | `font` | `font` |
| MenuButton | `font` | `font` |
| CodeEdit | `font` | `font` |

### Spacing – additional BoxContainer subtypes

| Control | Godot override | SML property |
|---|---|---|
| HFlowContainer | `h_separation` | `spacing` |
| VFlowContainer | `v_separation` | `spacing` |
| GridContainer | `h_separation`, `v_separation` | `spacingH`, `spacingV` |
| FlowContainer | `h_separation`, `v_separation` | `spacingH`, `spacingV` |

### Background Color / StyleBox Overrides

These require creating a `StyleBoxFlat` and setting it via `AddThemeStyleboxOverride`:

| Control | Godot override | SML property | Notes |
|---|---|---|---|
| PanelContainer | `panel` stylebox | `bgColor` | Already partially done for role system |
| Panel | `panel` stylebox | `bgColor` | |
| Label | `normal` stylebox | `bgColor` | |
| Button | `normal` / `hover` / `pressed` stylebox | `bgColor` | Needs state variants |
| LineEdit | `normal` stylebox | `bgColor` | |
| TextEdit | `normal` stylebox | `bgColor` | |

### Border / Radius (StyleBoxFlat properties)

If a `bgColor` stylebox override is set, the following could also be exposed:

| SML property | StyleBoxFlat field | Notes |
|---|---|---|
| `borderRadius` | `corner_radius_*` (all 4) | Single value for all corners |
| `borderColor` | `border_color` | |
| `borderWidth` | `border_width_*` (all 4) | Single value for all sides |

### Constants

| Control | Godot override | SML property |
|---|---|---|
| ItemList | `item_spacing` | `spacing` |
| TabContainer | `side_margin` | `tabMargin` |
| Tree | `button_margin` | already partially covered |

## Implementation Steps

1. **For each new control + property**:
   - Add case to the relevant `Apply*()` method in `NodePropertyMapper.cs` (or create a new method)
   - Add entry to `propertiesByType` in `tools/specs/runtime_overrides.gd`

2. **For StyleBox-based properties** (`bgColor`, `borderRadius` etc.):
   - Create a helper `ApplyBgColor(Control, string color)` that creates a `StyleBoxFlat`
   - Decide: create new stylebox or mutate an existing theme override?
   - Consider interactions with the role-based styling already in place

3. **Run docs**: `./run_runner.sh docs`

4. **Add tests** in `SMLCore.Tests` for new property names (parser-level)

## Priority

- High: `color` + `fontSize` for LineEdit, OptionButton, MenuButton
- Medium: `bgColor` for PanelContainer, Panel, Label
- Low: `borderRadius`, `borderColor`, GridContainer spacing
