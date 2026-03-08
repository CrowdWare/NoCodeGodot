# Native 3D Transform And Gizmo Parity

## Goal
Bring arrange/pose transform workflows to practical parity with legacy C#
gizmos (`move`, `rotate`, `scale`) in native runtime.

## Scope
- Gizmo interaction loop for selected prop/character.
- Pose mode bone rotation flow with visible axis affordance.
- Transform-space behavior (`world` / `local`) consistency.

## Implementation Steps
- Wire active selection to gizmo lifecycle (spawn/update/hide).
- Implement drag math per gizmo type and clamp rules where needed.
- Apply transforms to scene items and emit SMS events:
  - `objectMoved(index, pos)`
  - `poseChanged(boneName)`
- Add visual pass for axis colors, hit areas, and depth readability.

## Acceptance Criteria
- Move/rotate/scale gizmos work in arrange mode for selected objects.
- Bone rotation works in pose mode without selection loss.
- Transform changes are reflected in inspector fields without manual refresh.
- No critical usability regressions versus current legacy flow.

## Risks
- Interaction ambiguity between camera drag and gizmo drag.
- Numeric drift from repeated incremental transform updates.

