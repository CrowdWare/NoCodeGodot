# Native Poser Project Roundtrip

## Goal
Guarantee reliable `loadProject`/`saveProject` roundtrip behavior for Poser
scene files in native runtime.

## Scope
- Parse + materialize scene characters/props from `.scene`.
- Persist transforms and scene entries back to `.scene`.
- Preserve valid existing content without destructive rewrites.

## Implementation Steps
- Define canonical mapping for scene nodes to runtime items (id/name/src/transform).
- Implement save path that writes stable, deterministic output.
- Add explicit handling for unresolved assets with warnings (no hard crash).
- Validate with demo project files and edited sessions.

## Acceptance Criteria
- Loading `PoserDemoProjects/demo.scene` recreates expected scene items.
- Saving and reloading preserves item count, ids, src paths, and transforms.
- Unknown/missing assets do not crash runtime; warnings remain actionable.
- No unintended path rewrites across schemes (`res:/`, `appRes:/`, absolute).

## Risks
- Data loss if serializer omits fields currently tolerated by legacy files.
- Path normalization regressions across local project structures.

