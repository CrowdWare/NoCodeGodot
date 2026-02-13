# Docking Phase 1: Foundation (State + Slots)

## Goal

Create the technical foundation for docking without drag & drop.

## Scope

- Define stable slot IDs for all docking areas:
  - `left`, `far-left`, `right`, `far-right`
  - `bottom-left`, `bottom-far-left`, `bottom-right`, `bottom-far-right`
  - `center`
- Introduce a runtime panel state model:
  - `id`, `title`
  - `currentSlot`
  - `lastDockedSlot`
  - `isFloating`
  - `isClosed`
- Introduce a layout state root object that can represent the full dock arrangement.
- Create fixed UI containers for all dock slots.

## Non-Goals

- No tab drag/reorder.
- No floating windows yet.
- No save/load/reset persistence yet.

## Acceptance Criteria

1. Each slot exists as a known runtime target.
2. Panels can be assigned to slots programmatically.
3. Runtime can report a consistent `DockLayoutState` at any time.
