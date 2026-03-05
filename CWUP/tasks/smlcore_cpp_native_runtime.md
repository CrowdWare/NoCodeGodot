# SMLCore in C++ (Standalone Native Library)

## Goal
Build a native C++ implementation of `SMLCore` as a standalone library, independent from ForgeRunner, with behavior parity to the managed parser.

## Why
- Measured performance indicates a large parse-speed opportunity.
- Native core is the base for future AOT/native pipeline.
- Clear separation: parser/runtime engine independent from UI host.

## Scope
- Create `SMLCore.Native` (C++ lib) with:
  - lexer
  - parser
  - SML AST model
  - stable C ABI bridge
- Keep managed `SMLCore` as reference implementation during migration.
- Add perf harness integration (`perf/SmsPerfLab`) for managed vs native parser numbers.

## Non-Goals
- No direct Godot integration in this phase.
- No language extensions in this task.

## Deliverables
- Native C++ SML parser library (`.dylib/.so/.dll`).
- C ABI API for parse entry/result/error.
- Managed interop layer used by perf lab.
- Conformance tests against existing SML fixtures.

## Acceptance Criteria
- All existing SML parser tests pass against managed implementation.
- Native parser passes equivalent fixture set (same pass/fail outcomes).
- Perf report includes managed/native parse comparison on same input set.
- No regression in current SML grammar behavior.

## Dependencies
- None (first task in chain).

