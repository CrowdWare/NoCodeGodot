Window {
    id: mainWindow
    title: "NoCodeRunner"
    minSize: 800,400
    pos: 448, 156 // centered
    size: 1024, 768

    Panel {
        id: leftPanel
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
        id: middlePanel
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

    Panel {
        id: rightPanel
        anchors: top | right | bottom
        x: 600
        width: 600
        
        Tabs {
            Tab {
                title: "Desktop"
                Markdown {
                    layoutMode: document
                    padding: 8,8,8,20
                    src: "res:/sample.md"
                }
            }
            Tab {
                title: "Mobile Landscape"
            }
            Tab {
                title: "Mobile Portrait"
            }
        }
    }
}