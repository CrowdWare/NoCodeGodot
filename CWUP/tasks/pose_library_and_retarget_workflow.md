# Pose Library And Retarget Workflow

## Goal
Enable a fast production workflow where users can store a pose from animation keyframes,
browse/select it in a dialog, and apply it to the same or another character (retarget).

## Why
Current posing/keyframing is strong, but reusable pose workflows are missing.
For training/massage content, users need repeatable, named poses and quick transfer between rigs.

## Primary User Story
- User blocks a scene in Poser.
- User creates/edits a keyframe pose.
- User saves that pose into a catalog.
- User opens a pose dialog, chooses a pose, applies it to active character.
- If rigs differ, user runs retarget with mapping fallback and correction.

## Scope
- Save current keyframe pose as named pose asset.
- Pose Library dialog with search/filter and preview metadata.
- Apply selected pose to active character.
- Retarget pipeline for different skeleton naming/topology.
- Optional immediate keyframe write after apply.

## Out Of Scope (MVP)
- Advanced curve editing for pose blending.
- Full IK retarget solver.
- Cloud/shared pose registry.

## Runtime Surface (target)

### PosingEditor / Timeline actions
- `pose.saveToLibrary(name, tags, sourceFrame)`
- `pose.listLibrary(query, tags)`
- `pose.applyFromLibrary(poseId, options)`
- `pose.retargetPreview(poseId, targetCharacterId)`
- `pose.retargetApply(poseId, targetCharacterId, mappingProfile)`

### Suggested options object (SML-object style)
- `writeKeyframe: true|false`
- `frame: <int>`
- `mirror: true|false`
- `strength: 0..1`

## Data Format
Store poses as text assets, SML-first.

Suggested shape:

```sml
Pose {
    id: "pose_relaxed_back"
    name: "Relaxed Back"
    tags: "massage,back,neutral"
    sourceRig: "mixamo_v1"
    Bone { name: "mixamorig1_Spine" x: ... y: ... z: ... w: ... }
    Bone { name: "mixamorig1_LeftArm" x: ... y: ... z: ... w: ... }
}
```

## Retarget Strategy (MVP)
1. Exact bone-name match.
2. Normalized-name match (case/namespace cleanup).
3. Alias table/profile (user-editable mapping).
4. Unmapped bones are skipped with warnings (no hard fail).

## UX Requirements
- Pose dialog must be keyboard-first and fast.
- Applying a pose must be deterministic and undoable.
- Retarget warnings must be concise and actionable.
- "Try, View, Undo" loop must remain intact.

## Validation / Acceptance Criteria
- User can save pose from current keyed character in <= 2 interactions.
- User can apply saved pose to same rig and get matching result.
- User can apply to different rig with retarget and see explicit unmapped-bone warnings.
- Apply operation can be undone/redone reliably.
- Optional keyframe write works without adding unrelated channels.

## Risks
- Name-based retarget may miss custom rig conventions.
- Quaternion basis differences across rigs can create visual offsets.
- Library growth needs indexing for performance.

## Incremental Plan

### P0 - Same-Rig Pose Library
- Save/load/list/apply pose assets for same rig.
- Dialog skeleton + search/filter.
- Undo/redo integration.

### P1 - Retarget MVP
- Mapping pipeline + alias profile.
- Preview + apply with warnings.
- Persist mapping profile per project/user.

### P2 - Workflow Polish
- Batch apply to multiple characters.
- Pose categories and favorites.
- Optional blend strength/mirror improvements.

## Notes
- Keep this feature command-first so UI and CLI/agents use the same execution path.
- Preserve existing timeline sparsity rules; no implicit channel expansion on save/apply.
