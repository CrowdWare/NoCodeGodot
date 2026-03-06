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
