# Native Animation Serializer And Exporter

## Goal
Port and validate native equivalents for animation serialization (`.fpose`) and
GLB export workflows used by Poser.

## Scope
- `AnimationSerializer` native implementation for read/write.
- `GlbExporter` native implementation for scene/animation export.
- Focused compatibility checks against existing project artifacts.

## Implementation Steps
- Port serializer data model and IO routines from legacy C#.
- Port GLB export flow using Godot `GLTFDocument` + `GLTFState`.
- Add tests for:
  - valid load/save/load roundtrip
  - malformed input handling
  - minimal export happy path
- Integrate exporter hooks used by Poser SMS flow.

## Acceptance Criteria
- Native runtime can read and write core `.fpose` files used by Poser.
- Native GLB export succeeds for representative scene+animation sample.
- Failure modes return clear warnings/errors without runtime crash.

## Risks
- Format drift between legacy serializer and native implementation.
- Export incompatibilities due to engine-side GLTF behavior changes.

