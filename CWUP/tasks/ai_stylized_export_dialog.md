# AI Stylized Export Dialog (Frame + Video)

## Goal
Add one export dialog in ForgePoser that supports:
- single frame export (Img2Img)
- frame range export (Video2Video workflow)

This should enable turning greybox poses/scenes into stylized output via Grok.

## Scope
### Entry point
- Add under `File` menu in ForgePoser:
  - `Export actual frame`
  - `Export stylized...` (opens dialog for single/range/all)

### Dialog (single place for both modes)
Fields:
- `Mode`: `Single Frame` | `Range` | `All`
- `Frame` (visible for Single)
- `Frame From`, `Frame To` (visible for Range)
- `Use current frame` quick action
- `Style Image` path (optional)
- `Extra Reference Image` path (optional)
- `Prompt` (required)
- `Negative Prompt` (optional)
- `Model` (default from config)
- `Output` target path / base name

Constraints:
- Max export duration: **10 seconds**
- Timeline is 24 fps → max **240 frames** per export run
- Validate and block invalid ranges (`from > to`, empty prompt, range too long)

## Runtime behavior
### 1) Single Frame
- Render selected frame from PosingEditor viewport to image
- Call `ai.stylizeImage(...)` (Img2Img)
- Save result and show status/progress

### 2) Range / All
- Render frames to image sequence (24 fps)
- Build temporary video from sequence via ffmpeg
- Call `ai.stylizeVideo(...)` (Video2Video)
- Save styled video and show status/progress

Rule:
- If export contains exactly 1 frame → Img2Img path
- If export contains >= 2 frames (keyframes/range/all) → Video2Video path

## Project data persistence (.scene)
Store stylization defaults in project scene file:
- prompt
- negativePrompt
- styleImagePath
- extraImagePath
- preferredModel
- lastExportMode
- lastFrameFrom
- lastFrameTo
- lastOutputPath

These values should reload with project open and prefill the dialog.

## UX / status
- Status label updates for: preparing, rendering frames, encoding video, uploading, stylizing, downloading, done/failed
- Clear error messages for:
  - missing API key
  - invalid input paths
  - ffmpeg not available
  - provider/network failures

## Acceptance Criteria
- User can export one frame from current pose and receive a stylized image.
- User can export a frame range/all (<=240 frames) and receive a stylized video.
- Dialog validates range and prompt before start.
- Stylization settings persist in `.scene` and are restored on reopen.
- Existing GLB export behavior remains unchanged.

## Notes
- Keep first iteration synchronous/blocking if needed, but structure code so async/progress improvements are easy.
- Reuse `ForgeAiLib` APIs already integrated into SMS (`ai.describeImage`, `ai.stylizeImage`, `ai.stylizeVideo`).
