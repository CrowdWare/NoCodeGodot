# Pre-Release Notes

## Highlights
- ForgePoser is now available as a substantial real-world Forge app.
- Save flow in ForgePoser was consolidated (`Save`, `Save As`, shortcuts) for consistent behavior.
- Logging output was cleaned up: operational noise moved to debug-level, user-facing logs remain concise.
- Docking container clipping was fixed at host/container level to prevent panel overflow artifacts.

## Stability & Quality
- Improved startup and runtime log signal-to-noise ratio.
- Added CLI debug switch support (`--debug=true`) and optional Godot verbose passthrough (`--verbose`).
- Added tracking items for engine shutdown leak diagnostics.

## Known Issues
- Some Godot engine shutdown leak diagnostics may still appear on exit in verbose mode.

