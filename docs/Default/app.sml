Fonts {
    Sans-Bold:     "appRes://assets/fonts/SansBlack.ttf"
    Inter-Regular: "appRes://assets/fonts/Inter_18pt-Regular.ttf"
    Inter-Bold:    "appRes://assets/fonts/Inter_18pt-Bold.ttf"
}

SplashScreen {
    id: splashScreen
    size: 640, 480
    duration: 3000
    loadOnReady: "main.sml"

    VBoxContainer {
        anchors: left | top | right | bottom
        padding: 40, 40, 40, 32

        HBoxContainer {
            VBoxContainer {
                shrinkH: true
                padding: 30, 0, 0, 0
                TextureRect {
                    id: logo
                    src: "appRes://logo.svg"
                    width: 72
                    height: 72
                    offsetTop: 30
                    shrinkH: true
                    shrinkV: true
                }
            }
            VBoxContainer {
                padding: 16
                Label { 
                    id: appName    
                    text: "CrowdWare" 
                    color: "#4fc3f7" 
                    fontFace: "Sans"
                    fontSize: 45
                    fontWeight: bold
                }
                Label { 
                    id: appTitle 
                    fontSize: 35
                    fontFace: "Sans"
                    fontWeight: bold
                    text: "Forge" 
                }
                Label { 
                    id: appTagline 
                    fontFace: "Inter"
                    fontSize: 18
                    fontWeight: regular
                    text: "By the crowd. For the crowd." 
                }
            }
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
