# Define and Implement Layout Modes app and document (SML → Godot)

## Goal

Create a deterministic layout system with two modes:
	•	app (absolute positioning + anchors, WinForms-like)
	•	document (flow + scrolling, content-first)

This task captures the current decisions and turns them into a spec + implementation plan.

⸻

## 1) Mode: app

1.1 Supported properties (complete)
Window inherits from Panel, where layoutMode = app.
Panel supports:
	•	Geometry:
	•	x, y, width, height
	•	Anchors to parent edges:
	•	anchors are top, bottom, left, right

Anchor semantics

Anchors keep the corresponding margins constant when the parent resizes.
	•	If only one side is anchored (e.g. anchorRight: true):
	•	size stays fixed
	•	position changes to keep that margin constant
	•	If both sides are anchored (e.g. anchorLeft: true and anchorRight: true):
	•	position stays fixed on the first side
	•	size changes to keep both margins constant

(Apply same logic for vertical axis.)

Optional (already implied by usage)
	•	centerX: true, centerY: true (centering in parent)
	•	minSize at Window level (prevents shrinking below usability)

1.2 Non-goals
	•	No relative constraints like “below/nextTo another control”
	•	No weight-based layout
	•	No implicit scaling of fonts on resize

1.3 Acceptance criteria
	•	Resizing window changes root/control sizes and/or positions according to anchor rules.
	•	Fonts do not change size in app mode.
	•	Removing one control must not break layout of other controls (no cross-dependencies).

⸻

## 2) Mode: document

2.1 Core behavior
document is content-first:
	•	vertical flow
	•	text wraps
	•	scrolling is enabled via a property, not a special container type
	•	Document containers (Page, Column, Row, Box) do not support x, y, width, height or anchors.
	•	They always fill the content rectangle of their parent app container (Panel or Window).
	•	If there is no app-mode parent (Page is root), the viewport is the screen/window client rect.
	• 	Layout behavior per container:
        • Column: vertical stacking (top-to-bottom).
        • Row: horizontal stacking (left-to-right).
        • Box: overlay layout; children share the same origin; z-order equals declaration order.

2.2 Scrolling properties (current decisions)

Scrollbar dimensions:
	•	scrollBarWidth: <px>  // used for vertical scrollbar
	•	scrollBarHeight: <px> // used for horizontal scrollbar

Scrollbar placement:
	•	scrollBarPosition: right|left|top|bottom

Scrollbar visibility:
	•	scrollBarVisible: true|false
	•	scrollBarVisibleOnScroll: true|false
	•	scrollBarFadeOutTime: <ms>

Semantics rules (must be explicit)
	•	scrollable does not change layout rules; it only enables scrolling when content exceeds viewport.
	•	scrollBarVisibleOnScroll: true implies the bar is not permanently visible.
	•	scrollBarFadeOutTime applies after the last scroll interaction.

2.3 Non-goals (for v1)
	•	No responsive breakpoint system
	•	No flexbox-like rules
	•	No “weight”
	•	No cross-element constraints

2.4 Acceptance criteria
	•	Document content flows vertically and wraps.
	•	Scrollbar visibility behaves as configured (always visible vs only on scroll + fadeout).
	•	Scrollbar size and position are applied correctly.

2.5 Widgets
	•	Document mode containers (Page, Column, Row, Box) have layoutMode = document.
	•	Page ist the Root on Android, where Window is the root on desktop.

⸻

## 3) Implementation Plan (Godot)

3.1 SML Parsing
	•	Extend the SML parser to recognize:
	•	app properties: x/y/width/height, anchors
	•	document properties: scrollable, scrollbar properties

3.2 Layout Engine
	•	Implement app mode layout resolver:
	•	calculate margins at first layout
	•	apply anchor rules on resize
	•	Implement document flow resolver:
	•	stack children vertically
	•	apply wrapping for text
	•   treat Page as implicit Column (vertical stacking)

3.3 Scrolling
	•	Implement scroll behavior as a container capability:
	•	vertical scroll for overflow in height
	•	horizontal scroll if enabled/needed
	•	Implement scrollbar rendering:
	•	width/height
	•	position
	•	visibility modes + fadeout

3.4 Logging / Debug (mandatory)

Add optional debug overlay/logs:
	•	current mode
	•	container size vs content size
	•	scrollbar state (visible/hidden, fade timer)

⸻

## 4) Deliverables
	•	Spec doc (as markdown in repo): docs/layout_modes_v1.md
	•	Implementation in code
	•	A small sample SML scene for each mode:
	•	samples/app_layout_demo.sml
	•	samples/document_demo.sml

---

## Sample
This sample shows how we can mix both modes.

```qml
Window {
	title: "Window in app layout"
    minSize: 640, 480
	size: 1024, 768


    // Left app panel (fixed width)
    Panel {
        x: 8
        y: 8
        width: 320
		height: 752
        anchors: top | bottom | left
    }

    // Right content area (fills rest)
    Panel {
        x: 328
        y: 8
		height: 752
		width: 688
        anchors: right | top | bottom

        Column {
            scrollable: true
            scrollBarWidth: 8
            scrollBarVisibleOnScroll: true
            scrollBarFadeOutTime: 300 // in ms

            Markdown { 
				text: "
# Header
## Subheader				
Lorem Ipsum Dolor"
			}
        }
	}
}
```

```qml
Page {
	padding: 8, 8, 8, 8

	Markdown {src: "docs/doku.md"}
}
```