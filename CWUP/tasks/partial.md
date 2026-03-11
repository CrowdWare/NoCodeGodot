

# Task: Introduce a partial pattern for SML/SMS and split for example `ForgePoser/main.sms`

## Goal

Introduce a **partial pattern** for Forge SML/SMS files so that one logical screen or module can be split into multiple smaller files with reduced context.

The first real use case is `ForgePoser/main.sms`, which currently has around **900 lines** and should be distributed into smaller, responsibility-based files.

The purpose of this task is:

- keep file context small
- improve grep/search speed
- make AI-assisted editing safer
- reduce cognitive load
- make Git diffs smaller and more focused
- prepare Forge for larger projects

---

## High-Level Idea

A logical module should still feel like **one unit**, but it may be split into multiple files.

Example:

```text
main.sms
main.events.sms
main.timeline.sms
main.pose.sms
main.io.sms
```

Forge should treat these files as belonging to the same logical module.

This is conceptually similar to **partial classes** in C#, but applied to SML/SMS modules.

---

## Requirements

### 1. Partial file naming convention

Support a pattern like:

```text
<name>.sms
<name>.<part>.sms
```

Examples:

```text
main.sms
main.events.sms
main.timeline.sms
```

The same idea should later also work for SML files:

```text
main.sml
main.ui.sml
main.dialogs.sml
main.toolbar.sml
```

---

### 2. Loader behavior

When Forge loads `main.sms`, it should also detect and load matching partial files.

Expected behavior:

- `main.sms` remains the logical entry file and should contain `on <id>.ready(){}` for example.
- all `main.*.sms` files belonging to that base name are collected
- files are merged or processed in a deterministic order
- order must be documented and predictable

Recommended default order:

1. `main.sms`
2. `main.events.sms`
3. `main.timeline.sms`
4. remaining `main.*.sms` files sorted alphabetically

If a simpler deterministic strategy is easier to implement, that is acceptable, but it must be clearly documented.

---

### 3. No magic across unrelated files

Only files that share the same base name must be grouped together.

Example:

- `main.sms` loads `main.events.sms`
- `timeline.sms` must **not** be included automatically
- `mainWindow.sms` must **not** be included automatically

---

### 4. Error reporting

Compiler/parser/runtime errors must still report the **real partial filename** and line number.

This is important so that developers and AI tools can fix the correct file quickly.

---

### 5. Keep context small

The implementation should preserve the main benefit of the pattern:

- developers should be able to work on one responsibility in one file
- AI should not need to load 900 lines when only one event handler changes
- grep/search should target smaller files directly

---

## First target: split `ForgePoser/main.sms`

Refactor `ForgePoser/main.sms` into smaller files with clear responsibilities.

Suggested split:

```text
main.sms             -> root module / onReady / initialization /shared setup / core declarations
main.events.sms      -> allgemeine Events, nur wenn wirklich generisch
main.dispatcher.sms  -> default dispatcher / event routing / id -> handler mapping
main.timeline.sms    -> timeline / scrubber / playback-related logic
main.pose.sms        -> pose apply / pose edit / character pose logic
main.io.sms          -> load / save / import / export
main.menu.sms        -> menu actions / menu handlers
main.treeview.sms    -> tree selection / expand / collapse / node handling
```

If during implementation a slightly different split makes more sense, that is fine, but the split should remain **responsibility-based**.

Do **not** split arbitrarily by line count.

---

## Constraints

- Keep behavior unchanged
- This task is primarily about structure, not new features
- Avoid introducing unnecessary complexity
- Do not require a GUI change for this task
- Prefer a simple, deterministic implementation over a highly abstract one

---

## Acceptance Criteria

- Forge supports loading partial SMS files
- `ForgePoser/main.sms` is reduced significantly from its current ~900 lines
- Logic is distributed across multiple partial files with meaningful names
- Application behavior remains unchanged
- Error messages point to the correct physical file
- The loading strategy is deterministic and documented

---

## Why this matters

This task prepares Forge for larger real-world projects.

It improves:

- maintainability
- readability
- AI collaboration
- terminal-based workflows
- future modularization of SML/SMS projects

This is not just a refactor.
It establishes a **foundational pattern** for scaling Forge without growing single-file monoliths.
