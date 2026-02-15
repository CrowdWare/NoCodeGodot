Window {
    id: mainWindow
    title: "NoCodeDesigner - Main"
    minSize: 800,400
    pos: 0, 0 
    size: 1920, 1080
    
    Panel {
        id: leftPanel1
        anchors: top | left | bottom
        width: 300

        Tabs {
            Tab {
                title: "Project"

                TreeView {
                    id: treeview
                    showGuides: false
                }
            }

            Tab {
                title: "Hierarchy"
            }
        }

    }

    Panel {
        id: middlePanel2
        anchors: top | bottom | right | left
        x: 300
        width: 300

        Tabs {
            Tab {
                title: "<New>"
        
                CodeEdit {
                    id: codeEdit
                    text: "Window { titel: \"Test\"}"     
                    syntax: "sml"   
                }    
            }
        }
    }
}