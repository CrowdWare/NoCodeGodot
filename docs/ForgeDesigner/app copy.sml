Window {
    id: mainWindow
    title: @Strings.windowTitle
    minSize: 800,400
    pos: 0, 0
    size: 1920, 1080
    extendToTitle: true

    // comment

    MenuBar {
        preferGlobalMenu: true

        PopupMenu {
            id: appMenu
            title: @Strings.menuAppTitle, "Default value"

            Item { id: about text: @Strings.menuAppAbout }
            Item { id: settings text: @Strings.menuAppSettings }
            Item { id: quit text: @Strings.menuAppQuit }
        }

        PopupMenu {
            id: file
            title: @Strings.menuFileTitle

            Item { id: saveAs text: @Strings.menuFileSaveAs }
        }
    }

    WindowDrag {
        id: titleDrag
        anchors: left | top | right
        top: 0
        height: 42
    }

    Markdown {
        id: caption
        top: 5
        left: 100
        width: 300
        height: 20
        mouseFilter: ignore
        text: @Strings.captionDemo
    }

     DockingHost {
        id: mainDockHost
        anchors: left | top | right | bottom
        gap: 8
        offsetTop: 42

        DockingContainer {
            id: farLeftDock
            dockSide: left
            fixedWidth: 300
            dragToRearrangeEnabled: true
            tabsRearrangeGroup: 1

            VBoxContainer {
                id: project
                farLeftDock.title: @Strings.tabProject
                Tree {
                    id: treeview
                    sizeFlagsHorizontal: expandFill
                    sizeFlagsVertical: expandFill
                    showGuides: false
                }
            }

            VBoxContainer {
                id: hierarchy
                farLeftDock.title: @Strings.tabHierarchy
                Tree {
                    id: hierarchyTree
                    sizeFlagsHorizontal: expandFill
                    sizeFlagsVertical: expandFill
                    showGuides: false
                }
            }
        }

        DockingContainer {
            id: centerDock
            dockSide: center
            flex: true
            closeable: false
            dragToRearrangeEnabled: true

            CodeEdit {
                id: codeEdit
                centerDock.title: @Strings.tabNew
                text: "Window {
    title: \"Test\"
}"
                syntax: "sml"
                //font: "appres://DeineFont.ttf"
                fontSize: 13
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
                rightDock.title: @Strings.tabMarkdown
                Markdown {
                    padding: 8,8,8,20
                    src: "res:/sample.md"
                }
            }

            VBoxContainer {
                id: preview
                rightDock.title: @Strings.tabPortrait
                Label { text: "Portrait Preview" }
            }

            VBoxContainer {
                id: profiler
                rightDock.title: @Strings.tabLandscape
                Label { text: "Landscape Preview" }
            }
        }
    }
}
