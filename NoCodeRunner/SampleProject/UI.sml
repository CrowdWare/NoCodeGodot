Window {
    id: main
    title: "NoCodeRunner Sample"
    scaling: layout
    fillMaxSize: true

    Column {
        spacing: 12
        fillMaxSize: true

        Label {
            text: "Hello from file:/// UI.sml"
            fontSize: 26
            halign: 1
        }

        Row {
            spacing: 8

            Label {
                text: "Action test:"
            }

            Button {
                text: "Exit"
                action: closeQuery
            }
        }

        TextEdit {
            id: notes
            text: "This UI was loaded from a local SML file URL."
            width: 520
            height: 180
            multiline: true
        }

        Tabs {
            fillMaxSize: true
            Tab {
                title: "Overview"
                fillMaxSize: true
                Column {
                    spacing: 6
                    fillMaxSize: true
                    Label { text: "Tab 1 content" }
                    Button {
                        id: saveBtn
                        text: "Save"
                        action: save
                    }
                }
            }

            Tab {
                title: "Media"
                fillMaxSize: true
                Column {
                    spacing: 6
                    fillMaxSize: true
                    Label { text: "3D Viewport sample (.glb/.gltf imported as PackedScene)" }
                    Viewport3D {
                        id: hero
                        fillMaxSize: true
                        model: "res://SampleProject/PaladinIdle.glb"
                        cameraDistance: 4
                        lightEnergy: 1
                        playFirstAnimation: true
                    }

                    Label { text: "Video sample (res:// or user:// required)" }
                    Video {
                        width: 480
                        height: 270
                        autoplay: false
                        source: "res://SampleProject/sample.ogv"
                    }
                }
            }
        }
    }
}
