# Resilient Source Distribution Playbook (Junior-Friendly)

## Goal
Document and implement a practical, low-drama resilience setup so Forge cannot
be easily taken offline by a single platform/provider failure.

This is not a "magic bullet" architecture. It is a reproducible playbook that
junior developers can follow and operate.

## Scope
- Repository and release distribution resilience.
- Backup and recovery process.
- Clear operating checklist for small teams.

## Out Of Scope
- Replacing Git itself.
- P2P-only write workflows.
- Legal strategy.

## Baseline Architecture
1. Primary write source: self-hosted Forgejo/Gitea.
2. Public mirrors: GitHub (mirror-only), optional second mirror.
3. Immutable artifacts: periodic Git bundles + release artifacts.
4. Optional decentralized archive: IPFS pinning for selected artifacts.
5. Offsite backups: at least one independent provider/location.

## Practical Steps
1. Define "source of truth" and enforce one-way mirroring.
2. Add automated daily backups for:
   - Forgejo DB
   - `repos/`
   - `data/` (attachments/LFS/config)
3. Add signed release manifest:
   - commit SHA
   - tag
   - artifact hashes
   - optional IPFS CIDs
4. Add monthly restore drill:
   - recover to clean host
   - verify repo integrity
   - verify tags/releases
5. Add incident runbook:
   - primary down
   - mirror promotion
   - DNS switch
   - communication template

## Junior-Dev Deliverables
- `docs/ops/git_resilience.md` (step-by-step operations guide).
- `docs/ops/disaster_recovery.md` (checklist + timing targets).
- `scripts/ops/backup_*` and `scripts/ops/restore_*` (minimal automation).
- One "tabletop exercise" protocol for onboarding.

## Acceptance Criteria
- Team can restore Forge service from backups on a fresh host.
- Mirrors stay up to date without manual pushes.
- At least one recovery drill completed and documented.
- A junior dev can execute the documented restore procedure successfully.

## Risks
- False confidence without tested restores.
- Split-brain if mirror direction is misconfigured.
- IPFS pinning assumed durable without enough independent pinning nodes.

## Validation
- Quarterly dry-run restore to a disposable environment.
- Verify signed manifest against restored artifacts.
- Verify mirror lag SLA (for example: < 10 minutes for `main` and tags).
