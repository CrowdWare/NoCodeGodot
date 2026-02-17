# Supported SML Elements (Runner)

This reference describes SML elements currently supported by `NoCodeRunner`.

## Element → Godot Mapping

| SML | Godot Control |
|---|---|
| `Window` | `Panel` |
| `Page` | `VBoxContainer` |
| `Panel` | `Panel` |
| `Label` | `Label` |
| `MarkdownLabel` | `RichTextLabel` |
| `Button` | `Button` |
| `TextEdit` | `TextEdit` |
| `CodeEdit` | `CodeEdit` |
| `Row` | `HBoxContainer` |
| `Column` | `VBoxContainer` |
| `Box` | `Panel` |
| `Tabs` | `TabContainer` |
| `Tab` | `VBoxContainer` |
| `Slider` | `HSlider` |
| `Video` | `VideoStreamPlayer` |
| `Viewport3D` | `Viewport3DControl` |
| `Markdown` | `VBoxContainer` (rendered markdown blocks) |
| `Image` | `TextureRect` |
| `Spacer` | `Control` |
| `DockSpace` | `DockSpace` (custom, `VBoxContainer`-based) |
| `DockPanel` | `DockPanel` (custom, `PanelContainer`-based) |
| `MenuBar` | `MenuBar` |
| `Menu` | `MenuButton` (built from SML `Menu` data) |
| `MenuItem` | `PopupMenu` item (built from SML `MenuItem` data) |
| `Separator` | `PopupMenu` separator |

## Common Properties

Commonly supported (depending on node type):

- Text/content: `text`, `label`, `title`
- Typographic: `font`, `fontSize`, `wrap`, `align`, `halign`, `color`, `role`
- Sizing/position: `width`, `height`, `x`, `y`, `position`, `size`, `minSize`, `sizeFlagsHorizontal`, `sizeFlagsVertical`
- Spacing: `spacing`, `padding`
- Anchor/layout metadata: `anchors`, `anchorLeft`, `anchorRight`, `anchorTop`, `anchorBottom`, `centerX`, `centerY`, `layoutMode`
- Interaction metadata: `id`, `action`, `clicked`

### `id` semantics (important)

`id` is treated as an **identifier symbol**, not as a generic numeric property.

Allowed:

- `id: mainButton` (identifier)
- `id: 3` (**numeric identifier**)

Not allowed:

- `id: "mainButton"` (quoted string)
- `id: 1,2` (tuple)

Notes:

- Numeric `id` values are interpreted as identifier symbols (for example, `"3"`), then mapped by runtime ID scope to internal integer IDs.
- Duplicate `id` values in the same SML document scope are rejected by the parser.

### Dual layout syntax (`x/y/width/height` + `position/size`)

The runner uses one internal rect-style geometry model. Both syntaxes map to the same internal fields:

- `x` / `y` (WinForms-style)
- `position: x, y` (Godot-style)
- `width` / `height` (WinForms-style)
- `size: width, height` (Godot-style)

Mixed usage is allowed. If multiple geometry properties target the same component, the **last parsed value wins**.

Example:

```qml
Panel {
    x: 10
    position: 50, 60
    width: 200
    size: 320, 100
}
```

Resulting rect:

- `x = 50`, `y = 60`
- `width = 320`, `height = 100`

## Container/Layout Notes

- `Page`, `Column`, `Row`, `Markdown`, `Box` default to `layoutMode: document` if not specified.
- `Window`, `Page`, `Panel`, `Column`, `Markdown`, `Box`, `Tabs`, `Tab` are expanded to fill available size by default unless overridden.

## Node-Specific Properties

### Container nodes (`Page`, `Panel`, `Column`, `Row`, `Box`, `Markdown`, `Tab`)

- `spacing` (int): gap between children for `Row`/`Column` (`BoxContainer`-based)
- `padding` (shorthand, int tuple):
  - `padding: 8` → top/right/bottom/left = 8
  - `padding: 8,16` → top/bottom=8, left/right=16
  - `padding: 8,12,16,20` → top/right/bottom/left

Validation:

- only `1`, `2`, or `4` integer values are accepted
- `3` values are rejected with parser error
- floats are rejected (SML numeric rule: integer-only)

### Window

In addition to common properties, `Window` supports runtime bootstrap/scaling metadata:

| Property | Type | Notes |
|---|---|---|
| `title` | `string` | Applied to native window title (`Window.Title`) |
| `pos` | `vec2i` (`x, y`) | Sets native window position |
| `size` | `vec2i` (`w, h`) | Sets native window size |
| `minSize` | `vec2i` (`w, h`) | Sets runtime window minimum size metadata (`MetaWindowMinSizeX/Y`) |
| `scaling` | `enum` | `layout` or `fixed` (handled by `SmlUiBuilder` / `Main`) |
| `designSize` | `vec2i` (`w, h`) | Required when `scaling: fixed`; used as fixed render design resolution |
| `layoutMode` | `enum` | `app` or `document` metadata |

Notes:

- `scaling` and `designSize` are consumed before generic property mapping and stored as window-level metadata.
- `scaling: fixed` without valid `designSize` causes runtime startup validation failure.
- `pos` and `size` are also accepted as shorthand vectors (`pos: 20,20`, `size: 1024,768`).
- Window content still follows normal node/layout rules (`Page`, `Column`, etc.).

### TextEdit / CodeEdit

