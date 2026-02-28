# Forge: PosingEditor & Timeline Controls — Claude Code Task

## Context

Forge is a UI framework that parses SML (Simple Markup Language, similar to QML) and instantiates
Godot objects. It already has:

- A working `Viewport3D` control that loads `.glb` models and plays `.glb` animations
- SML markup parsed into Godot nodes at runtime
- SMS (Simple Multiplatform Script, Kotlin-based interpreter in a sandbox) for event logic

The goal is to build two new Forge controls: **PosingEditor** and **Timeline**, plus a shared
**RotationGizmo3D** node that both controls depend on.

---

## 1. RotationGizmo3D

A runtime 3D rotation gizmo rendered directly in the Godot viewport. Inspired by Blockbench's gizmo.

### Visual Design
- Three torus rings, one per axis:
  - X axis → red (`TorusMesh`)
  - Y axis → green (`TorusMesh`)
  - Z axis → blue (`TorusMesh`)
- Each ring has a small draggable **handle** (`SphereMesh`) positioned at 90° on the ring
- Only the handle is interactive — dragging it rotates the bone on that axis
- The gizmo renders on top of the model (no depth occlusion)

### Behavior
- Attach to a `Skeleton3D` bone by index
- Position itself at the bone's global transform origin every frame
- On handle drag: rotate the bone on the corresponding axis
- **Min/Max limits per axis** — the ring visually shows the allowed arc, rotation is clamped:
  ```
  gizmo.limitX(minDeg: Float, maxDeg: Float)
  gizmo.limitY(minDeg: Float, maxDeg: Float)
  gizmo.limitZ(minDeg: Float, maxDeg: Float)
  ```
- When a limit is reached, the handle stops and the ring turns orange as visual feedback
- Gizmo is hidden when no bone is selected

### Godot References to study
- `editor/plugins/skeleton_3d_editor_plugin.cpp` — Godot's own bone gizmo implementation
- `scene/3d/skeleton_3d.cpp` — `set_bone_pose_rotation()`, `get_bone_global_pose()`
- `SkeletonIK3D` — for automatic IK solving after manual bone rotation

---

## 2. PosingEditor Control

A Forge control that combines a 3D viewport, a bone tree panel, and the RotationGizmo3D into one
self-contained posing tool.

### SML Usage
```
PosingEditor {
    src: "res://assets/models/demo.glb"
    showBoneTree: true
}
```

### Sub-components

#### 2a. 3D Viewport (left/center area)
- Renders the loaded `.glb` model with its `Skeleton3D`
- Displays **pickable joint spheres** at every bone position (`SphereMesh`, semi-transparent)
- On joint click (raycasting via `PhysicsDirectSpaceState3D`):
  - Selects the bone
  - Shows the `RotationGizmo3D` at that joint
  - Highlights the corresponding entry in the BoneTree
- Camera: orbit with mouse drag, zoom with scroll wheel, touch-friendly for tablet

#### 2b. BoneTree Panel (right side)
- Hierarchical tree of all bones read from `Skeleton3D.get_bone_name()` / `get_bone_parent()`
- Click on a bone entry → selects it, shows gizmo in viewport
- Selected bone is highlighted
- Collapsible bone groups

#### 2c. Pose Data Model
- Internal dictionary: `boneName → Quaternion`
- Updated on every gizmo interaction
- Readable via SML property `editor.poseData`

### Joint Constraint System
Constraints are defined in SML and applied to the gizmo:
```
PosingEditor {
    src: "res://assets/models/demo.glb"

    JointConstraint { bone: "RightKnee";  minX: -140; maxX: 0; minY: -30; maxY: 30 }
    JointConstraint { bone: "LeftKnee";   minX: -140; maxX: 0; minY: -30; maxY: 30 }
    JointConstraint { bone: "RightElbow"; minX: -145; maxX: 0; minY: 0;   maxY: 0  }
    JointConstraint { bone: "LeftElbow";  minX: -145; maxX: 0; minY: 0;   maxY: 0  }
}
```

### IK Support
- Use Godot's `SkeletonIK3D` as a child of `Skeleton3D`
- When a hand or foot bone is moved, IK automatically resolves the chain
- IK chain length configurable per limb

### Events (SMS syntax)
```kotlin
on editor.boneSelected(boneName) { }
on editor.poseChanged(boneName, rotation) { }
on editor.poseReset() { }
```

### Methods
```kotlin
editor.resetPose()
editor.exportPoseAsGLB(path)
editor.loadPose(poseData)        // load a previously saved SML pose
editor.savePoseAsSML()           // returns SML string of current pose
```

### Pose saved as SML (for AI context feeding)
```
Pose {
    source: "res://assets/models/demo.glb"
    Bone { name: "Hips";        rotX: 0;    rotY: 15;   rotZ: 0   }
    Bone { name: "Spine";       rotX: 10;   rotY: 0;    rotZ: 0   }
    Bone { name: "LeftArm";     rotX: -45;  rotY: 0;    rotZ: 90  }
    Bone { name: "RightArm";    rotX: -45;  rotY: 0;    rotZ: -90 }
    Bone { name: "RightKnee";   rotX: -90;  rotY: 0;    rotZ: 0   }
}
```

---

## 3. Timeline Control

A Forge control for keyframe-based animation editing.

### SML Usage
```
Timeline {
    fps: 24
    totalFrames: 120
}
```

### Visual Layout
- Horizontal scrollable track area
- One track per bone that has keyframes
- Keyframe diamonds on the track at their frame position
- Playhead (vertical line) draggable to scrub
- Play / Pause / Stop buttons
- Frame counter display

