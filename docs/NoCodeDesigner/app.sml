Window {
    id: mainWindow
    title: "NoCodeDesigner"
    minSize: 800,400
    pos: 448, 156 // centered
    size: 1024, 768

    MenuBar {
        id: mainMenu
        preferGlobalMenu: true

        Menu {
            id: appMenu
            title: "Programm" 

            MenuItem { text: "About NoCodeDesigner"  id: about }
            MenuItem { text: "Settings" id: settings}
            Separator {}
            MenuItem { text: "Quit NoCodeDesigner" id: quit shortcut: "Cmd+Q" }
        }

        Menu { 
            title: "File"  
            MenuItem { text: "New"  id: newFile }
            MenuItem { text: "Open"  id: openFile }
            MenuItem { text: "Save"  id: saveFile }
            MenuItem { text: "Save As"  id: saveFileAs }
        }
        Menu { title: "Edit"  }
        Menu { title: "View"  }
    }

    Panel {
        id: leftPanel
        anchors: top | left | bottom
        width: 300

        TreeView {
            id: treeview
            showGuides: false
        }
    }

    Panel {
        id: middlePanel
        anchors: top | bottom | right | left
        x: 300
        width: 300

        CodeEdit {
            id: codeEdit
            text: "Window { titel: \"Test\"}"     
            syntax: "sml"       
        }
    }

    Panel {
        id: rightPanel
        anchors: top | right | bottom
        x: 600
        width: 600
        
        Markdown {
            layoutMode: document
            padding: 8,8,8,20
            src: "res:/sample.md"
        }
    }
}