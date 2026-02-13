# Docking Phase 6: Layout Persistence, Reset and Hardening

## Goal

Make docking layout durable, resettable and release-ready.

## Scope

- Implement `SaveLayout()` to persist full `DockLayoutState`.
- Implement `LoadLayout()` to restore full state on startup or command.
- Implement `ResetDefaultLayout()` to restore initial default layout.
- Persist layout as JSON including a `layoutVersion` field.
- Add validation and fallback behavior for invalid or outdated saved layouts.
- Add stabilization checks for:
  - close/reopen cycle
  - floating → redock transitions
  - splitter sizes
  - mixed tab groups and slot occupancy

## Non-Goals

- No new docking interaction features.

## Acceptance Criteria

1. Layout can be saved and restored deterministically.
2. Default layout reset works from any runtime state.
3. Invalid saved layout never crashes runtime and falls back safely.
