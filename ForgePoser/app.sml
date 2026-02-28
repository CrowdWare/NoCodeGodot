SplashScreen {
    id: splashScreen
    size: 800, 600
    duration: 800
    loadOnReady: "main.sml"

    VBoxContainer {
        anchors: left | top | right | bottom
        padding: 60, 60, 60, 40

        Control { sizeFlagsVertical: expandFill }

        Label {
            text: "ForgePoser"
            sizeFlagsHorizontal: shrinkCenter
            fontSize: 36
            fontWeight: bold
        }

        Label {
            text: "Animation Editor"
            sizeFlagsHorizontal: shrinkCenter
            fontSize: 16
        }

        Control { sizeFlagsVertical: expandFill }

        Label {
            id: statusLabel
            text: "Loading..."
            sizeFlagsHorizontal: shrinkCenter
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
