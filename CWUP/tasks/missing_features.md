# Caption Buttons
In Godot 4.x we can see that windows caption is located on the left side. In the center we can see buttons like 2D, 3D and on the right side there is a play buton, stop button and a dropdownlistbox.
I want the same possibility to place controls in the caption bar.
Also it would be nice to change the height of the caption bar.

# CodeEdit - Minimap
We need a property to anable the minimap of the CodeEditor.

# Dialog for Dock Position
Should look like the one in Godot.

# Dialog
We need Dialogs in general.

```qml
Dialog {
    modal: true
    title: "Save Query Dialog"

    Markdown {
        text: "# You have unsaved changes !
Do you want to safe the file?"
    }

    Button {
        text: "Cancel"
    }

    Button {
        text: "Ignore"
    }

    Button {
        text: "Save"
    }
}
```

```qml
AlertDialog {
    message: "You are low on disk space!"
}
```

