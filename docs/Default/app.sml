Window {
    id: mainWindow
    title: "ForgeRunner"
    titleKey: "window.main.title"
    minSize: 800,400
    pos: 0, 0
    size: 1920, 1080
    extendToTitle: true

    MenuBar {
        preferGlobalMenu: true

        PopupMenu {
            id: appMenu
            title: "ForgeRunner"
            titleKey: "menu.app.title"

            Item { id: about text: "About ForgeRunner" textKey: "menu.app.about" }
            Item { id: settings text: "Settings" textKey: "menu.app.settings" }
            Item { id: quit text: "Quit ForgeRunner" textKey: "menu.app.quit" }
        }

        PopupMenu {
            id: file
            title: "File"
            titleKey: "menu.file.title"

            Item { id: saveAs text: "Save As..." textKey: "menu.file.saveAs" }
        }
    }

    // must be placed after title to stay clickable
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
        text: "**Forge** - Docking Demo"
        textKey: "caption.demo"
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
                titleKey: "tab.project"
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
                titleKey: "tab.hierarchy"
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
                titleKey: "tab.new"
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
                titleKey: "tab.markdown"
                Markdown {        
                    padding: 8,8,8,20
                    src: "res:/sample.md"
                }
            }

            VBoxContainer {
                id: preview
                title: "Portrait"
                titleKey: "tab.portrait"
                Label { text: "Portrait Preview" }
            }

            VBoxContainer {
                id: profiler
                title: "Landscape"
                titleKey: "tab.landscape"
                Label { text: "Landscape Preview" }
            }
        }
    }
}