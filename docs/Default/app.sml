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

    Button { 
        top: 3
        left: 200
        text: "This is a control living in the caption"
    }

     DockingHost {
        id: mainDockHost
        anchors: left | top | right | bottom
        gap: 8
        offsetTop: 42

        DockingContainer {
            id: farLeftDock
            dockSide: left
            fixedWidth: 200
            heightPercent: 62
            dragToRearrangeEnabled: true
            tabsRearrangeGroup: 1

            VBoxContainer {
                id: assets
                title: "Project"
                Label { text: "Far left dock" }
            }
        }
    }
    /*
    PanelContainer {
        anchors: left | top | bottom
        sizeFlagsVertical: expandFill
        offsetTop: 38
        width: 300

        TabContainer {
            dragToRearrangeEnabled: true
            tabsRearrangeGroup: 1

            VBoxContainer { 
                title: "Project" 

                Tree {
                    id: treeview
                    sizeFlagsHorizontal: expandFill
                    sizeFlagsVertical: expandFill
                    showGuides: false
                } 
            }
            PanelContainer { title: "Hierarchy" }
        }
    }

    PanelContainer {
        anchors: left | right | top | bottom
        offsetLeft: 300
        offsetRight: -400
        offsetTop: 38
        offsetBottom: 0

        TabContainer {
            CodeEdit {
                id: codeEdit
                title: "<New>"
                text: "Window { titel: \"Test\"}"     
                syntax: "sml"
                //font: "appres://DeineFont.ttf"   
                fontSize: 13
            }    
        }
        
    }

    PanelContainer {
        anchors: right | top | bottom
        offsetLeft: -400
        offsetRight: 0
        offsetTop: 38
        offsetBottom: 0

        TabContainer {
            dragToRearrangeEnabled: true
            tabsRearrangeGroup: 1
            VBoxContainer { 
                title: "Desktop"
                Markdown {        
                    padding: 8,8,8,20
                    src: "res:/sample.md"
                }
            }
            PanelContainer { title: "Landscape" }
            PanelContainer { title: "Portrait" }
        }
    }
    */
}