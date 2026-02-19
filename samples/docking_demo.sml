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
            Item { id: viewFarLeftBottomPanel text: "Far Left Bottom" isChecked: true }
            Item { id: viewProject text: "Project" isChecked: true }
            Item { id: viewHierarchy text: "Hierarchy" isChecked: true }
            Item { id: viewSearch text: "Search" isChecked: true }
            Item { id: viewEditor text: "Editor" isChecked: true }
            Item { id: viewShader text: "Shader.gd" isChecked: true }
            Item { id: viewOutput text: "Output" isChecked: true }
            Item { id: viewPreview text: "Preview" isChecked: true }
            Item { id: viewProfiler text: "Profiler" isChecked: true }
            Item { id: viewLeftBottomPanel text: "Left Bottom" isChecked: true }
            Item { id: viewInspector text: "Inspector" isChecked: true }
            Item { id: viewRightBottomPanel text: "Right Bottom" isChecked: true }
            Item { id: viewConsole text: "Console" isChecked: true }
            Item { id: viewFarRightBottomPanel text: "Far Right Bottom" isChecked: true }
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
            heightPercent: 62
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
            minFixedHeight: 100
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
            fixedHeight: 420
            dragToRearrangeEnabled: true
            tabsRearrangeGroup: 1

            VBoxContainer {
                id: project
                title: "Project"
                Label { text: "Project explorer" }
            }

            VBoxContainer {
                id: hierarchy
                title: "Hierarchy"
                Label { text: "Scene hierarchy" }
            }

            VBoxContainer {
                id: search
                title: "Search"
                Label { text: "Search results" }
            }
        }

        DockingContainer {
            id: leftBottomDock
            dockSide: leftBottom
            fixedWidth: 280
            minFixedHeight: 120
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
            dragToRearrangeEnabled: true

            VBoxContainer {
                id: editor
                title: "Editor"
                Label { text: "Main work area" }
            }

            VBoxContainer {
                id: shader
                title: "Shader.gd"
                Label { text: "Code editor tab" }
            }

            VBoxContainer {
                id: output
                title: "Output"
                Label { text: "Logs / terminal / problems" }
            }
        }

        DockingContainer {
            id: rightDock
            dockSide: right
            fixedWidth: 360
            heightPercent: 68
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
                id: profiler
                title: "Profiler"
                Label { text: "Performance metrics" }
            }
        }

        DockingContainer {
            id: rightBottomDock
            dockSide: rightBottom
            fixedWidth: 360
            fixedHeight: 220
            minFixedHeight: 100
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
            heightPercent: 60
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
            minFixedHeight: 90
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
