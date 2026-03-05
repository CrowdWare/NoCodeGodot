# SMS AI Authoring Profile v0

This profile defines how SMS should be designed and validated so it is reliably writable by any AI model, not only specific assistants.

## Goals

- deterministic generation
- deterministic parsing
- low ambiguity
- stable machine feedback loops

## Core Rules

1. Single canonical syntax per concept
- Avoid multiple equivalent spellings.
- One preferred form for declarations, calls, events, and control flow.

2. Deterministic grammar
- No context-sensitive shortcuts that change meaning by position.
- Parsing must not depend on runtime state.

3. Strict structure, simple semantics
- Favor explicit blocks and explicit separators.
- Minimize implicit conversions and hidden behavior.

4. Stable language versioning
- Every SMS document can declare language version.
- Breaking grammar/semantic changes require version bump.

## AI-Safe Subset (v0)

Recommended default subset for generated code:

- `var`, `fun`, `return`
- `if`, `when`, `for`, `while`
- event handlers: `on <id>.<event>(...) { ... }`
- typed annotations (optional but preferred in generated code)
- imports/exports (once module system lands)

Avoid in generated code until fully stabilized:

- advanced meta features
- ambiguous shorthand forms
- implicit cross-thread state mutation

## Canonical Formatting Requirements

1. Enforce formatter output
- Generated SMS must pass formatter with zero diffs.

2. Enforce lint rules
- No unknown symbols, no duplicate handlers, no mixed invalid arg styles.

3. Enforce normalized style
- Lower camel case for ids/events/functions where applicable.
- Stable quote and spacing conventions.

## Error Model For AI Loops

Compiler/parser/runtime diagnostics should be machine-oriented:

- `code`: stable identifier (e.g. `SMS1003`)
- `message`: human-readable summary
- `file`, `line`, `column`
- `severity`: error/warning/info
- `hint`: one concrete auto-fix direction

Example:

```text
SMS2011 error at main.sms:12:19
Unknown named argument 'speed' for 'Vec3D'.
Hint: valid args are x, y, z.
```

## Schema-Driven Runtime Surface

All SMS-callable functions/events should be discoverable from machine-readable schema:

- function name
- parameter names
- parameter types
- return type
- side-effect/thread affinity metadata

This enables model-agnostic codegen and robust validation.

## Async Safety For AI

AI-generated async code must obey:

- no UI mutation outside main-thread context
- no hidden scope loss after `await`
- explicit async boundaries
- deterministic fallback behavior on failure

## Acceptance Gate For AI-Generated SMS

A generated SMS file is accepted only if:

1. parses successfully
2. passes formatter
3. passes linter
4. passes type checks (mode-dependent)
5. passes backend parity checks (managed/native where applicable)

If any gate fails, diagnostics must be fed back in structured form.
