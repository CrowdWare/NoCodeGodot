Window {
    id: mainWindow
    title: "ForgePoser"
    minSize: 1024, 680
    size: 1440, 900

    MenuBar {
        preferGlobalMenu: true

        PopupMenu {
            id: menuFile
            title: "File"

            Item { id: menuNew     text: "New"           }
            Item { id: menuOpen    text: "Open Project…" }
            Item { id: menuSave    text: "Save"          }
            Item { id: menuSaveAs  text: "Save As…"      }
            Item { id: menuOpenGlb text: "Load Model…"   }
        }
    }

    VBoxContainer {
        anchors: left | top | right | bottom
        spacing: 0

        DockingHost {
            id: mainDockHost
            sizeFlagsHorizontal: expandFill
            sizeFlagsVertical: expandFill
            gap: 4

            // ── Left panel: Scene Assets + Keyframe Inspector (two tabs) ──────
            DockingContainer {
                id: leftDock
                dockSide: left
                fixedWidth: 240
                dragToRearrangeEnabled: true
                tabsRearrangeGroup: 1

                VBoxContainer {
                    id: scenePanel
                    leftDock.title: "Scene"
                    padding: 4, 4, 4, 4
                    spacing: 4

                    Label { text: "Scene Assets"  fontSize: 11  fontWeight: bold }

                    ItemList {
                        id: sceneAssetList
                        customMinimumSize: 0, 110
                        sizeFlagsHorizontal: expandFill
                    }

                    HBoxContainer {
                        spacing: 4
                        Button { id: btnAddProp    text: "+"  customMinimumSize: 28, 24 }
                        Button { id: btnRemoveProp text: "−"  customMinimumSize: 28, 24 }
                    }
                }

                VBoxContainer {
                    id: kfPanel
                    leftDock.title: "Keyframes"
                    padding: 4, 4, 4, 4
                    spacing: 4

                    Label { text: "Keyframes"  fontSize: 11  fontWeight: bold }

                    Tree {
                        id: keyframeTree
                        sizeFlagsHorizontal: expandFill
                        sizeFlagsVertical: expandFill
                        hideRoot: true
                    }
                }

            }

            // ── Center: Toolbar + PosingEditor (top) + Timeline (bottom) ─────
            DockingContainer {
                id: centerDock
                dockSide: center
                flex: true
                closeable: false
                dragToRearrangeEnabled: false

                VBoxContainer {
                    centerDock.title: "Viewport"
                    sizeFlagsHorizontal: expandFill
                    sizeFlagsVertical: expandFill
                    spacing: 0

                    HBoxContainer {
                        id: modeToolbar
                        padding: 4, 2, 4, 2
                        spacing: 4

                        Button { id: btnPoseMode    text: "Pose"    toggleMode: true  buttonPressed: true  }
                        Button { id: btnArrangeMode text: "Arrange" toggleMode: true  buttonPressed: false }

                        VSeparator { }

                        Button { id: btnModeMove   text: "Move"   toggleMode: true  buttonPressed: true   disabled: true }
                        Button { id: btnModeScale  text: "Scale"  toggleMode: true  buttonPressed: false  disabled: true }
                        Button { id: btnModeRotate text: "Rotate" toggleMode: true  buttonPressed: false  disabled: true }

                        VSeparator { }
                        
                        Button { id: btnToggleJoints text: "Joints ●" customMinimumSize: 80, 0 }
                    }
                    VSplitContainer {
                        sizeFlagsHorizontal: expandFill
                        sizeFlagsVertical: expandFill

                        PosingEditor {
                            id: editor
                            src: ""
                            showBoneTree: false
                            normalizeNames: true
                            sizeFlagsHorizontal: expandFill
                            sizeFlagsVertical: expandFill

                            JointConstraint { bone: "RightKnee"  minX: -140 maxX: 0 minY: -20 maxY: 20 }
                            JointConstraint { bone: "LeftKnee"   minX: -140 maxX: 0 minY: -20 maxY: 20 }
                            JointConstraint { bone: "RightElbow" minX: -145 maxX: 0 }
                            JointConstraint { bone: "LeftElbow"  minX: -145 maxX: 0 }
                        }

                        Timeline {
                            id: timeline
                            fps: 24
                            totalFrames: 120
                            sizeFlagsHorizontal: expandFill
                            customMinimumSize: 0, 160
                        }
                    }
                }
            }

            DockingContainer {
                id: rightDock
                dockSide: right
                fixedWidth: 240
                dragToRearrangeEnabled: true
                tabsRearrangeGroup: 1

                VBoxContainer {
                    id: bonesPanel
                    rightDock.title: "Bones"
                    padding: 4, 4, 4, 4
                    spacing: 4

                    Label { text: "Bones"  fontSize: 11  fontWeight: bold }

                    Tree {
                        id: boneTree
                        sizeFlagsHorizontal: expandFill
                        sizeFlagsVertical: expandFill
                        hideRoot: true
                    }
                }
            }
        }

        // ── Status bar ────────────────────────────────────────────────────────
        HBoxContainer {
            padding: 4, 2, 4, 2
            spacing: 6
            
            Label {
                id: statusLabel
                text: "No model loaded."
                sizeFlagsHorizontal: expandFill
                sizeFlagsVertical: shrinkCenter
            }
        }
    }
}
