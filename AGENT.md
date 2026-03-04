# AGENT.md

Repository guidance for coding agents working in Forge.

## Scope
- Applies to the entire repository unless overridden by a deeper `AGENT.md`.

## Architecture Baseline
- Stack: Godot 4.6 + C# (`CWUP/ARCHITECTURE.md`).
- `SML` = structure/UI declaration.
- `SMS` = event scripting and glue logic.
- C# runtime = execution and platform integration.
- Keep SML readable/declarative and avoid introducing hidden runtime magic.
- For editor workflows, treat SML source text as source-of-truth.

## Process (CWUP)
- Follow CWUP direction from `CWUP/README.md`.
- Architecture-first decisions.
- Risk-driven implementation and early validation.
- Crowd feedback is input, not architecture authority.
- Treat `CWUP/tasks/*.md` as active task context.
- Treat `CWUP/archives_tasks/*.md` as historical context unless a task references them.

## Implementation Rules
- Prefer small, focused changes.
- Keep behavior unchanged unless the task explicitly requests behavior changes.
- Do not introduce new dependencies unless needed.
- Preserve platform parity expectations (Windows/macOS/Linux; Android for runner where relevant).
- When touching concurrency-sensitive runtime logic, keep scene graph mutations on main thread.
- On parser/runtime tolerance: malformed/unknown SML should fail gracefully with warnings when applicable, not hard-crash.
- Respect security boundaries in file/path features (no traversal/symlink escape bypasses, no implicit bypass paths).

## Specs And Docs Sync (Required)
- `tools/specs/*.gd` are source-of-truth inputs for generated docs/schema metadata.
- If you add/change SML/SMS surface area (properties, actions, events, resources, overrides), update relevant spec files in `tools/specs/`.
- If you change runtime property mapping/theme override behavior, update runtime code (for example `NodePropertyMapper`-related code) and `tools/specs/runtime_overrides.gd`.
- Regenerate docs after spec/runtime-surface changes with `./run.sh docs`.
- If theme token behavior in `ForgeRunner/theme.sml` changes, regenerate with `./run.sh theme`.

## Useful Commands
- Build runner: `./run.sh build`
- Generate docs: `./run.sh docs`
- Generate theme resource: `./run.sh theme`
- Run default UI: `./run.sh default`
- Run poser UI: `./run.sh poser`
- Run full tests: `./run.sh test`

## Current Delivery Status (2026-03-04)
- `ForgeAiLib` exists and is integrated into `ForgeRunner` SMS runtime via global `ai` object.
- ForgePoser proof path is wired:
  - `menuExportCurrentFrame` -> capture + `ai.stylizeImage(...)`
  - `menuExportStylizedClip` -> frame sequence export -> ffmpeg encode -> `ai.stylizeVideo(...)`
- Frame range export was switched to async frame-by-frame capture so timeline pose updates are visible before each screenshot.
- `tools/specs/` and runtime surface were updated for new AI and poser APIs; regenerate docs with `./run.sh docs` after interface changes.
- Known open point: xAI video endpoint/protocol and model availability can change; keep `ForgeAiLib` request/response mapping aligned with current xAI docs before production rollout.
- Known open point: `RotationGizmo3D` guide circle radii are tuned, but the colored rotation arcs still need a final geometry/style pass to match the intended Blockbench-like look.

## Code Style
- Match existing project conventions.
- Use clear names over clever abstractions.
- Keep comments concise and only where they add value.
- Keep naming and APIs explicit/deterministic for tool and AI generation workflows.

## Safety
- Avoid destructive actions (mass deletes, hard resets) unless explicitly requested.
- If unexpected unrelated changes are detected, stop and confirm before proceeding.

## Validation
- Run the smallest relevant validation for touched areas (build/tests/docs generation/theme generation as applicable).
- Report what was run and any remaining risks.
