SplashScreen {
    id: splashScreen
    size: 640, 480
    duration: 3000
    loadOnReady: "main.sml"

    VBoxContainer {
        anchors: left | top | right | bottom
        padding: 40, 40, 40, 32

        HBoxContainer {
            TextureRect {
                id: logo
                src: "res://logo.svg"
                width: 72
                height: 72
                shrinkH: true
                shrinkV: true
            }
            VBoxContainer {
                padding: 16
                Label { 
                    id: appName    
                    text: "CrowdWare" 
                    color: "#4fc3f7" 
                    fontSize: 45
                    //fontWeight: 400
                }
                Label { 
                    id: appTitle 
                    fontSize: 35  
                    text: "Forge" 
                }
                Label { id: appTagline text: "By the crowd. For the crowd." }
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
