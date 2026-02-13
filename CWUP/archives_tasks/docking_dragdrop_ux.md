# Docking Phase 5: Drag & Drop UX Polish

## Goal

Provide clear, Godot-like drag and drop feedback for docking operations.

## Scope

- Add drop-zone highlighting for valid docking targets.
- Show valid/invalid drag feedback:
  - valid placement icon (hand)
  - invalid placement icon (stop)
- Add drag preview/ghost behavior during panel or tab drag.
- Handle edge cases:
  - too-small targets
  - quick cursor movement across multiple targets
  - target cancellation and rollback

## Non-Goals

- No layout persistence in this phase.

## Acceptance Criteria

1. Drag feedback is always visible and unambiguous.
2. Valid and invalid targets are clearly distinguished.
3. Dropping outside valid targets never corrupts layout state.
