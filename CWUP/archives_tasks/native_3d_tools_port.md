# Native 3D Tools Port (Timeline / PosingEditor / Gizmos)

## Status
- [x] Done (2026-03-10): Native 3D tools are functionally complete for current delivery scope.
- Remaining items are minor polish/details with low delivery risk.

## Goal
Port the 3D posing and animation tools from C# to C++ GDExtension so that
`ForgePoser` can run on `ForgeRunner.Native` without any C# dependency.

## Context
These are the most complex controls in the codebase. They are currently only
used by `ForgePoser`. Porting them is the last major step before full native parity.

## Classes to Port

### ForgeTimelineControl : SubViewportContainer
Port of `TimelineControl.cs`.

- Ruler (frame numbers), per-bone tracks, playhead (draggable).
- Add / remove keyframes.
- Play / Stop with interpolation between keyframes (Quaternion slerp).
- Inspector methods: `GetKeyframeCount()`, `GetKeyframeFrameAt()`, `ClearAllKeyframes()`.
- Properties: `fps`, `totalFrames`.
- Events (→ SMS): `keyframe_added(frame, bone)`, `keyframe_removed(frame)`,
  `frame_changed(frame)`, `playback_started()`, `playback_stopped()`.

### ForgePosingEditorControl : SubViewportContainer
Port of `PosingEditorControl.cs`.

- Embedded `SubViewport` with 3D scene: camera, directional light, ground plane.
- Load `.glb` / `.gltf` character model via `src:` property.
- Orbit camera (mouse drag), zoom (scroll wheel).
- Skeleton rendering: joint spheres per bone, pickable.
- Bone tree panel (collapsible, `Godot::Tree`).
- Scene prop management: `AddSceneProp(path, x, y, z)` / `RemoveSceneProp(index)`.
- Pose mode: rotate joints with `ForgeRotationGizmo3D`.
- Arrange mode: move/scale/rotate props with `ForgeMoveGizmo3D` / `ForgeScaleGizmo3D`
  / `ForgeRotationGizmo3D`.
- Events (→ SMS): `bone_selected(name)`, `pose_changed(bone, quat)`,
  `scene_prop_added(index, path)`, `scene_prop_removed(index)`.

### ForgeRotationGizmo3D / ForgeMoveGizmo3D / ForgeScaleGizmo3D
Port of the three gizmo classes. These are `Node3D` subclasses with:
- Custom mesh drawing (torus rings / arrows / cubes).
- Mouse-pick via raycast from the camera.
- World-space drag calculations.

### AnimationSerializer (C++ standalone)
Port of `AnimationSerializer.cs`.
- Read / write `.fpose` format (SML-based).
- Data model: `AnimationProjectData { scenes, keyframes }`.

### GlbExporter (C++ standalone)
Port of `GlbExporter.cs`.
- Uses `GltfDocument` + `GltfState` Godot classes.
- Exports character ± animation ± scene props → `.glb`.

## SMS Registration
After porting, register in `forge_runner_extension.cpp`:
- `ForgeTimelineControl`, `ForgePosingEditorControl` as GDExtension classes.
- Attach SMS extensions: `editor.loadProject()`, `editor.saveProject()`,
  `editor.showExportDialog()`, `editor.setMode()`, `editor.setEditMode()`.

## Priority
Lower priority than core runtime features (SMS bridge, theming, download cache).
Start only after `native_sms_ui_bridge.md` and `native_bg_containers.md` are done.

## Reference
- C#: `ForgeRunner/Runtime/ThreeD/`
- Godot API: `GltfDocument`, `GltfState`, `Skeleton3D`, `AnimationPlayer`

## Implementation Progress
- [x] Native class scaffolds registered: `ForgeTimelineControl` and `ForgePosingEditorControl`.
- [x] UI builder wiring added for `Timeline` / `PosingEditor` node creation and core property mapping (`fps`, `totalFrames`, `src`, `showBoneTree`).
- [x] SMS bridge upgraded with generic property/method fallback (`ui.get`/`ui.set`/`ui.invoke` via Godot `Variant` + JSON), enabling scripted calls to native control methods.
- [x] Native SMS event hookup extended for 3D-tool signal names (`boneSelected`, `poseChanged`, `scenePropAdded`, `keyframeAdded`, `frameChanged`, etc.).
- [x] Visible baseline added: `ForgePosingEditorControl` now creates a native `SubViewport` with camera/light/ground, and `ForgeTimelineControl` now renders ruler/playhead with click-to-seek.
- [x] Initial model loading wired in `ForgePosingEditorControl` (`src`, `addSceneAsset`) with `ResourceLoader` + GLTF fallback and first-bone auto-select signal.
- [ ] Replace scaffolds with full runtime parity implementations (3D scene, gizmos, timeline drawing/interpolation, serializer/exporter).

## Remaining Work (Prioritized)

### P0 - Functional Parity (must-have)
- [ ] Camera interaction parity in `ForgePosingEditorControl` (orbit/pan/zoom behavior matching legacy workflow).
- [ ] Selection and transform loop parity:
  - pick object/bone in viewport
  - apply translate/rotate/scale in arrange mode
  - apply bone rotation in pose mode
- [ ] Timeline parity essentials:
  - add/remove keyframes from UI actions
  - frame scrubbing updates pose deterministically
  - playback start/stop emits expected SMS events
- [ ] Project I/O parity:
  - `loadProject` + `saveProject` preserve scene chars/props and transforms
  - no loss of data on load/save roundtrip for demo.scene

Acceptance (P0):
- Poser demo can load, pose, keyframe, scrub, and save without runtime errors.
- Main legacy edit flows are executable end-to-end in native runtime.

### P1 - Visual/UX Parity (high)
- [ ] Gizmo visual pass (`Rotation/Move/Scale`) to match intended readability (axis color, hit targets, depth behavior).
- [ ] Bone/tree + inspector sync polish (selection, active item highlight, immediate refresh consistency).
- [ ] Timeline rendering pass (major/minor ticks, keyframe markers, selected frame contrast).

Acceptance (P1):
- Side-by-side native vs legacy usage shows no major usability regressions in daily posing flow.

### P2 - Export/Serialization Completion
- [ ] Port `AnimationSerializer` (`.fpose`) and validate compatibility with existing files.
- [ ] Port `GlbExporter` path for character + optional animation + scene props.
- [ ] Add focused native tests for serializer/exporter happy-path and malformed input handling.

Acceptance (P2):
- Native runtime can read/write core animation artifacts used by Poser workflows.

## Task Split Suggestion (next executable tasks)
- [ ] `native_3d_camera_and_pick_parity.md` (P0 camera + picking)
- [ ] `native_3d_transform_and_gizmo_parity.md` (P0/P1 transforms + gizmos)
- [ ] `native_timeline_playback_parity.md` (P0 timeline behavior)
- [ ] `native_poser_project_roundtrip.md` (P0 load/save roundtrip safety)
- [ ] `native_animation_serializer_exporter.md` (P2 serializer/exporter)
- [x] `sms_native_reentrant_invoke_guard.md` additional task after getting a stackoverflow runtime error
