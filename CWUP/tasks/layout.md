# Define and Implement Layout Modes app and document (SML → Godot)

## Goal

Create a deterministic layout system with two modes:
	•	app (absolute positioning + anchors, WinForms-like)
	•	document (flow + scrolling, content-first)

This task captures the current decisions and turns them into a spec + implementation plan.

⸻

## 1) Mode: app

1.1 Supported properties (complete)

Each control supports:
	•	Geometry:
	•	left, top, width, height
	•	Anchors to parent edges:
	•	anchorLeft, anchorRight, anchorTop, anchorBottom

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
	•	minWindowSize at Window level (prevents shrinking below usability)

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
	•	vertical flow (Window behaves like an implicit Column)
	•	text wraps
	•	scrolling is enabled via a property, not a special container type

Window-level defaults in document
	•	scrollable: true (default)
	•	flow layout enabled by default (vertical stacking)

2.2 Scrolling properties (current decisions)

Any container (including Window) can be scrollable:
	•	scrollable: true|false

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
	•	Window/container scrolls when content exceeds size.
	•	Scrollbar visibility behaves as configured (always visible vs only on scroll + fadeout).
	•	Scrollbar size and position are applied correctly.

⸻

## 3) Implementation Plan (Godot)

3.1 SML Parsing
	•	Extend the SML parser to recognize:
	•	Window { mode: app|document }
	•	app properties: left/top/width/height, anchors
	•	document properties: scrollable, scrollbar properties

3.2 Layout Engine
	•	Implement app mode layout resolver:
	•	calculate margins at first layout
	•	apply anchor rules on resize
	•	Implement document flow resolver:
	•	treat Window as implicit Column
	•	stack children vertically
	•	apply wrapping for text

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