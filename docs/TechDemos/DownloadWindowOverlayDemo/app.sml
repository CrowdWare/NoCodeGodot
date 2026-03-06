Window {
    id: techDemoWindow
    title: "TechDemo Window Overlay Progress"
    width: 800
    height: 480

    VBoxContainer {
        anchors: left | top | right | bottom
        padding: 24, 24, 24, 24

        Label {
            text: "Root type: Window"
        }

        Label {
            text: "When startup downloads are pending, progress is shown as runtime overlay."
        }

        Control { sizeFlagsVertical: expandFill }

        Label {
            text: "Use this demo to verify overlay behavior."
            horizontalAlignment: center
            sizeFlagsHorizontal: expandFill
        }
    }
}
