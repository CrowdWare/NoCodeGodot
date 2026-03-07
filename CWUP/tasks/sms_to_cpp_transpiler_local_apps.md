# SMS → C++ Transpiler For Local Apps

## Goal
Add an optional transpiler that converts SMS scripts directly to C++ for apps
that run locally and are not deployed over HTTP — eliminating interpreter overhead
in production-local builds while keeping the authoring model unchanged.

## Why
- Remove interpreter overhead in production-local builds.
- Keep authoring model unchanged: users continue writing SMS only.
- The interpreted path stays active for HTTP/deployed scenarios (hot-reload, no rebuild).

## Architecture: SMS AST → C++ (direct, no IL)

The SMS AST produced by the existing `SMSCore.Native` parser is the natural
intermediate representation. A second backend consuming the same AST is all
that is needed — a separate IL format would add spec maintenance, versioning
overhead, and a second transformation pass with no immediate benefit.

```
SMS source
    ↓  (existing SMSCore.Native lexer + parser — unchanged)
SMS AST
    ↓  (new: SmsCodegenCpp)
Generated .cpp + .h
    ↓  (CMake / ninja)
Native binary — no interpreter in hot path
```

If a future additional backend is ever needed, it adds a second consumer of
the same SMS AST — not a consumer of a custom IL.

## Why Not IL

- `SMSCore.Native` is already native C++. Interpreter overhead is only relevant
  for tight loops or heavy computation, which UI event handlers are not.
- **Evaluate first**: before implementing the transpiler, profile a
  representative local app to confirm the interpreter is actually the bottleneck.
  If it isn't, the interpreted path is the right answer and this task is deferred.
- An IL layer is justified only when two or more concrete backends exist
  simultaneously. With one target (C++) the SMS AST is the IR.

## Scope
- Implement `SmsCodegenCpp`: an AST walker that emits a `.cpp` + `.h` pair per
  SMS script.
- Integrate into local build flow only (`--sms-transpile=true` flag).
- Keep the standard interpreted SMS path for HTTP/deployed scenarios (no change).
- Document which SMS constructs are supported; unsupported constructs emit a
  clear compile-time diagnostic and abort the build.

## Non-Goals
- No IL format, no IL spec, no IL versioning.
- No additional language backends (Compose/JVM path is deprecated).
- No JIT.
- No remote/HTTP deployment transpilation.
- No changes to the SMS authoring language.

## Supported Subset (v1)
The first version targets the constructs actually used in practice:

| Construct | Status |
|---|---|
| `var`, assignment, arithmetic | ✅ |
| `if` / `else` | ✅ |
| `while`, `for … in` | ✅ |
| `fun` (top-level functions) | ✅ |
| `when` (event handler registration) | ✅ |
| Native function calls (`ui.*`, `os.*`, `log.*`) | ✅ via generated glue |
| Closures / first-class functions | — (not in SMS language) |
| Dynamic `ui.getObject()` with runtime id | ⚠️ supported with warning |
| `Array`, `Dictionary` literals | ✅ |
| Recursive functions | ✅ |

## Deliverables
- `SMSCore.Native/src/sms_codegen_cpp.cpp` + `.h` — AST → C++ code generator.
- `run.sh` flag `--sms-transpile=true` that invokes codegen before CMake build.
- Generated runtime glue header (`sms_native_glue.h`) wiring `ui.*` / `os.*`
  calls to the same C++ bridge used by the interpreter.
- Documentation: supported subset, limits, how to add a new construct.

## Acceptance Criteria
- **Profiling gate**: task is only implemented if profiling shows interpreter
  overhead > 5 % of frame time in a representative local app.
- Local app runs with transpiled SMS and no interpreter in the hot path.
- Generated C++ is deterministic for identical SMS input.
- Event handlers and supported control-flow produce behaviour-identical output
  to the interpreter for the defined subset.
- Unsupported constructs abort the build with a clear, actionable error message.
- Interpreted path is unaffected; switching `--sms-transpile` off reverts to
  interpreter with no other changes.
