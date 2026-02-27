Window {
    id: mainWindow
    title: @Strings.windowTitle
    minSize: 900, 580
    size: 960, 660
    extendToTitle: true

    // Drag area behind title bar content
    WindowDrag {
        anchors: left | top | right
        top: 0
        height: 55
    }

    TextureRect {
        src: "appRes://logo.svg"
        top: 13
        left: 70
        width: 30
        height: 30
        shrinkH: true
        shrinkV: true
        mouseFilter: ignore
    }

    Label {
        text: "Forge"
        top: 13
        left: 110
        fontWeight: bold
        fontSize: 27
        mouseFilter: ignore
    }

    LineEdit {
        id: searchBar
        anchors: left | top | right
        offsetTop: 13
        offsetLeft: 200
        offsetRight: -200
        placeholderText: @Strings.searchPlaceholder
    }

    Button {
        id: btnUser
        anchors: top | right
        offsetTop: 13
        offsetLeft: -122
        offsetRight: -55
        text: "● 1000"
        
    }

    Button {
        id: btnBell
        anchors: top | right
        offsetTop: 13
        offsetLeft: -45
        offsetRight: -32
        text: "🔔"
    }

    VBoxContainer {
        anchors: left | top | right | bottom
        offsetTop: 60
        spacing: 0

        // ── Navigation Tabs ───────────────────────────────────────
        HBoxContainer {
            id: navBar
            spacing: 0
            padding: 0,0,0,20
            borderColor: "#242736"
            borderTop: 1
            borderBottom: 1
            height: 59

            ui.NavTab { tabId: tabStart text: "Start"}
            ui.NavTab { tabId: tabLearn text: "Learn"}
            ui.NavTab { tabId: tabDiscover text: "Discover"}
            ui.NavTab { tabId: tabUpdates text: "Updates"}
            Control { sizeFlagsHorizontal: expandFill }
        }

        // ── Content Area ──────────────────────────────────────────
        HBoxContainer {
            id: contentArea
            padding: 20,20,20,20
            spacing: 0

            VBoxContainer {
                id: createSection
                sizeFlagsHorizontal: expandFill
                sizeFlagsVertical: expandFill
                spacing: 24
                padding: 24, 24, 24, 16
                elevation: raised

                // Create new project
         

                    Label {
                        text: @Strings.headCreateProject
                        fontSize: 18
                        fontWeight: bold
                    }

                    HBoxContainer {
                        id: actionButtons
                        spacing: 8

                        Button { id: btnNewProject text: @Strings.btnNewProject  shrinkH: true }
                        Button { id: btnDesigner   text: @Strings.btnDesigner    shrinkH: true }
                        Button { id: btnOpen       text: @Strings.btnOpenProject  shrinkH: true }
                        Button { id: btnExplore    text: @Strings.btnExplore     shrinkH: true }
                    }
                
            }
        }

        HBoxContainer {
            id: contentArea2
            padding: 20,20,20,20
            sizeFlagsVertical: expandFill
            spacing: 0
            

            // Left column
            VBoxContainer {
                id: leftCol2
                sizeFlagsHorizontal: expandFill
                sizeFlagsVertical: expandFill
                spacing: 24
                padding: 24, 24, 24, 24
                elevation: raised

                // Recommended templates
                VBoxContainer {
                    id: templatesSection
                    spacing: 12

                    Label {
                        text: @Strings.headTemplates
                        fontSize: 16
                        fontWeight: bold
                    }

                    HBoxContainer {
                        id: templateCards
                        spacing: 12

                        // Card: UI Toolkit
                        VBoxContainer {
                            id: cardUiToolkit
                            sizeFlagsHorizontal: expandFill
                            bgColor: "#1E2030"
                            borderColor: "#2E3250"
                            borderWidth: 1
                            borderRadius: 6
                            spacing: 0

                            TextureRect {
                                src: "appRes://assets/images/document.png"
                                height: 120
                                sizeFlagsHorizontal: expandFill
                            }

                            VBoxContainer {
                                spacing: 4
                                padding: 8, 8, 8, 8

                                Label {
                                    text: @Strings.cardUiToolkitTitle
                                    fontWeight: bold
                                }

                                Label {
                                    text: @Strings.cardUiToolkitDesc
                                    fontSize: 11
                                }
                            }
                        }

                        // Card: RPG Adventure
                        VBoxContainer {
                            id: cardRpg
                            sizeFlagsHorizontal: expandFill
                            bgColor: "#1E2030"
                            borderColor: "#2E3250"
                            borderWidth: 1
                            borderRadius: 6
                            spacing: 0

                            TextureRect {
                                src: "appRes://assets/images/document.png"
                                height: 120
                                sizeFlagsHorizontal: expandFill
                            }

                            VBoxContainer {
                                spacing: 4
                                padding: 8, 8, 8, 8

                                Label {
                                    text: @Strings.cardRpgTitle
                                    fontWeight: bold
                                }

                                Label {
                                    text: @Strings.cardRpgDesc
                                    fontSize: 11
                                }
                            }
                        }
                    }
                }
            }


            // Right column: News & Tips
            VBoxContainer {
                id: rightCol
                sizeFlagsVertical: expandFill
                spacing: 0
                elevation: raised
                padding: 24, 24, 24, 24

                Label {
                    text: @Strings.headNews
                    fontSize: 14
                    fontWeight: bold
                }

                Control { height: 12 }

                VBoxContainer {
                    id: news1
                    spacing: 2

                    Label { text: @Strings.news1Title fontWeight: bold fontSize: 13 }
                    Label { text: @Strings.news1Meta  fontSize: 11 }
                }

                HSeparator {}

                VBoxContainer {
                    id: news2
                    spacing: 2

                    Label { text: @Strings.news2Title fontWeight: bold fontSize: 13 }
                    Label { text: @Strings.news2Meta  fontSize: 11 }
                }

                HSeparator {}

                VBoxContainer {
                    id: news3
                    spacing: 2

                    Label { text: @Strings.news3Title fontWeight: bold fontSize: 13 }
                    Label { text: @Strings.news3Meta  fontSize: 11 }
                }

                Control { sizeFlagsVertical: expandFill }

                LinkButton {
                    id: btnMoreNews
                    text: @Strings.newsMore
                    shrinkH: true
                }
            }
        }

        // ── Status Bar ────────────────────────────────────────────
        HBoxContainer {
            id: statusBar
            spacing: 6
            padding: 6, 6, 6, 6

            Label {
                text: "●"
                color: "#4CAF50"
                fontSize: 10
            }

            Label {
                text: @Strings.statusOnline
                fontSize: 11
            }
        }
    }
}
