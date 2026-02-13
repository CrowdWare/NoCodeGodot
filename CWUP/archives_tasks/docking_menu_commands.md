# Docking Phase 2: Menu Commands (No Drag & Drop)

## Goal

Make docking fully usable via panel menu commands before implementing drag behavior.

## Scope

- Add a 3-dots dock header menu per panel.
- Expose all target commands:
  - `left`, `far-left`, `right`, `far-right`
  - `bottom-left`, `bottom-far-left`, `bottom-right`, `bottom-far-right`
  - `floating`, `closed`
- Implement command-based move logic between slots.
- Implement close/open behavior:
  - Close hides panel but keeps state.
  - Closed panels can be reopened via menu/list.

## Non-Goals

- No drag/drop interaction yet.
- No floating `Window` behavior yet (only state placeholder).

## Acceptance Criteria

1. Every panel exposes the docking menu.
2. Menu command execution moves panel to the selected area.
3. Closed panels can be reopened without data loss.
