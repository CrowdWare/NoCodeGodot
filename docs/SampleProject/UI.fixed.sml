Window {
    id: mainFixed
    title: "NoCodeRunner Fixed Scaling Sample"
    scaling: fixed
    designSize: 1152, 648
    fillMaxSize: true

    Column {
        spacing: 12
        fillMaxSize: true

        Label {
            text: "Fixed mode: SubViewport render at 1152x648"
            fontSize: 26
            halign: 1
        }

        Row {
            spacing: 8
            Label { text: "All UI and fonts should scale proportionally." }
            Button {
                text: "Exit"
                action: closeQuery
            }
        }

        TextEdit {
            text: "Resize the window and inspect [fixed] logs in output."
            width: 520
            height: 180
            multiline: true
        }
    }
}