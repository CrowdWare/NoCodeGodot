# Source Of Truth: Self-Hosted Git, GitHub As Mirror (No PR Flow)

## Goal
Move the Forge repository workflow to a self-hosted Git platform as the single
source of truth. Keep GitHub as a read-only/public mirror and marketing channel.
Do not use Pull Requests as the core development flow.

## Scope
- Repository hosting and collaboration flow only.
- CI/CD ownership handover away from GitHub Actions.
- Branch/process policy for direct maintainer-driven development.

## Out Of Scope
- Rewriting Forge runtime code.
- SMS/SML language or interpreter changes.
- Product feature changes unrelated to repository operations.

## Requirements
- Primary remote: self-hosted Forgejo/Gitea (or equivalent), authoritative.
- Secondary remote: GitHub mirror, one-way sync from primary to GitHub.
- GitHub must be clearly marked as mirror-only (README + links).
- No PR workflow required for core development.
- Preserve release/tag visibility on GitHub mirror.

## Target Workflow (No PR)
- Trunk-based maintainer flow on `main`.
- No force-push on protected branches.
- Optional local feature branches, but integration remains maintainer-driven.
- CI gates on primary host; release artifacts originate from primary host.

## Implementation Steps
1. Stand up self-hosted Git service and migrate current repository.
2. Set branch protections/policies (`main` protected, no force-push).
3. Configure CI on primary host and verify parity with current build/test jobs.
4. Configure one-way mirror from primary host to GitHub (`main` + tags).
5. Mark GitHub repository as mirror-only and disable/redirect Issues/PRs.
6. Switch local/team default remotes to self-hosted primary.
7. Dry-run rollback plan and document cutover steps.

## Acceptance Criteria
- Developers push/pull against self-hosted primary by default.
- GitHub receives mirrored commits/tags automatically from primary.
- No PRs are required to ship changes.
- CI/release pipeline works without GitHub as control plane.
- Migration and rollback docs are committed and reproducible.

## Risks
- CI mismatch during migration can block delivery.
- Incomplete mirror rules could leak split-brain history.
- Team tooling may still assume GitHub APIs.

## Validation
- Perform at least one full cycle:
  - commit on primary -> CI green -> tag/release -> mirrored on GitHub.
- Verify write protection expectations on GitHub mirror.
- Verify all project docs point to primary source-of-truth URLs.
