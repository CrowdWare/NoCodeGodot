# Fix UI Scaling in Godot (Window Fill + HiDPI / Retina)

## Context / Problem

The current UI appears too small and does not fill the available window space. Large unused areas are visible and the content is centered in a relatively small region (see screenshot).

This is not a cosmetic issue.
It is a functional UI scaling bug and must be treated as a requirement.

## Goal

The SML-generated UI must:
	1.	Fill the available window area by default
	2.	Scale correctly on HiDPI / Retina displays (macOS)
	3.	React immediately to window resizing

## Requirements
	1.	Root-Fill by Default
	•	If no explicit width / height is defined in SML, the root UI container must fill the available space.
	•	No hardcoded pixel sizes that cause the UI to remain small.
	•	Resizing the window must resize the UI instantly.
	2.	HiDPI / Retina Correctness
	•	On Retina displays, the UI must not appear “miniature”.
	•	Font size, spacing and interaction areas must scale consistently (not only render scale).
	3.	Live Resize
	•	UI must update correctly when the window size changes (no restart required).
	4.	No Regression
	•	If SML explicitly defines sizes, those sizes must still be respected.

## Acceptance Criteria
	•	UI looks proportional and usable at:
	•	1280×720
	•	1920×1080
	•	2560×1440
	•	macOS Retina displays
	•	No “small centered box” unless explicitly requested by SML.
	•	Text is readable, buttons and tabs are correctly sized, mouse interaction matches visuals.

---

## Proposed Implementation (Godot)

### 1) Root Control Must Always Fill the Window

When building the UI from SML, ensure the top-level Control node fills the available area.

Godot 4 (C# / GDScript equivalent):
	•	Set root control to Full Rect
	•	Enable expand/fill size flags

Conceptually:
	•	Anchors: Full Rect
	•	Horizontal Size Flags: Expand | Fill
	•	Vertical Size Flags: Expand | Fill

If the root control is nested, all parent containers must also use Expand/Fill, otherwise the UI will remain constrained.

Recommended structure:
	•	Root: Control or PanelContainer (Full Rect)
	•	Inner layout: VBox / HBox / Tabs / custom containers

---

### 2) Project Settings: Correct Stretch Configuration

Ensure Godot is allowed to scale the UI properly.

Project Settings → Display → Window
	•	Stretch Mode: canvas_items
	•	Stretch Aspect: expand

This ensures the UI uses the full window without letterboxing and responds correctly to resizing.

---

### 3) HiDPI / Retina Scaling

Godot handles HiDPI, but the UI theme must scale correctly.

Recommended approach:
	•	Read display scale using DisplayServer.screen_get_scale()
	•	Adjust theme parameters accordingly:
	•	Default font size
	•	Margins / spacing constants (if needed)

Do not rely on render scaling alone — fonts and layout metrics must scale consistently.

Optional:
	•	Allow overriding UI scale via a user setting for accessibility.

---

### 4) Debug Verification (Required)

Add temporary debug output or overlay showing:
	•	Window size
	•	Viewport size
	•	Screen scale factor
	•	Root Control rect size
	•	Root Control anchors

This makes it immediately visible whether scaling issues come from anchors, containers or DPI handling.

---

## Implementation Checklist
	•	Root UI Control uses Full Rect
	•	All parent containers use Expand/Fill
	•	Window Stretch Mode = canvas_items
	•	Stretch Aspect = expand
	•	HiDPI scale applied to theme / fonts
	•	Resize works live without restart
	•	No regression for explicit SML sizes