### Behavior
- **Set Keyframe**: captures current `PosingEditor.poseData` at current frame
- **Scrub**: moving playhead interpolates between keyframes and applies pose to model
- **Interpolation**: Linear by default, with Ease In/Out option per keyframe
- **Delete Keyframe**: select diamond + delete key
- Multiple bones can share the same frame or have independent keyframe positions

### Events (SMS syntax)
```kotlin
on timeline.frameChanged(frame) { }
on timeline.keyframeAdded(frame, boneName) { }
on timeline.keyframeRemoved(frame, boneName) { }
on timeline.playbackStarted() { }
on timeline.playbackStopped() { }
```

### Methods
```kotlin
timeline.setKeyframe(frame, poseData)
timeline.removeKeyframe(frame)
timeline.play()
timeline.stop()
timeline.exportAnimationAsGLB(path)
```

---

## 4. Scene Builder (optional, Phase 2)

Allow dragging additional `.glb` objects into the PosingEditor scene to build a stage/environment.

### SML Usage
```
PosingEditor {
    src: "res://assets/models/character.glb"
    SceneAssets {
        assetsPath: "res://assets/props/"
    }
}
```

- Asset panel showing thumbnails of available `.glb` props
- Drag & drop into viewport to place object
- Selected objects get a standard move/scale gizmo (not rotation gizmo)
- Scene exported as single `.glb` including character pose + props

---

## 5. Full SML Example

```
Window {
    Column {
        PosingEditor {
            id: "editor"
            src: "res://assets/models/demo.glb"
            showBoneTree: true

            JointConstraint { bone: "RightKnee"; minX: -140; maxX: 0 }
            JointConstraint { bone: "LeftKnee";  minX: -140; maxX: 0 }
        }

        Timeline {
            id: "timeline"
            fps: 24
            totalFrames: 120
        }
    }
}
```

### SMS wiring
```kotlin
on editor.poseChanged(boneName, rotation) {
    // auto-set keyframe on pose change
    timeline.setKeyframe(timeline.currentFrame, editor.poseData)
}

on timeline.frameChanged(frame) {
    editor.loadPose(timeline.getPoseAt(frame))
}
```

---

## 6. Godot Source References

Study these before implementing:

| File | Relevant for |
|---|---|
| `editor/plugins/skeleton_3d_editor_plugin.cpp` | Bone gizmo, picking, SubGizmo pattern |
| `scene/3d/skeleton_3d.cpp` | `set_bone_pose_rotation()`, bone transforms |
| `scene/3d/skeleton_ik_3d.cpp` | IK solver integration |
| `modules/gltf/gltf_document.cpp` | Runtime GLB export with skeleton pose |
| `github.com/thimenesup/BoneGizmo` | Simple reference implementation in Godot |

---

## 7. What Godot Does NOT Have (must be built)

| Missing piece | Notes |
|---|---|
| Runtime RotationGizmo3D with min/max limits | Godot's gizmo only works in editor, not runtime |
| Pickable joint spheres at runtime | Must be placed manually on bone positions each frame |
| SML-driven JointConstraint system | Custom Forge node |
| Timeline control | No Godot runtime equivalent of AnimationTrackEditor |
| Pose → SML serialization | Custom serializer |
| AI-readable pose format | SML Pose block (see section 2) |

---

## 8. Platforms

Must work on:
- Desktop (Windows, macOS, Linux)
- Web (Godot HTML5 export)
- Tablet (Android / iPad via Godot mobile export)

Touch input must work for: bone picking, gizmo handle dragging, timeline scrubbing.


## 9. TestEnvironment
We will create a new Forge-app in the folder /ForgePoser.
There we will use these new controls in an animation creation tools.
The export output will be a .glb with an animation.
And maybe a scene (phase 2) as .glb.

---

## 10. Implementierungsstand & offene Punkte

### Fertig implementiert
- RotationGizmo3D (Torus-Ringe, Handles, World-Space-Rotation, Limits, Orange-Feedback, Winkel-Label)
- PosingEditorControl (Orbit-Kamera, Joint-Spheres, Picking, BoneTree als collapsible Tree, Constraints)
- TimelineControl (Ruler, Bone-Tracks, Keyframe-Diamonds, Playhead, Play/Stop, lineare Interpolation)
- SMS-Verdrahtung: `poseChanged → setKeyframe`, `frameChanged → loadPose`
- ForgePoser App: `main.sml` + `main.sms`

### Abgeschlossen (Phase 2)

| Feature | Aufwand | Hinweis |
|---|---|---|
| `btnOpen` — Datei-Dialog um GLB zu laden | mittel | `ui.openFileDialog(callback)` in SmsUiRuntime; `main.sms` Handler |
| Keyframe löschen (UI-Geste) | klein | Rechtsklick auf Diamond-Position oder Del-Taste in TimelineTrackArea |
| SMS-Events `keyframeAdded` / `keyframeRemoved` | klein | In `SetKeyframe`/`RemoveKeyframe` gefeuert; Dispatcher + SmsUiRuntime-Handler |

### Noch offen (Phase 3, optional)

| Feature | Aufwand | Hinweis |
|---|---|---|
| Touch-Input (Tablet) | mittel | Poll-Drag nutzt Maus; `InputEventScreenTouch` / `InputEventScreenDrag` ergänzen |
| `exportPoseAsGLB(path)` | groß | Godot `GltfDocument` mit gesetzten Bone-Poses exportieren |
| `exportAnimationAsGLB(path)` | groß | Keyframes → `AnimationLibrary` → `GltfDocument` exportieren |
| IK-Support (`SkeletonIK3D`) | groß | Godot `SkeletonIK3D` als Kind von Skeleton3D; Hand/Fuß-Knochen triggern IK-Solve |