# Docking Phase 3: Floating Windows

## Goal

Support true undocking into standalone windows, including multi-monitor workflows.

## Scope

- Convert a docked panel into its own Godot `Window` when set to `floating`.
- Ensure floating window supports native controls:
  - close
  - maximize
  - minimize
- Track `lastDockedSlot` per panel.
- On floating window close, re-dock panel back to `lastDockedSlot`.
- Ensure focus/ownership is stable when moving between docked and floating states.

## Non-Goals

- No tab drag/reorder yet.
- No visual drag/drop placement hints yet.

## Acceptance Criteria

1. Panels can be floated into separate windows.
2. Closing a floating window re-docks to the previous dock slot.
3. No panel is lost during dock/float transitions.
