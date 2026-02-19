Window {
    id: dockingDemoWindow
    title: "Docking Demo (DockingHost + DockingContainer)"
    minSize: 900, 560
    size: 1440, 900

    MenuBar {
        id: dockMenu
        preferGlobalMenu: true

        PopupMenu {
            id: fileMenu
            title: "File"
            Item { id: saveLayout text: "Save Layout" }
            Item { id: loadLayout text: "Load Layout" }
            Item { id: resetLayout text: "Reset Layout" }
        }

        PopupMenu {
            id: viewMenu
            title: "View"
            Item { id: viewAssets text: "Assets" isChecked: true }
            Item { id: viewFarLeftBottom text: "Far Left Bottom" isChecked: true }
            Item { id: viewProject text: "Project" isChecked: true }
            Item { id: viewPreview text: "Preview" isChecked: true }
            Item { id: viewLeftBottom text: "Left Bottom" isChecked: true }
            Item { id: viewInspector text: "Inspector" isChecked: true }
            Item { id: viewRightBottom text: "Right Bottom" isChecked: true }
            Item { id: viewConsole text: "Console" isChecked: true }
            Item { id: viewFarRightBottom text: "Far Right Bottom" isChecked: true }
        }
    }

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
                id: assets
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
                id: farLeftBottomPanel
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
                id: project
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
                id: leftBottomPanel
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
                id: inspector
                title: "Inspector"
                Label { text: "Selection properties" }
            }

            VBoxContainer {
                id: preview
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
                id: rightBottomPanel
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
                id: console
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
                id: farRightBottomPanel
                title: "Far Right Bottom"
                Label { text: "Far right bottom dock" }
            }
        }
    }
}
