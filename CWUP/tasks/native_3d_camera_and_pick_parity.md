# Native 3D Camera And Pick Parity

## Goal
Reach legacy-equivalent camera interaction and object/bone picking behavior in
`ForgePosingEditorControl` on `ForgeRunner.Native`.

## Scope
- Orbit / pan / zoom interaction in the `SubViewport`.
- Stable hit-testing for scene props and skeleton bones.
- Deterministic selection state updates for viewport + SMS events.

## Implementation Steps
- Add native input handling for mouse drag + wheel with mode-aware behavior.
- Implement raycast picking from active camera into scene content.
- Normalize selection contract:
  - prop selection emits `objectSelected(index)`
  - bone selection emits `boneSelected(name)`
- Ensure selection changes refresh inspector/bone tree immediately.

## Acceptance Criteria
- Camera controls are responsive and predictable (no jumpy resets).
- Clicking visible props selects the expected prop index.
- Clicking rig joints selects the expected bone name.
- No runtime warnings/errors during repeated camera + pick interactions.

## Risks
- Selection drift if node/index mapping is not centralized.
- Input conflicts with gizmo drag operations.

