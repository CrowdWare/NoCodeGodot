# Mainmenu
We want to define the Mainmenu also via SML.
Below is a sample.
If we are on macOS and preferGlobalMenu is set to true, then the appMenu shall be merged.
Id about, and quit are already on macOS. Here we only need to map the id to the user defined script.
The settings menu shall be inserted in the appMenu from macOS.

```qml
MenuBar {
    id: mainMenu
    preferGlobalMenu: true // for macOS

    Menu {
        id: appMenu // point the appMenu on macOS 
        title: "Program" // Will be overridden  by macOS

        MenuItem { text: "About AppName"  id: about }
        Separator {}
        MenuItem { text: "Settings id: settings}
        Separator {}
        MenuItem { text: "Close AppName" id: quit shortcut: "Cmd+Q" } // will be overridden on macOS
    }

    Menu { title: "File"  ... }
    Menu { title: "Edit" ... }
    Menu { title: "View" ... }
}
```