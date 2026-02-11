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

## Common Properties

Commonly supported (depending on node type):

- Text/content: `text`, `label`, `title`
- Typographic: `font`, `fontSize`, `wrap`, `align`, `halign`, `color`, `role`
- Sizing/position: `width`, `height`, `x`, `y`, `fillMaxSize`, `minSize`
- Anchor/layout metadata: `anchors`, `anchorLeft`, `anchorRight`, `anchorTop`, `anchorBottom`, `centerX`, `centerY`, `layoutMode`
- Interaction metadata: `id`, `action`, `clicked`

## Container/Layout Notes

- `Page`, `Column`, `Row`, `Markdown`, `Box` default to `layoutMode: document` if not specified.
- `Window`, `Page`, `Panel`, `Column`, `Markdown`, `Box`, `Tabs`, `Tab` are expanded to fill available size by default unless overridden.

## Node-Specific Properties

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

### Image

- `src` (resolved via runtime URI resolver)
- `alt` (stored as metadata)

### Video

- `source` or `url`
- `autoplay`

### Slider

- `min`, `max`, `step`, `value`
- can dispatch action workflows such as `animScrub`

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
