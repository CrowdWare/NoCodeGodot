Window {
    id: mainWindow
    title: "NoCodeRunner"
    minSize: 800,400
    pos: 0, 0
    size: 1920, 1080
    extendToTitle: true

    WindowDrag {
        id: titleDrag
        anchors: left | top | right
        top: 0
        height: 38
    }

    MenuBar {
        preferGlobalMenu: true

        PopupMenu {
            id: appMenu
            title: "NoCodeRunner"

            Item { id: about text: "About NoCodeRunner" }
            Item { id: settings text: "Settings" }
            Item { id: quit text: "Quit NoCodeRunner" }
        }

        PopupMenu {
            id: file
            title: "File"

            Item { id: saveAs text: "Save As..." }
        }
    }


    Markdown {
        top: 5
        left: 100
        width: 200
        height: 20
        text: "**NoCode** - Docking Demo"
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
                title: "Project"
                Tree {
                    id: treeview
                    sizeFlagsHorizontal: expandFill
                    sizeFlagsVertical: expandFill
                    showGuides: false
                } 
            }

            VBoxContainer {
                id: hierarchy
                title: "Hierarchy"
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
                title: "<New>"
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
                title: "Markdown"
                Markdown {        
                    padding: 8,8,8,20
                    src: "res:/sample.md"
                }
            }

            VBoxContainer {
                id: preview
                title: "Portrait"
                Label { text: "Portrait Preview" }
            }

            VBoxContainer {
                id: profiler
                title: "Landscape"
                Label { text: "Landscape Preview" }
            }
        }
    }
}