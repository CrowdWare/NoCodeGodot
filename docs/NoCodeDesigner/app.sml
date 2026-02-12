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

        TreeView {
            id: treeview
            showGuides: false

            Item { 
                text: "Root" 
                expanded: true

                Item {
                    id: 1
                    text: "Branch 1"
 
                    Item {
                        id: 11
                        text: "Branch 1.1"
                        icon: "res:/assets/images/document.png"

                        Toggle {
                            id: showObject
                            imageOn: "res:/assets/images/eye_open.png"
                            imageOff: "res:/assets/images/eye_closed.png"
                        }
                    }
                }
                Item {
                    id: 2
                    text: "Branch 2"
                }
                Item {
                    id: 3
                    text: "Branch 3"
                }
            }
        }
    }

    Panel {
        id: middlePanel
        anchors: top | bottom | right | left
        x: 300
        width: 300

        CodeEdit {

            text: "Window { titel: \"Test\"}"     
            syntax: "sml"       
        }
    }

    Panel {
        id: rightPanel
        anchors: top | right | bottom
        x: 600
        width: 600
        
        Markdown {
            layoutMode: document
            padding: 8,8,8,20
            src: "res:/sample.md"
        }
    }
}