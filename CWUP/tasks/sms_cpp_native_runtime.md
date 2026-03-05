# SMS in C++ (Native Interpreter Runtime)

## Goal
Implement SMS interpreter/runtime in C++ with parity to managed `SMSCore`, and wire it into the perf lab for measurable comparison.

## Why
- Current benchmark shows native SMS interpreter has significant speedup potential.
- Interpreter parity is the required step before optional transpiler/AOT stages.

## Scope
- Create `SMSCore.Native` (C++ lib) with:
  - tokenizer/lexer
  - parser
  - runtime/interpreter
  - value model compatible with current SMS semantics
  - stable C ABI
- Keep managed runtime as functional reference during rollout.
- Add benchmark mode in `perf/SmsPerfLab` for managed vs native SMS interpret runs.

## Non-Goals
- No SMS language expansion in this task (covered in task 2b).
- No full compiler backend in this task.

## Deliverables
- Native C++ SMS runtime library.
- C ABI entry points for compile/execute/eval and error diagnostics.
- Managed interop shim for benchmark + smoke tests.
- Parity test suite for core language features in current SMS.

## Acceptance Criteria
- Existing SMS core behavior is preserved for covered features.
- Native runtime can execute baseline scripts used by perf lab.
- Perf report includes managed/native SMS numbers with reproducible command.
- Error diagnostics include line/column and message parity at practical level.

## Dependencies
- Depends on: `CWUP/tasks/smlcore_cpp_native_runtime.md` (native foundation and interop patterns).

