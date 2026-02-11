Page {
    layoutMode: document

    Column {
        scrollable: true
        scrollBarWidth: 8
        scrollBarVisibleOnScroll: true
        scrollBarFadeOutTime: 300

        Label { text: "Document Demo" fontSize: 26 }
        Label { text: "This page demonstrates document flow with scrolling." }

        Row {
            spacing: 8
            Label { text: "Row item A" }
            Label { text: "Row item B" }
            Label { text: "Row item C" }
        }

        Box {
            padding: 8,16
            Label { text: "Box overlay item #1" }
            Label { text: "Box overlay item #2" }
        }

        Label { text: "---" }
        Label { text: "Lorem ipsum dolor sit amet, consectetur adipiscing elit." }
        Label { text: "Sed do eiusmod tempor incididunt ut labore et dolore magna aliqua." }
        Label { text: "Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris." }
        Label { text: "Duis aute irure dolor in reprehenderit in voluptate velit esse." }
        Label { text: "Cillum dolore eu fugiat nulla pariatur." }
        Label { text: "Excepteur sint occaecat cupidatat non proident." }
        Label { text: "Sunt in culpa qui officia deserunt mollit anim id est laborum." }
    }
}
