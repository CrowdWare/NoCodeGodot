Window {
    title: "NoCodeRunner"
    minSize: 800,400
    pos: 448, 156 // centered
    size: 1024, 768

    Panel {
        anchors: top | left | bottom
        width: 320


        CodeEdit {
            text: "Here you can write code..."            
        }
    }

    Panel {
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