# Native Timeline Playback Parity

## Goal
Complete timeline behavior parity for keyframe editing, scrubbing, and playback
in `ForgeTimelineControl`.

## Scope
- Keyframe add/remove and per-frame pose retrieval.
- Scrubber/frame-change behavior and event emission.
- Playback start/stop path and interpolation baseline.

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

