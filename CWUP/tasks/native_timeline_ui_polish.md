# Native Timeline UI Polish

## Goal
Bring the native timeline UX/visual behavior to practical parity with legacy usage flow.

## Scope
- Timeline readability and interaction polish in `ForgeTimelineControl`.
- Focused frame/keyframe visibility improvements.
- Keyboard-first keyframe edit ergonomics.

## Implementation Steps
- Ensure horizontal scrollbar is consistently visible and usable when timeline content exceeds viewport.
- Add focused-frame keyframe emphasis (current frame marker/keyframe highlight).
- Bind Backspace/Delete to remove keyframe at the current playhead frame.
- Remove legacy right-click delete shortcut from timeline canvas to avoid accidental deletion.

## Acceptance Criteria
- Users can clearly identify keyframes at the focused/current frame.
- Pressing Backspace/Delete removes keyframe at the focused frame deterministically.
- Scrollbar is visible whenever horizontal overflow exists and supports frame navigation context.

## Risks
- Keyboard event routing conflicts when focus is not on timeline.
- Visual clutter if focus highlight and playhead styles are not balanced.
