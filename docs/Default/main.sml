Window {
    id: mainWindow
    title: @Strings.windowTitle
    minSize: 900, 580
    size: 960, 660
    extendToTitle: true

    // Drag area behind title bar content
    WindowDrag {
        anchors: left | top | right
        height: 40
    }

    VBoxContainer {
        anchors: left | top | right | bottom
        spacing: 0

        // ── Title Bar ────────────────────────────────────────────
        // height: 40 prevents the MarginContainer padding wrapper from inflating
        // the row. Horizontal spacing is handled by spacing: 8.
        HBoxContainer {
            id: titleBar
            height: 40
            spacing: 8

            HBoxContainer {
                spacing: 6
                mouseFilter: ignore
                padding: 0, 0, 0, 8

                TextureRect {
                    src: "appRes://logo.png"
                    width: 20
                    height: 20
                    shrinkH: true
                    shrinkV: true
                    mouseFilter: ignore
                }

                Label {
                    text: "Forge"
                    fontWeight: bold
                    mouseFilter: ignore
                }
            }

            LineEdit {
                id: searchBar
                sizeFlagsHorizontal: expandFill
                placeholderText: @Strings.searchPlaceholder
            }

            HBoxContainer {
                spacing: 4
                padding: 0, 8, 0, 0

                Button {
                    id: btnBell
                    text: "🔔"
                    shrinkH: true
                }

                Button {
                    id: btnUser
                    text: "●"
                    shrinkH: true
                }
            }
        }

        // ── Navigation Tabs ───────────────────────────────────────
        HBoxContainer {
            id: navBar
            spacing: 0

            Button { id: tabStart    text: @Strings.navStart    shrinkH: true }
            Button { id: tabLearn    text: @Strings.navLearn    shrinkH: true }
            Button { id: tabDiscover text: @Strings.navDiscover shrinkH: true }
            Button { id: tabUpdates  text: @Strings.navUpdates  shrinkH: true }

            Control { sizeFlagsHorizontal: expandFill }
        }

        // ── Content Area ──────────────────────────────────────────
        HBoxContainer {
            id: contentArea
            sizeFlagsVertical: expandFill
            spacing: 0

            // Left column
            VBoxContainer {
                id: leftCol
                sizeFlagsHorizontal: expandFill
                sizeFlagsVertical: expandFill
                spacing: 24
                padding: 24, 24, 24, 16

                // Create new project
                VBoxContainer {
                    id: createSection
                    spacing: 12

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

            VSeparator {}

            // Right column: News & Tips
            VBoxContainer {
                id: rightCol
                width: 280
                sizeFlagsVertical: expandFill
                spacing: 0
                padding: 16, 16, 16, 12

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
