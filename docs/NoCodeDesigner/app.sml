Window {
    id: mainWindow
    title: "NoCodeDesigner"
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