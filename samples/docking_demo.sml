Window {
    id: dockingDemoWindow
    title: "Docking Demo (DockingHost + DockingContainer)"
    minSize: 900, 560
    size: 1440, 900

    DockingHost {
        id: mainDockHost
        anchors: left | top | right | bottom
        gap: 8

        DockingContainer {
            id: farLeftDock
            dockSide: farLeft
            fixedWidth: 200
            dragToRearrangeEnabled: true
            tabsRearrangeGroup: 1

            VBoxContainer {
                title: "Assets"
                Label { text: "Far left dock" }
            }
        }

        DockingContainer {
            id: farLeftBottomDock
            dockSide: farLeftBottom
            fixedWidth: 200
            dragToRearrangeEnabled: true
            tabsRearrangeGroup: 1

            VBoxContainer {
                title: "Far Left Bottom"
                Label { text: "Far left bottom dock" }
            }
        }

        DockingContainer {
            id: leftDock
            dockSide: left
            fixedWidth: 280
            dragToRearrangeEnabled: true
            tabsRearrangeGroup: 1

            VBoxContainer {
                title: "Project"
                Label { text: "Project explorer" }
            }

            VBoxContainer {
                title: "Hierarchy"
                Label { text: "Scene hierarchy" }
            }

            VBoxContainer {
                title: "Search"
                Label { text: "Search results" }
            }
        }

        DockingContainer {
            id: leftBottomDock
            dockSide: leftBottom
            fixedWidth: 280
            dragToRearrangeEnabled: true
            tabsRearrangeGroup: 1

            VBoxContainer {
                title: "Left Bottom"
                Label { text: "Left bottom dock" }
            }
        }

        DockingContainer {
            id: centerDock
            dockSide: center
            flex: true
            closeable: false
            dragToRearrangeEnabled: false

            VBoxContainer {
                title: "Editor"
                Label { text: "Main work area" }
            }

            VBoxContainer {
                title: "Shader.gd"
                Label { text: "Code editor tab" }
            }

            VBoxContainer {
                title: "Output"
                Label { text: "Logs / terminal / problems" }
            }
        }

        DockingContainer {
            id: rightDock
            dockSide: right
            fixedWidth: 360
            dragToRearrangeEnabled: true
            tabsRearrangeGroup: 1

            VBoxContainer {
                title: "Inspector"
                Label { text: "Selection properties" }
            }

            VBoxContainer {
                title: "Preview"
                Label { text: "Preview pane" }
            }

            VBoxContainer {
                title: "Profiler"
                Label { text: "Performance metrics" }
            }
        }

        DockingContainer {
            id: rightBottomDock
            dockSide: rightBottom
            fixedWidth: 360
            dragToRearrangeEnabled: true
            tabsRearrangeGroup: 1

            VBoxContainer {
                title: "Right Bottom"
                Label { text: "Right bottom dock" }
            }
        }

        DockingContainer {
            id: farRightDock
            dockSide: farRight
            fixedWidth: 220
            closeable: false
            dragToRearrangeEnabled: true
            tabsRearrangeGroup: 1

            VBoxContainer {
                title: "Console"
                Label { text: "Far right dock" }
            }
        }

        DockingContainer {
            id: farRightBottomDock
            dockSide: farRightBottom
            fixedWidth: 220
            closeable: false
            dragToRearrangeEnabled: true
            tabsRearrangeGroup: 1

            VBoxContainer {
                title: "Far Right Bottom"
                Label { text: "Far right bottom dock" }
            }
        }
    }
}
