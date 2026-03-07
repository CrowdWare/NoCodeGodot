SplashScreen {
    id: splashScreen
    size: 960, 480
    duration: 1000
    loadOnReady: "main.sml"

    TextureRect {
        id: logo
        src: "appRes:/assets/images/splash3d.png"
        width: 960
        height: 480
    }

    VBoxContainer {
        anchors: left | top | right | bottom
        padding: 40, 40, 40, 32

        /*Control { sizeFlagsVertical: expandFill }

        TextureRect {
            id: logo
            src: "appRes:/assets/images/logo.png"
            width: 350
            height: 175
            sizeFlagsHorizontal: shrinkCenter
        }*/

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
