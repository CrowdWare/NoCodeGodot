# Layout Modes v1 (SML → Godot)

Dieses Dokument beschreibt die aktuell implementierte v1-Semantik für `app`- und `document`-Layout im Runner.

## 1) app mode

`app` ist absolut-positioniert mit Anchor-Verhalten (WinForms-ähnlich).

### Unterstützte Properties

- `x`, `y`, `width`, `height`
- `anchors: top | bottom | left | right`
- `anchorTop`, `anchorBottom`, `anchorLeft`, `anchorRight`
- `centerX`, `centerY`
- `minSize: w, h` auf `Window`

### Anchor-Semantik

- Nur eine Seite verankert (z. B. `anchorRight: true`):
  - Größe bleibt konstant
  - Position folgt der konstanten Margin
- Beide Seiten verankert (z. B. `anchorLeft: true` + `anchorRight: true`):
  - Erste Seite bleibt fix
  - Größe passt sich an, um beide Margins konstant zu halten

Vertikal analog (`top`/`bottom`).

### Hinweise

- Fonts werden in `app` nicht automatisch skaliert.
- Es gibt keine Cross-Control-Constraints (`below`, `nextTo`, Gewichtung, Breakpoints).

## 2) document mode

`document` ist content-first mit Flow und optionalem Scrolling.

### Unterstützte Container

- `Page` (Root auf mobilen/document-getriebenen Layouts)
- `Column`
- `Row`
- `Box`

Standardmäßig haben diese Container `layoutMode=document`, wenn nicht explizit überschrieben.

### Scrolling-Properties

- `scrollable: true|false`
- `scrollBarWidth: <px>`
- `scrollBarHeight: <px>`
- `scrollBarPosition: right|left|top|bottom`
- `scrollBarVisible: true|false`
- `scrollBarVisibleOnScroll: true|false`
- `scrollBarFadeOutTime: <ms>`

Semantik:

- `scrollable` aktiviert ScrollContainer-Verhalten, ändert aber nicht die Flow-Regeln.
- `scrollBarVisibleOnScroll: true` blendet Scrollbars nur bei Scroll-Interaktion ein.
- `scrollBarFadeOutTime` steuert den Ausblendzeitpunkt nach letzter Scroll-Interaktion.

## 3) Logging / Debug

Der Runner loggt:

- `layout/app`: berechnete Child-Rechtecke inkl. Anchor-Zustand
- `layout/document`: Parent/Content-Bezug im document-Kontext
- `scroll`: Sichtbarkeit, Position, Fade-Parameter
- `Window minSize` bei Anwendung auf das Godot-Window

## 4) Bekannte v1-Einschränkungen

- `scrollBarPosition` mit `left/top` ist aktuell best-effort und wird als Warnung geloggt (Godot nutzt native Scrollbars primär rechts/unten).
- Kein flexbox-/constraint-basiertes Layout.
- Keine implizite Font-Skalierung in `app`.
