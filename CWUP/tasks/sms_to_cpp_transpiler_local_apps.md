# SMS -> C++ Transpiler For Local Apps

## Goal
Add a transpiler pipeline that converts SMS scripts to C++ for apps that run locally and are not deployed over HTTP.
Define the architecture as a two-step pipeline: `SMS -> IL -> target backend`.

## Why
- Maximize runtime performance for local/offline distributions.
- Remove interpreter overhead in production-local builds.
- Keep authoring model unchanged: users continue writing SMS only.
- Enable future multi-target compilation without changing SMS source authoring.

## Scope
- Define a stable intermediate language (IL) contract produced from SMS AST.
- Implement deterministic `SMS -> IL` lowering for supported language subset.
- Implement deterministic `IL -> C++` backend for local/offline app builds.
- Integrate transpiler into local build flow only.
- Keep standard interpreted/runtime SMS path for HTTP/deployed scenarios.
- Reserve and document `IL -> JVM` backend path for future Kotlin/Ktor application-server target.

## Non-Goals
- No remote HTTP deployment transpilation in this task.
- No JIT backend in this task.
- No changes to user-facing authoring language beyond supported transpiler subset constraints.

## Deliverables
- Transpiler module + CLI/build entry point with explicit two-step stages.
- IL schema/spec document with versioning strategy.
- Generated C++ runtime glue for event dispatch and callable functions from IL.
- Local build profile flag (for example `--sms-transpile-local=true`) and script integration.
- Documentation for supported subset, limits, and fallback behavior.
- Architecture note for future `IL -> JVM (Kotlin/Ktor)` backend for SMS-based application servers.

## Acceptance Criteria
- Local app build can run with transpiled SMS (`SMS -> IL -> C++`) and without SMS interpreter in hot path.
- Generated C++ output is deterministic for identical SMS input.
- Generated IL output is deterministic for identical SMS input.
- Event handlers and core control-flow constructs execute behavior-parity for supported subset.
- Unsupported constructs fail with explicit compile-time diagnostics and actionable messages.
