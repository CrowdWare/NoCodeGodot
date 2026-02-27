# UserDefined Controls
In the following sample we user a similar Control and childs multiple times.
It would be nice if we can can declare such a Control ones and resuse it somewehere else.

We should be able to define it in the same .sml or in a separate .sml and imclude it for rendering.
QML which is the idea giver fpr SML is doing that already.

It can also be usefull, when an included Control has got its own .sms script in order to be able to handle event. Naming conventions like in all other SMLs.
<name>.sml
<name>.sms

```qml
Control {
    shrinkH: true
    width: 120
    height: 44

                TextureButton { 
                    id: tabStart 
                    textureNormal: "res://assets/images/button_normal.png" 
                    texturePressed: "res://assets/images/button_focused.png"
                    shrinkH: true 
                    toggleMode: true
                }
                TextureRect {
                    src: "appRes://logo.svg"
                    top: 15
                    left: 10
                    width: 30
                    height: 30
                }
                Label {
                    top: 15
                    left: 45
                    text: "Start"
                }
            }
            Control {
                shrinkH: true
                width: 120
                height: 44

                TextureButton { 
                    id: tabLearn
                    textureNormal: "res://assets/images/button_normal.png" 
                    texturePressed: "res://assets/images/button_focused.png"
                    shrinkH: true 
                    toggleMode: true
                }
                TextureRect {
                    src: "appRes://logo.svg"
                    top: 15
                    left: 10
                    width: 30
                    height: 30
                }
                Label {
                    top: 15
                    left: 45
                    text: "Learn"
                }
            }
```