Window {
    id: mainWindow
    title: "NoCodeDesigner"
    minSize: 800,400
    pos: 0, 0 // centered
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
        Menu {
            title: "View"
            id: viewMenu
            MenuItem { text: "Projekt" id: panelLeft isChecked: true }
            MenuItem { text: "Markdown" id: panelRight isChecked: true }
            MenuItem { text: "Floating Window" id: floating}
        }
    }

    DockSpace {
        id: editorDock
        allowFloating: true
        allowTabbing: true
        allowSplitting: true
        splitterSize: 6

        DockPanel {
            id: leftTop
            title: "Projekt"
            area: "left"
            width: 300

            TreeView {
                id: treeview
                showGuides: false
            }
        }

        DockPanel {
            id: viewport
            title: "Viewport"
            area: "center"
            closeable: false
            floatable: false
            dockable: false
            isDropTarget: false

            CodeEdit {
                id: codeEdit
                text: "Window { titel: \"Test\"}"     
                syntax: "sml"       
            }
        }

        DockPanel {
            id: rightTop
            title: "Markdown"
            area: "right"
            //width: 300

            Markdown {
                padding: 8,8,8,20
                src: "res:/sample.md"
            }
        }
    }
}