# Docking 
Like in Godot 4.6 I want to implement a panel docking.
It should be possible to undoc panels so that they can be move to another monitor.
The dockheader should display a button with 3 dots to open the docking menu, wehere you can decide left, far-left, right, far-right, bottom-left, bottom-far-left, bottom_right, bottom-far-right,floating, closed
The tabs can be repositioned and also be dragged into another dock-tab.
A closed dock shall be able to reopen via menu, so there should be a method to set the default layout back. Also save layout and load layout.
Docks are separated with a draggable splitter.

A floating panel will become its own Window with close, maximize and minimize. On close it will redock to the last position where its was before.

Icons while dragginf is hand if dock can be placed and a stop symbol if not.

## Areas
- left
- right
- far-left
- far-right
- bottom-left
- bottom-right
- bottom-far-left
- bottom-far-right
- center

```qml
DockSpace {
    id: editorDock
    allowFloating: true
    allowTabbing: true
    allowSplitting: true
    splitterSize: 6


    DockPanel {
        id: sceneTree
        title: "Scene"
        area: "left"
        floatable: true
        closable: true
    }

    DockPanel {
        id: inspector
        title: "Inspector"
        area: "right"
        floatable: true
        closable: true
    }

    DockPanel {
        id: viewport
        title: "undefined" // empty, unsafed file
        area: "center"
        floatable: false
    }
}
```