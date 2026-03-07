# Native 3D Tools Port (Timeline / PosingEditor / Gizmos)

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
