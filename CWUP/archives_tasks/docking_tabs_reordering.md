# Docking Phase 4: Tabs and Reordering

## Goal

Enable productive tab workflows across dock areas.

## Scope

- Support tab reordering within the same dock group.
- Support moving tabs from one dock group to another.
- Support merging a dragged tab into an existing dock tab group.
- Keep panel state and active tab state consistent after moves.

## Non-Goals

- No polished drag visuals yet (icons/highlights handled in Phase 5).
- No persistence yet (handled in Phase 6).

## Acceptance Criteria

1. Tabs can be reordered in-place.
2. Tabs can be moved between dock groups.
3. Tab group merge behavior is deterministic and stable.
