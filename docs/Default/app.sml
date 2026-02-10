Window {
    title: "NoCodeRunner"

    Column {
        spacing: 6
        fillMaxSize: true
        
        Label { text: "Default Content" }
        Viewport3D {
            id: hero
            model: "res://assets/models/Opa.glb"
            animation: "res://assets/models/Idle.glb"
            cameraDistance: 2
            lightEnergy: 1
            playAnimation: 1
            playLoop: true
        }

        Label { text: "Animation Controls" }
        Row {
            spacing: 8
            
            Button {
                text: "Play"
                action: animPlay
                clicked: hero
            }
            Button {
                text: "Stop"
                action: animStop
                clicked: hero
            }
            Button {
                text: "Rewind"
                action: animRewind
                clicked: hero
            }
        }

        Row {
            spacing: 8
            
            Label { text: "Scrub" }
            Slider {
                width: 360
                min: 0
                max: 100
                step: 1
                value: 0
                action: animScrub
                clicked: hero
            }
        }

        Label { text: "Perspective Presets" }
        Row {
            spacing: 8

            Button {
                text: "Near"
                action: perspectiveNear
                clicked: hero
            }
            Button {
                text: "Default"
                action: perspectiveDefault
                clicked: hero
            }
            Button {
                text: "Far"
                action: perspectiveFar
                clicked: hero
            }
        }

        Label { text: "Camera Interaction" }
        Row {
            spacing: 8
            
            Button {
                text: "Zoom In"
                action: zoomIn
                clicked: hero
            }
            Button {
                text: "Zoom Out"
                action: zoomOut
                clicked: hero
            }
            Button {
                text: "Reset View"
                action: cameraReset
                clicked: hero
            }
        }        
    }
}