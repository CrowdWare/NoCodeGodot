SplashScreen {
    id: splashScreen
    size: 640, 480
    duration: 3000
    loadOnReady: "main.sml"

    VBoxContainer {
        anchors: left | top | right | bottom
        padding: 40, 40, 40, 32

        Control { sizeFlagsVertical: expandFill }

        TextureRect {
            id: logo
            src: "appRes://logo.png"
            width: 350
            height: 175
            sizeFlagsHorizontal: shrinkCenter
        }

        Control { sizeFlagsVertical: expandFill }

        Label {
            id: statusLabel
            text: "Loading assets..."
            visible: false
        }
        ProgressBar {
            id: downloadProgress
            min: 0
            max: 100
            value: 0
            showPercentage: false
            sizeFlagsHorizontal: expandFill
            visible: false
        }
    }
}
