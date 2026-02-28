Window {
    id: mainWindow
    title: "ForgePoser"
    minSize: 1024, 640
    size: 1280, 800

    VBoxContainer {
        anchors: left | top | right | bottom
        spacing: 0

        // ── Toolbar ──────────────────────────────────────────────────────
        HBoxContainer {
            id: toolbar
            sizeFlagsHorizontal: expandFill
            padding: 4, 4, 4, 4
            spacing: 6

            Button {
                id: btnOpen
                text: "Open GLB"
            }

            VSeparator {}

            Button {
                id: btnResetPose
                text: "Reset Pose"
            }

            Button {
                id: btnSavePose
                text: "Save Pose"
            }

            Control { sizeFlagsHorizontal: expandFill }

            Label {
                id: statusLabel
                text: "No model loaded"
                sizeFlagsVertical: shrinkCenter
            }
        }

        HSeparator {}

        // ── Main area: PosingEditor ───────────────────────────────────────
        PosingEditor {
            id: editor
            src: "file:///Users/art/SourceCode/Forge/docs/Default/assets/models/Opa.glb"
            showBoneTree: true
            sizeFlagsHorizontal: expandFill
            sizeFlagsVertical: expandFill

            JointConstraint { bone: "RightKnee"  minX: -140 maxX: 0 minY: -20 maxY: 20 }
            JointConstraint { bone: "LeftKnee"   minX: -140 maxX: 0 minY: -20 maxY: 20 }
            JointConstraint { bone: "RightElbow" minX: -145 maxX: 0 minY: 0   maxY: 0  }
            JointConstraint { bone: "LeftElbow"  minX: -145 maxX: 0 minY: 0   maxY: 0  }
        }

        HSeparator {}

        // ── Timeline ─────────────────────────────────────────────────────
        // (Phase 2 — will be replaced by Timeline control)
        HBoxContainer {
            id: timelineBar
            sizeFlagsHorizontal: expandFill
            padding: 6, 4, 6, 4
            spacing: 8

            Button { id: btnPlay  text: "▶" }
            Button { id: btnStop  text: "■" }
            Label  { text: "Frame:" sizeFlagsVertical: shrinkCenter }
            Label  { id: frameLabel text: "0" sizeFlagsVertical: shrinkCenter }
        }
    }
}
