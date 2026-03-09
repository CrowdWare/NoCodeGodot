# Native Timeline Playback Parity

## Goal
Complete timeline behavior parity for keyframe editing, scrubbing, and playback
in `ForgeTimelineControl`.

## Scope
- Keyframe add/remove and per-frame pose retrieval.
- Scrubber/frame-change behavior and event emission.
- Playback start/stop path and interpolation baseline.

## Progress
- [x] `keyframeAdded(frame, boneName)` emission now mirrors stored pose keys instead of wildcard-only payload.
- [x] `setVisibleCharacterId(...)` now triggers redraw immediately so visible-track switching is reflected without extra interaction.
- [x] Native timeline now renders per-bone track rows with left bone-name list and per-row keyframe markers (no longer single-line keyframe row).
- [x] Native playback/scrubbing now applies merged pose data for all keyed characters (complete scene animation) directly in C++ timeline control.
- [ ] Timeline scrollbar + full ruler/label polish still differs from C# widget.

## Implementation Steps
- Finalize keyframe storage contract per character and frame.
- Ensure scrub updates current frame and drives pose load consistently.
- Implement playback loop with deterministic frame stepping.
- Emit/consume events:
  - `frameChanged(frame)`
  - `keyframeAdded(frame, boneName)`
  - `keyframeRemoved(frame)`
  - `playbackStarted()`, `playbackStopped()`

## Acceptance Criteria
- Adding/removing keyframes updates timeline and scene state correctly.
- Scrubbing updates pose immediately and repeatably.
- Playback runs and stops cleanly without stuck state.
- Timeline state survives project load/save roundtrip.

## Risks
- Event storms causing SMS/UI feedback loops.
- Interpolation mismatch with legacy data expectations.
