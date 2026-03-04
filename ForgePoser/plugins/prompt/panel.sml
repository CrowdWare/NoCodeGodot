VBoxContainer {
    id: pluginPromptPanel
    padding: 4, 4, 4, 4
    spacing: 4

    Label { text: "Prompt (Plugin)" fontSize: 11 fontWeight: bold }

    TextEdit {
        id: pluginPromptEditor
        sizeFlagsHorizontal: expandFill
        sizeFlagsVertical: expandFill
        customMinimumSize: 0, 120
    }

    Label { text: "Negative Prompt" fontSize: 10 }
    LineEdit {
        id: pluginNegativePromptEdit
        sizeFlagsHorizontal: expandFill
        placeholderText: "Optional negative prompt"
    }

    Label { text: "Style Image" fontSize: 10 }
    HBoxContainer {
        spacing: 4
        LineEdit {
            id: pluginStyleImagePathEdit
            sizeFlagsHorizontal: expandFill
            placeholderText: "Path to style image"
        }
        Button { id: pluginBtnPickStyleImage text: "..." customMinimumSize: 28, 24 }
    }

    Label { text: "Extra Image" fontSize: 10 }
    HBoxContainer {
        spacing: 4
        LineEdit {
            id: pluginExtraImagePathEdit
            sizeFlagsHorizontal: expandFill
            placeholderText: "Path to extra image"
        }
        Button { id: pluginBtnPickExtraImage text: "..." customMinimumSize: 28, 24 }
    }

    HBoxContainer {
        spacing: 4
        Button { id: pluginBtnAssemblyCall text: "Run Plugin Function" sizeFlagsHorizontal: expandFill }
    }
}
