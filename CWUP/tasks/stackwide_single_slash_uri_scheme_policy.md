# Stack-Wide Single-Slash URI Scheme Policy

## Goal
Define and enforce one canonical URI scheme policy across the full Forge stack:

- `res:/...`
- `appRes:/...`
- `user:/...`

and reject double-slash variants in Forge-controlled URI parsing paths:

- `res://...`
- `appRes://...`
- `user://...`

## Why
- Keep path syntax simple and consistent for users and AI-generated content.
- Prevent ambiguity with network-like URL mental models.
- Ensure deterministic path handling behavior across Runner, Designer, Poser, docs, and tooling.

## Scope
### Policy and specification
- Document the canonical URI scheme policy in public docs and internal implementation notes.
- Define normalization behavior for legacy content:
  - read/compat path may auto-normalize double-slash to single-slash where safe
  - write/output path must emit only single-slash canonical form

### Runtime and tooling alignment
- Audit and update URI/path resolvers in:
  - ForgeRunner runtime systems (SML/SMS/asset resolvers)
  - ForgeDesigner path and template outputs
  - ForgePoser path handling
  - related tools/scripts that generate or transform paths
- Ensure diagnostics clearly explain invalid double-slash inputs when strict mode is enabled.

### Migration and compatibility
- Add an explicit migration note for existing projects using `://` forms.
- Provide deterministic conversion rules with no traversal/security regression.

## Non-Goals
- No change to HTTP/HTTPS URL handling for network APIs that are not Forge URI schemes.
- No relaxation of existing sandbox/root-jail security constraints.

## Deliverables
- Canonical policy doc updates (including SMS/SML-relevant docs).
- Runtime and tooling updates to enforce single-slash canonical paths.
- Test coverage for parse/normalize/reject behavior.
- Migration note for existing `://`-based content.

## Acceptance Criteria
- All Forge-owned resource URI outputs are canonical single-slash (`res:/`, `appRes:/`, `user:/`).
- No Forge-owned parser/resolver emits `res://`, `appRes://`, or `user://`.
- Legacy double-slash inputs follow documented compatibility behavior and never bypass security checks.
- Managed/native and editor/runtime path behavior are consistent for covered cases.

## Dependencies
- Depends on:
  - `CWUP/tasks/sms_language_spec_2026_native.md`
