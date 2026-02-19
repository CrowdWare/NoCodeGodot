# Replace TabContainer With Explicit TabBar + Content Architecture

## Goal

Remove usage of TabContainer inside DockPanels and replace it with an explicit TabBar + ContentContainer architecture.

This enables:
	•	Custom Dock Header (MenuButton, actions, layout menu)
	•	Full control over tab switching
	•	Future support for docking, floating, layout persistence
	•	Deterministic behavior (no hidden engine logic)

⸻

## Motivation

TabContainer encapsulates:
	•	Internal TabBar
	•	Internal content switching
	•	Internal layout logic

This prevents:
	•	Injecting controls into the header
	•	Fine-grained docking control
	•	Layout serialization
	•	Floating panel reattachment logic

For a docking system, we need explicit control.

⸻

## Target Architecture

Replace:
```
TabContainer
 ├─ PanelA
 ├─ PanelB
 └─ PanelC
```
With:
```
DockPanel (VBoxContainer)
 ├─ Header (HBoxContainer)
 │    ├─ TabBar (Expand | Fill)
 │    └─ MenuButton (⋮)
 └─ ContentContainer (Control)
       ├─ PanelA
       ├─ PanelB
       └─ PanelC
```

⸻

## Implementation Requirements

### 1. Remove TabContainer
	•	Replace all instances of TabContainer in DockPanels.

### 2. Introduce TabBar
	•	Create TabBar in header.
	•	Add one tab per content panel.
	•	Tab titles must match panel names.

### 3. Manual Tab Switching

Connect:
```
tabBar.tab_changed
```
Handler must:
	•	Hide all content panels
	•	Show selected panel
	•	Sync internal state

⸻

## Switching Logic (Reference Pattern)
```csharp
private void OnTabChanged(long index)
{
    string tabTitle = _tabBar.GetTabTitle((int)index);

    foreach (Control child in _contentContainer.GetChildren())
        child.Visible = (child.Name == tabTitle);
}
```
Must also be executed once during _Ready() for initial sync.

⸻
## Now we can also remove the most right extra gap.
This gap was only there to host the MenuButton.

⸻

## Acceptance Criteria
	•	Clicking a tab changes visible content.
	•	MenuButton is aligned right inside header.
	•	No TabContainer remains in DockPanel.
	•	Layout visually matches previous behavior.
	•	No regression in existing panel functionality.

⸻

## Why This Refactor Is Critical

Future features depend on:
	•	Explicit tab switching
	•	Explicit visibility control
	•	Header-level action injection
	•	Layout state tracking

Without this refactor, Docking will become fragile.