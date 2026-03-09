# Native 3D Transform And Gizmo Parity

## Goal
Bring arrange/pose transform workflows to practical parity with legacy C#
gizmos (`move`, `rotate`, `scale`) in native runtime.

## Scope
- Gizmo interaction loop for selected prop/character.
- Pose mode bone rotation flow with visible axis affordance.
- Transform-space behavior (`world` / `local`) consistency.

## Progress
- [x] Arrange-mode drag transform loop ported in native `ForgePosingEditorControl`:
  - move / scale / rotate drag math aligned to C# formulas (screen-axis projection + depth scaling, quaternion rotation accumulation).
  - world/local transform-space axis resolution wired for arrange transforms.
  - live `objectMoved(index, pos)` emission during drag for prop and character (`index=-1` for character parity).
- [x] Pose-mode bone rotate drag path added in native:
  - per-axis virtual handle picking around selected bone.
  - quaternion world-axis rotation with constraint clamping and live `poseChanged(boneName)` emission.
- [x] Native in-viewport gizmo visuals are now shown for move/scale/rotate with axis color coding and active-axis highlight, driven by current selection and mode.
- [ ] Dedicated standalone native gizmo classes (`ForgeMoveGizmo3D` / `ForgeScaleGizmo3D` / `ForgeRotationGizmo3D`) are still pending (current implementation is integrated in `ForgePosingEditorControl` for parity-first unblock).

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
