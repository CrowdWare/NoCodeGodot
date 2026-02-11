Window {
    title: "App Layout Demo"
    minSize: 640, 480
    layoutMode: app

    Panel {
        x: 8
        y: 8
        width: 240
        height: 464
        anchors: top | bottom | left

        Column {
            spacing: 8
            Label { text: "Navigation" }
            Button { text: "Dashboard" }
            Button { text: "Settings" }
        }
    }

    Panel {
        x: 256
        y: 8
        width: 376
        height: 464
        anchors: top | bottom | left | right

        Column {
            spacing: 10
            Label { text: "Content" }
            TextEdit {
                text: "Resize the window to see anchor behavior."
                height: 120
                multiline: true
            }
        }
    }

    Button {
        width: 140
        height: 36
        y: 420
        centerX: true
        text: "Centered Action"
    }
}
