Window {
    id: mainWindow
    title: "NoCodeRunner"
    minSize: 800,400
    pos: 0, 0
    size: 1920, 1080

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
            title: "<New>"
        
            CodeEdit {
                id: codeEdit
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