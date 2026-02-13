# Implement explicit window scaling modes (layout vs fixed) with constant font behavior

## Context

The application must support two explicitly selectable window scaling modes.
Scaling behavior must never be implicit or inferred from window size.

This is required to support:
	•	App / Editor UI (Compose-like behavior)
	•	Fixed design for games

Configuration (source of truth)

The scaling mode is defined in SML:

```qml
Window {
    scaling: layout   // or fixed
    designSize: 1152, 648 // required if scaling == fixed
}
```

scaling: layout and no designSize is the default

--- 

## Scaling Modes

### ode A: layout (default – App / Editor behavior)

Use this mode for:
	•	Editors
	•	Forms
	•	Text-heavy UIs
	•	Compose-like layouts

### Behavior
	•	Window resize / fullscreen → layout recomputes
	•	Containers reflow
	•	Font size remains constant
	•	No automatic canvas or font scaling
	•	Accessibility-friendly

### Requirements
	•	No global scaling based on window size
	•	No font scaling on resize
	•	Root Control resizes to actual window size
	•	Layout changes only via containers/anchors

---

## Mode B: fixed (simulation / board / game screen)

Use this mode for:
	•	Games
	•	Board / table simulations
	•	Any screen where relative proportions must remain visually stable

### Design intent
The UI is designed for a fixed logical resolution (e.g. 1152×648).
On resize, the result is scaled, not re-laid out.

### Behavior
	•	UI is rendered at designSize
	•	Window resize does NOT trigger layout recomputation
	•	The rendered result is scaled to the new window size
	•	Fonts and graphics scale proportionally
	•	No partial reflow

### Example use case
In games its usefull that every thing scales.

---

## Implementation Requirements (Godot 4.6)

### General
	•	Scaling mode must be selected explicitly via SML
	•	No implicit behavior based on window size
	•	Switching modes must be deterministic and debuggable

---

## Layout mode implementation
	•	Disable any global UI scaling
	•	Use Control + Container layout only
	•	Ensure font size does not change on resize
	•	Window resize triggers real Control.size changes
	•	No canvas-style stretch or zoom behavior

---

## Fixed mode implementation

Implement one of the following (A preferred):

A) SubViewport / Render-to-Texture (preferred)
	•	Render UI at designSize
	•	Display the result scaled to the actual window size
	•	Preserve aspect ratio or expand as defined by the design

B) Snapshot / Screenshot scaling (acceptable fallback)
	•	Render once at designSize
	•	On resize:
	•	Capture frame
	•	Scale image to new window size
	•	No layout recomputation

---

## Non-negotiable constraints
	1.	Do NOT mark this task as done unless both modes are implemented.
	2.	Behavior must be observable at runtime (not code inspection).
	3.	Font size must:
	•	Stay constant in layout
	•	Scale proportionally only in fixed
	4.	No mixed or hybrid scaling behavior.

---

## Proof requirements (mandatory)

Include runtime verification for both modes:

### Layout mode proof
	•	Resize window larger/smaller
	•	Log:
	•	Root Control.size
	•	Effective font size
	•	Font size must remain unchanged

### Fixed mode proof
	•	Resize window larger/smaller
	•	Log:
	•	Render resolution (designSize)
	•	Window size
	•	Layout must not recompute
	•	Visual scaling must be proportional

---

## Acceptance Criteria
	•	layout behaves like a Compose-style UI
	•	fixed behaves like a scaled simulation screen
	•	Behavior matches the declared SML configuration
	•	No automatic or hidden scaling logic