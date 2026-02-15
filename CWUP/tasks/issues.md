# Issues
The window taht we create via funktion is sms does not have the same layout mode than the standard window.

var sml = fs.readText("Default/main.sml")
var created = ui.CreateWindow(sml)

Window {

    Panel {
        width: 300
    }
    Panel {
        x: 300
    }
}

Both panels overlap.