# SMS Language Spec 2026 (2b) + Native Implementation

## Goal
Define and implement the next SMS language spec (Kotlin-friendly feel, AI-writable syntax), then apply it consistently to managed and native runtimes.

## Why
- Avoid native rework by freezing essential language decisions first.
- Ensure reusable SMS modules inside the same stack.
- Keep syntax approachable for humans and multiple AI coding models.

## Scope
### Spec work
- Write `docs/SMS/SPEC_2026.md` with grammar + semantics for:
  - `import`
  - optional typing surface needed for transpiler path
  - named arguments (including mixed positional/named rules)
  - collection loop syntax: `for(item in list)`
  - async model rules (`await`, scheduler/thread model, scope guarantees)
- Add machine-friendly grammar artifact (`.ebnf` or equivalent).

### Runtime work
- Implement the approved spec delta in:
  - managed `SMSCore`
  - native `SMSCore.Native`
- Keep feature flags if needed during transition.

## Non-Goals
- No full SMS->C++ compiler in this task.
- No LLVM backend in this task.

## Deliverables
- New spec document + grammar file.
- Parser/interpreter updates in managed + native cores.
- Conformance tests for every new syntax/semantic rule.
- Updated perf baseline including new constructs where relevant.

## Acceptance Criteria
- Spec is explicit enough to generate parser tests from grammar examples.
- Managed and native runtimes pass the same conformance suite for new features.
- No regressions on existing SMS scripts (or documented/approved breaking changes).
- AI-oriented syntax examples are included and executable.

## Dependencies
- Depends on:
  - `CWUP/tasks/smlcore_cpp_native_runtime.md`
  - `CWUP/tasks/sms_cpp_native_runtime.md`

