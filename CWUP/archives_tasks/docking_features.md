# Implement Missing Docking System Features

## Context

The current Docking system is functional but lacks several essential layout management and panel interaction features.

This task defines the missing capabilities required to make the docking system production-ready.

The implementation must integrate with:
	•	SML (structure & menu definitions)
	•	SMS (event handling logic)
	•	The existing DockHost / DockPanel system
	•	The sample application: samples/docking_demo.sml

⸻

## 1. Layout Persistence

### 1.1 Save Layout As
	•	Provide a function to save the current docking layout under a user-defined name.
	•	Store:
	•	Panel positions (left, right, top, bottom, center, farLeft, farRight)
	•	Size ratios
	•	Visibility state
	•	Floating state
	•	Layouts must be serializable and reloadable.

### 1.2 Load Layout
	•	Load a previously saved layout.
	•	Restore:
	•	Dock positions
	•	Size ratios
	•	Visibility
	•	Floating panels

### 1.3 Reset Layout
	•	Restore default layout defined in SML.
	•	Must discard runtime modifications.

### 1.4 Auto Save / Auto Restore
	•	On application close:
	•	Automatically save current layout as “last session”.
	•	On application startup:
	•	If a “last session” layout exists, load it automatically.

⸻

## 2. Docking Selection Dialog

Implement a visual dialog for selecting docking positions.

### Requirements
	•	Grid layout:
	•	2 rows
	•	5 mini panels per row
	•	Center panel must be visually wider
	•	Below the grid:
	•	Button: “Floating”
	•	Button: “Closed”

### Behavior

When selecting:
	•	A grid position → dock panel to selected slot
	•	Floating → undock panel and make it free-floating
	•	Closed → hide panel

The dialog must visually represent the layout clearly.

⸻

## 3. Panel Visibility Integration

### 3.1 Close Panel
	•	Closing a panel must:
	•	Update internal visibility state
	•	Update corresponding menu item checkmark

### 3.2 Menu Sync

In samples/docking_demo.sml:
	•	The “View” menu contains entries for panels
	•	Each menu item has a checked flag
	•	Toggling the menu entry must:
	•	Show/hide the corresponding panel
	•	Stay synchronized with runtime state

⸻

## 4. Resize Behavior

### 4.1 Horizontal Resize Handles

Add resize handles for:
	•	Top panels (vertical size change)
	•	Bottom panels (vertical size change)

Resizing must:
	•	Update layout ratios
	•	Be persistent when layout is saved

⸻

## 5. Floating Panels

### 5.1 Undock
	•	A docked panel can be undocked into a free-floating window.

### 5.2 Redock on Close
	•	If a floating panel window is closed:
	•	It must return to its last docked position
	•	Not be destroyed permanently

⸻

## 6. SMS API Requirements

The following usage pattern must work:
```sms
var mainDockPanel = ui.getObject(mainDockPanel)
mainDockHost.hidePanel(left)
mainDockPanel.showPanel(farRight)
```
Required API Functions

Ensure existence (or implement):
	•	hidePanel(position)
	•	showPanel(position)
	•	dockPanel(panel, position)
	•	undockPanel(panel)
	•	isPanelVisible(panel)
	•	saveLayout(name)
	•	loadLayout(name)
	•	resetLayout()

These functions must be callable from SMS.

⸻

## 7. Sample Application Compatibility

File:
```
samples/docking_demo.sml
```
Must demonstrate:
	•	Menu-based panel visibility toggle
	•	Dock/undock
	•	Floating
	•	Layout save/load
	•	Auto restore on restart

⸻

## Deliverables
	1.	Full docking persistence system
	2.	Visual docking selection dialog
	3.	Floating panel lifecycle
	4.	Resize handles
	5.	SMS-accessible API
	6.	Updated docking_demo.sml showing all features