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
        Timeline {
            id: timeline
            fps: 24
            totalFrames: 120
            sizeFlagsHorizontal: expandFill
        }
    }
}
