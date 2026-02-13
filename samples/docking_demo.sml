Window {
    title: "Docking Demo"
    minSize: 1000, 700
    size: 1280, 800

    DockSpace {
        id: editorDock
        allowFloating: true
        allowTabbing: true
        allowSplitting: true
        splitterSize: 6

        DockPanel {
            id: farLeftTop
            title: "Far Left"
            area: "far-left"

            Column {
                spacing: 8
                Label { text: "Far Left (Top)" }
                Label { text: "Slot: far-left" }
            }
        }

        DockPanel {
            id: leftTop
            title: "Left"
            area: "left"

            Column {
                spacing: 8
                Label { text: "Left (Top)" }
                Label { text: "Slot: left" }
            }
        }

        DockPanel {
            id: rightTop
            title: "Right"
            area: "right"

            Column {
                spacing: 8
                Label { text: "Right (Top)" }
                Label { text: "Slot: right" }
            }
        }

        DockPanel {
            id: viewport
            title: "Viewport"
            area: "center"

            Column {
                spacing: 8
                Label { text: "Viewport" }
                Label { text: "Use the 3-dot menu in panel headers to move/floating/close docks." }
            }
        }

        DockPanel {
            id: farRightTop
            title: "Far Right"
            area: "far-right"

            Column {
                spacing: 4
                Label { text: "Far Right (Top)" }
                Label { text: "Slot: far-right" }
            }
        }

        DockPanel {
            id: farLeftBottom
            title: "Far Left Bottom"
            area: "bottom-far-left"

            Column {
                spacing: 4
                Label { text: "Far Left (Bottom)" }
                Label { text: "Slot: bottom-far-left" }
            }
        }

        DockPanel {
            id: leftBottom
            title: "Left Bottom"
            area: "bottom-left"

            Column {
                spacing: 4
                Label { text: "Left (Bottom)" }
                Label { text: "Slot: bottom-left" }
            }
        }

        DockPanel {
            id: rightBottom
            title: "Right Bottom"
            area: "bottom-right"

            Column {
                spacing: 4
                Label { text: "Right (Bottom)" }
                Label { text: "Slot: bottom-right" }
            }
        }

        DockPanel {
            id: farRightBottom
            title: "Far Right Bottom"
            area: "bottom-far-right"

            Column {
                spacing: 4
                Label { text: "Far Right (Bottom)" }
                Label { text: "Slot: bottom-far-right" }
            }
        }
    }
}
