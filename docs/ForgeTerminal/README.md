# ForgeTerminal

Terminal-first workflow for Forge authoring, runtime control, and AI-agent automation.

## Why

UI-only guidance ages quickly (menus move, toggles rename, layouts change). A command-first surface stays stable and is easier for humans and agents.

Core goal: model intent as deterministic commands, not click paths.

## Interaction Model

Two-screen workflow:

1. Terminal: edit and control (SML/SMS, commands, history, hot reload)
2. Runtime view: visual feedback (Godot/ForgePoser viewport)

The terminal is the control plane. The viewport is the feedback plane.

## Principles (HCI)

### Try

Fast, low-friction experimentation.

- Commands should be cheap to run.
- Prefer non-destructive previews where possible.
- Keep command latency low.

### View

Immediate, unambiguous feedback.

- Every command should have a visible or textual result.
- State should be inspectable as text.
- Avoid hidden side effects.

### Undo

All significant edits must be reversible.

- Command history with `undo`/`redo`.
- Group drag/batch edits into single undo units.
- Reversal should restore exact prior state.

### Error Tolerant

Errors should be recoverable and informative.

- Validate before execute.
- Fail with actionable messages.
- Use transactional apply (all-or-nothing).
- Unknown commands should suggest close matches.

## Source Format

SML-first for machine/human interoperability.

- Avoid JSON as primary control payload for this workflow.
- Commands and state dumps should be text-oriented and diff-friendly.

Example state output:

```sml
Status {
    mode: "pose"
    frame: 73
    project: "res:/PoserDemoProjects/demo.scene"
}
```

## Command Pattern Baseline

Use one execution path for UI, CLI, and agents.

- `execute(context)`
- `undo(context)`
- optional: `serializeSml()`

Recommended runtime components:

1. `CommandBus`: dispatch + event emission
2. `History`: undo/redo stacks
3. `CommandGroup`: batch operations as one logical edit
4. `Validation`: argument + state preflight before mutation

## Hot Reload

Hot reload should be command-triggered and deterministic.

- File-watch is optional convenience, not source of truth.
- Prefer explicit commands for reload/apply.
- Debounce write bursts and preserve selection/frame when possible.

## AI-Agent Contract

Agents should output commands, not UI click instructions.

Good:

- `timeline.setKeyframeMove 73`
- `character "char1" pos x 5.6`

Avoid:

- "Open menu X and click Y"

## Practical Rollout

1. Define canonical command vocabulary (small, stable set).
2. Wire commands through shared `CommandBus`.
3. Add history + undo grouping.
4. Add terminal UI shell (e.g. TUI) on top of same commands.
5. Keep viewport as live feedback layer.

