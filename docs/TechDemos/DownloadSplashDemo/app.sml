SplashScreen {
    id: techDemoSplash
    size: 640, 360
    duration: 3500
    loadOnReady: "next.sml"

    VBoxContainer {
        anchors: left | top | right | bottom
        padding: 40, 32, 40, 32

        Control { sizeFlagsVertical: expandFill }

        Label {
            text: "TechDemo: Splash download progress"
            horizontalAlignment: center
            sizeFlagsHorizontal: expandFill
        }

        Label {
            text: "Runner uses this embedded ProgressBar while syncing assets."
            horizontalAlignment: center
            sizeFlagsHorizontal: expandFill
        }

        Control { height: 8 }

        ProgressBar {
            id: downloadProgress
            min: 0
            max: 100
            value: 0
            showPercentage: true
            sizeFlagsHorizontal: expandFill
            visible: false
        }
    }
}
