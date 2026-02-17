Window {
    id: mainWindow
    title: "NoCodeRunner"
    minSize: 800,400
    pos: 0, 0
    size: 1920, 1080

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

    PanelContainer {
         anchors: left | top | bottom
         sizeFlagsVertical: expandFill
         width: 300

        TabContainer {
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
        offsetTop: 0
        offsetBottom: 0

        TabContainer {
            CodeEdit {
                id: codeEdit
                title: "<New>"
                text: "Window { titel: \"Test\"}"     
                syntax: "sml"   
            }    
        }
        
    }

    PanelContainer {
        anchors: right | top | bottom
        offsetLeft: -400
        offsetRight: 0
        offsetTop: 0
        offsetBottom: 0

        TabContainer {
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
}