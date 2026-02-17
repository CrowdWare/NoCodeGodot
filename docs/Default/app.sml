Window {
    id: mainWindow
    title: "NoCodeRunner"
    minSize: 800,400
    pos: 0, 0
    size: 1920, 1080

    PanelContainer {
        
        size: 300, 400

        TabContainer {
            PanelContainer { title: "Project" }
            PanelContainer { title: "Hierarchy" }
        }
    }
    
    /*
    PanelContainer {
        id: leftPanel
        //anchors: top | left | bottom
        //width: 300
        size: 300, 400
        Button {text: "Click Me"}
        TabBar {
            TabContainer {
                title: "Project"

                Tree {
                    id: treeview
                    showGuides: false
                }
            }

            TabContainer {
                title: "Hierarchy"
            }
        }
    }
    */
/*
    PanelContainer {
        id: middlePanel
        anchors: top | bottom | right | left
        x: 300
        width: 300

        TabBar {
            TabContainer {
                title: "<New>"
        
                CodeEdit {
                    id: codeEdit
                    text: "Window { titel: \"Test\"}"     
                    syntax: "sml"   
                }    
            }
        }
    }

    PanelContainer {
        id: rightPanel
        anchors: top | right | bottom
        x: 600
        width: 600
        
        TabBar {
            TabContainer {
                title: "Desktop"
                Markdown {
                    
                    padding: 8,8,8,20
                    src: "res:/sample.md"
                }
            }
            TabContainer {
                title: "Mobile Landscape"
            }
            TabContainer {
                title: "Mobile Portrait"
            }
        }
    }
    */
}