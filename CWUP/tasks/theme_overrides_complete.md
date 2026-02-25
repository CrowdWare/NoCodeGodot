# Task: Complete Theme Override Coverage for All Controls

## Goal

All Godot theme overrides that make sense in an SML context should be implementable via SML properties. Currently only `color`, `fontSize`, `font`, and `spacing` are implemented. Many other useful overrides are missing.

## Background

Godot's theme override system allows per-node customization without touching the global theme. The SML mapper calls `AddTheme*Override()` methods on controls. These must be:
1. Implemented in `NodePropertyMapper.cs`
2. Documented in `tools/specs/runtime_overrides.gd` under `propertiesByType`

## Missing Overrides to Implement

### fontWeight and fontFace – Font Selection by Name

Currently, the only way to select a font is via a file path (`font: "res://fonts/Roboto-Bold.ttf"`). This is verbose and requires the caller to know the exact file location and naming convention. Two higher-level properties should be introduced:

#### `fontFace` – select a font family by name

```sml
Label {
    fontFace: "Roboto"
    text: "Hello"
}
```

The mapper looks up the family name in a font registry and loads the matching `.ttf`/`.otf` file. If no registry entry is found, falls back to system default with a warning.

#### `fontWeight` – select a weight variant

```sml
Label {
    fontFace: "Roboto"
    fontWeight: 700       # numeric (100–900)
    fontWeight: bold      # named alias
    text: "Hello"
}
```

Named weight aliases to support: `thin` (100), `extraLight` (200), `light` (300), `regular` (400), `medium` (500), `semiBold` (600), `bold` (700), `extraBold` (800), `black` (900).

> **Why fontWeight alone doesn't work in Godot**: Godot has no built-in weight axis on Label. Weight must be encoded in the FontFile resource. The only path is either loading a separate `.ttf` file per weight, or using `FontVariation` with a variable font that has a `wght` axis.

#### Font Registry – two possible approaches

**Option A: Fonts resource block in SML (recommended)**

Add a `Fonts` resource namespace (analogous to `Colors`/`Strings`) to the SML document:

```sml
Fonts {
    Roboto-Regular:   "res://fonts/Roboto-Regular.ttf"
    Roboto-Bold:      "res://fonts/Roboto-Bold.ttf"
    Roboto-Light:     "res://fonts/Roboto-Light.ttf"
}

Label {
    fontFace: "Roboto"
    fontWeight: bold
}
```

The mapper combines `fontFace` + `fontWeight` → key `"Roboto-Bold"` → looks up in `Fonts` resource → calls `AddThemeFontOverride`.

**Option B: Naming convention (no registry)**

Convention: `res://fonts/<Face>-<Weight>.ttf` where Weight is the named alias capitalized. The mapper constructs the path directly. Simpler, but less flexible.

#### Affected controls

Same as the existing `font` property: Label, RichTextLabel, Button, TextEdit, Markdown (and the additional controls from "Font – additional controls" below).

#### Implementation steps

1. Add `Fonts` namespace support to `SmlDocument.Resources` (alongside Colors/Strings/Layouts/Icons)
2. Add `Fonts` as a known namespace in `SmlParser.cs` → `ValidateResourceRefs()`
3. Extend `LocalizationStore` or create `FontStore` to resolve `fontFace`+`fontWeight` → FontFile
4. Add `case "fontface":` and `case "fontweight":` to `NodePropertyMapper.Apply()`
5. Implement `ApplyFontFace()` and `ApplyFontWeight()` that buffer the values and resolve once both are set (or use a two-pass approach)
6. Add entries to `tools/specs/runtime_overrides.gd` for Label, Button, RichTextLabel, TextEdit
7. Add `Fonts` to known namespaces in `tools/specs/resources.gd`
8. Run `./run_runner.sh docs`

---

### Font Color – additional controls

| Control | Godot override | SML property |
|---|---|---|
| LineEdit | `font_color` | `color` |
| SpinBox (inner LineEdit) | `font_color` | `color` |
| OptionButton | `font_color` | `color` |
| MenuButton | `font_color` | `color` |
| CheckBox / CheckButton | `font_color` | `color` |
| Markdown | `font_color` | `color` |

### Font Size – additional controls

| Control | Godot override | SML property |
|---|---|---|
| LineEdit | `font_size` | `fontSize` / `fontSizePx` |
| OptionButton | `font_size` | `fontSize` |
| MenuButton | `font_size` | `fontSize` |
| CodeEdit | `font_size` | `fontSize` |
| Markdown | `font_size` | `fontSize` |

### Font – additional controls

| Control | Godot override | SML property |
|---|---|---|
| LineEdit | `font` | `font` / `fontSource` |
| OptionButton | `font` | `font` |
| MenuButton | `font` | `font` |
| CodeEdit | `font` | `font` |
| Markdown | `font` | `font` |

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
| Markdown | `normal` stylebox | `bgColor` | |

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
- High: `fontFace` + `fontWeight` with Fonts resource block (enables clean font selection without hardcoded paths)
- Medium: `bgColor` for PanelContainer, Panel, Label
- Low: `borderRadius`, `borderColor`, GridContainer spacing
