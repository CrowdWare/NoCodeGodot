# ForgeRunner Native-Only C++ Migration

## Goal
Migrate ForgeRunner to a fully native C++ runtime and remove all C# from the project.

## Why
- Eliminate mixed-runtime complexity.
- Remove bridge overhead and failure modes.
- Align architecture with native-first SML/SMS direction.

## Scope
- Reimplement ForgeRunner runtime and app entry path in C++.
- Remove all C# project/runtime dependencies from ForgeRunner deliverables.
- Remove all C# <-> C++ bridge calls in both directions.
- Keep feature parity for current default/designer/poser startup paths.

## Non-Goals
- No temporary hybrid bridge as end state.
- No fallback to managed parser/interpreter/runtime in production path.

## Deliverables
- Native C++ ForgeRunner runtime path for startup, UI loading, SML parse, SMS execution, and event dispatch.
- Build/export scripts updated to native-only flow.
- C# ForgeRunner sources removed (or archived outside production build path).
- Documentation updated to reflect native-only architecture.

## Acceptance Criteria
- ForgeRunner builds and runs without C# runtime dependencies.
- No runtime calls from C# to C++ and no calls from C++ to C# remain.
- Default, Designer, and Poser flows start and execute in native-only mode.
- Existing critical smoke scenarios pass (startup, UI load, SMS event handling).

## Implementation Progress
- [x] Native host scaffold created (`ForgeRunner.Native`, GDExtension bootstrap class, build docs).
- [x] Runner scripts extended with dedicated native-host build mode (`./run.sh build-native-host`) and optional inclusion in `./run.sh build` via `--build-native-host=true`.
- [x] Opt-in runtime probe path wired (`--native-host-probe=true`, `./run.sh native-host-probe`) without changing default C# startup path.
- [x] Native-only SMS dispatch logging clarified (removed managed-fallback wording in strict mode).
- [x] Startup download progress routing aligned by root intent: `SplashScreen` -> embedded `ProgressBar`, `Window` -> overlay, `Terminal` -> console/log progress.
- [x] `Terminal` root wired in schema + node factory (no unknown-node/factory warning; CLI progress routing now active for terminal-root docs demo).
- [ ] Integrate native host as active startup path in `ForgeRunner` (scene/project cutover gate).
- [ ] Port core runtime services from C# host to native host with feature parity.
- [ ] Remove managed runtime dependencies from production build path.
