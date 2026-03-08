# Native Release Cleanup: No JSON Fallback In Hot Path

## Goal
For local-only production builds (especially with SMS -> C++ transpilation), remove JSON-based UI bridge fallbacks from runtime hot paths.

## Why
- JSON parsing/stringifying in `ui.get` / `ui.set` / `ui.invoke` is flexible but not ideal for release performance.
- Transpiled local apps have stable method/property surfaces and should use typed calls.
- Release behavior should be deterministic: unsupported calls fail fast instead of silently falling back.

## Scope
- Introduce typed/native bridge paths for known high-frequency calls.
- Keep JSON fallback available only for development/debug compatibility.
- Gate fallback behavior by explicit build/runtime mode (`release_local`, `dev`, or equivalent).
- Add call telemetry during development runs to identify remaining fallback usage.
- Generate typed bridge stubs from `tools/specs` (source-of-truth) where applicable.

## Non-Goals
- Do not remove JSON fallback for remote/deployed HTTP workflows immediately.
- Do not change SMS language semantics.
- Do not block prototype iteration in dev mode.

## Deliverables
- Build/runtime switch to disable JSON fallback in release-local mode.
- Typed bridge coverage for all required `ForgePoser` and default local-app calls.
- Telemetry/report output listing unresolved fallback calls during dev/test runs.
- Updated specs/codegen flow documenting how typed bridge entries are maintained.

## Acceptance Criteria
- Local release run path executes without JSON fallback in hot path.
- If an unmapped bridge call occurs in release-local mode, runtime logs clear actionable error and aborts the call.
- Dev mode still supports JSON fallback for iteration.
- `tools/specs` + generated bridge stubs are in sync for supported typed calls.