- `editable` / `readonly`
- `multiline`
- `wrap`
- `font`, `fontSize`
- `syntax` (`CodeEdit` only): `auto` (default), `sml`, `cs`, `markdown`, or custom `res:/...` rule file

`CodeEdit` runtime syntax behavior (V1):

- Without associated file path:
  - `syntax: "auto"` → `plain_text` (no highlighter)
  - `syntax: "sml" | "cs" | "markdown"` → mapped highlighter
  - `syntax: "res:/..."` → custom rule file highlighter
- On every `load(path)` or `save_as(path)` call:
  - syntax is always recomputed from extension (`.sml`, `.cs`, `.md`, `.markdown`, else `plain_text`)
  - extension mapping always overrides previous syntax (including custom rule paths)

Rule file mapping:

- `sml` → `res:/syntax/sml_syntax.cs`
- `cs` → `res:/syntax/cs_syntax.cs`
- `markdown` → `res:/syntax/markdown_syntax.cs`

### Image

- `src` (resolved via runtime URI resolver)
- `alt` (stored as metadata)

### Video

- `source` or `url`
- `autoplay`

### Slider

- `min`, `max`, `step`, `value`
- can dispatch action workflows such as `animScrub`

### TreeView

- `hideRoot` (`bool`, default: `true`)
- `showGuides` (`bool`, default: `true`)
- `indent` (`int`)
- `rowHeight` (`int`)

`Item` children can contain optional `Toggle` children:

- `id` (identifier)
- `imageOn` (icon when state is `true`, required)
- `imageOff` (icon when state is `false`, required)
- `state` (`bool`, default: `true`)

`Item` supports:

- `icon` (optional item icon)

Notes:

- `hideRoot` maps to `Tree.HideRoot`.
- `showGuides` controls guide-line rendering only (`draw_guides` theme constant).
- `indent` is applied via tree theme constant override (`item_margin`).
- `rowHeight` is applied via a theme constant override (`v_separation`).
- Toggle click event convention:
  - `<treeId>ItemToggle(Id itemId, TreeViewItem item, ToggleId toggleId, bool isOn)`
  - fallback: `treeViewItemToggle(Id itemId, TreeViewItem item, ToggleId toggleId, bool isOn)`

Example:

```qml
Item {
    id: 11
    text: "Branch 1.1"
    icon: "res:/assets/images/document.png"

    Toggle {
        id: showObject
        imageOn: "res:/assets/images/eye_open.png"
        imageOff: "res:/assets/images/eye_closed.png"
    }
}
```

### Viewport3D

- `model` / `modelSource`
- `animation` / `animationSource`
- `playAnimation`
- `playFirstAnimation` / `autoplayAnimation`
- `defaultAnimation`
- `playLoop`
- `cameraDistance`
- `lightEnergy`
- `id` (used for camera/animation action targeting)

### DockSpace

- `allowFloating` (`bool`, default: `true`)
- `allowTabbing` (`bool`, default: `true`)
- `allowSplitting` (`bool`, default: `true`)
- `splitterSize` (`int`, default: `6`)

Behavior:

- If `allowSplitting` is enabled, the dock layout uses split containers with draggable split handles.
- `splitterSize` controls the visual/interactive thickness of split separators.
- Dock layout persistence stores both:
  - panel state (`slot`, `tabIndex`, `active`, `floating`, `closed`)
  - splitter offsets (panel widths/heights after manual resize)

### DockPanel

- `id` (`string`/identifier)
- `title` (`string`)
- `area` (`string`): `far-left`, `left`, `center`, `right`, `far-right`, `bottom-far-left`, `bottom-left`, `bottom-right`, `bottom-far-right`
- `closeable` / `closable` (`bool`, default: `true`)
- `floatable` (`bool`, default: `true`)
- `dockable` / `moveable` / `movable` (`bool`, default: `true`)
- `isDropTarget` (`bool`, default: `true`)

Behavior:

- `dockable: false` prevents re-docking via drag & drop and blocks slot changes via docking move commands.
- `isDropTarget: false` prevents other dock panels from being dropped into that panel/slot (useful e.g. for a fixed viewport area).

### MenuBar / Menu / MenuItem

`MenuBar` supports:

- `id`
- `preferGlobalMenu` (`bool`, default platform behavior)

`Menu` supports:

- `id`
- `title`

`MenuItem` supports:

- `id`
- `text`
- `shortcut` (stored as metadata; visual shortcut rendering is runtime/platform dependent)

`Separator` has no properties.

Runtime behavior:

- Menu clicks dispatch action `menuItemSelected` with:
  - `SourceId` = menu id
  - `Clicked` = menu item id
- Companion SMS scripts can handle this via:

```sms
fun menuItemSelected(menu, id) {
    log.info("menuItemSelected -> menu=${menu}, id=${id}")
}
```

macOS notes:

- If `preferGlobalMenu: true`, the runtime prefers global menu integration.
- For `appMenu`, a `settings` entry is auto-added when missing.

## Markdown Node Behavior

`Markdown` supports:

- inline markdown via `text`
- external markdown via `src`

It is preprocessed into runtime nodes:

- headings → `MarkdownLabel`
- paragraphs → `MarkdownLabel`
- list items → `Row` + bullet `Label` + text `MarkdownLabel`
- image → `Image`
- code fence → `CodeEdit`

Markdown extras include basic emoji aliases and simple bold/italic conversion to BBCode.

## Unsupported / Unknown Fields

- Unknown properties are logged as warnings and ignored.
- Unknown nodes are logged as warnings and skipped.
