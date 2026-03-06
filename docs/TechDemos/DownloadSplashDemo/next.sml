Window {
    title: "TechDemo Splash Complete"
    width: 640
    height: 360

    VBoxContainer {
        anchors: left | top | right | bottom
        padding: 24, 24, 24, 24

        Label {
            text: "SplashScreen demo completed."
        }

        Label {
            text: "If downloads were pending, progress was shown in the embedded bar."
        }
    }
}
