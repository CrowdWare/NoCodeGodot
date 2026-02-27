NavTab {
    property tabId: id
    property text: "Tab"
    inheritance: Control

    shrinkH: true
    width: 120
    height: 44

    TextureButton {
        id: {tabId}
        textureNormal: "res://assets/images/button_normal.png"
        texturePressed: "res://assets/images/button_focused.png"
        textureHover: "res://assets/images/button_focused.png"
        width: 120
        height: 44
        ignoreTextureSize: true
        toggleMode: true
    }
    TextureRect {
        src: "appRes://logo.svg"
        top: 15
        left: 10
        width: 30
        height: 30
        mouseFilter: ignore
    }
    Label {
        top: 15
        left: 45
        text: {text}
        mouseFilter: ignore
    }
}