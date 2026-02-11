Window {
    title: "NoCodeRunner - Test"
    minSize: 600,400
    //size: 700, 500

    Panel {
        anchors: top | left | bottom
        width: 320
    }

    Panel {
        anchors: top | right | bottom
        x: 600
        width: 600
        padding: 8,8,8,8
        Markdown {
            text: "# Title
## Subtitle
### Sub Sub
Lorem *ipsum* **dolor**  
Next Line
Noch ne Line
- Item 1
- Item 2
- Item 3
:smile:"
        }
    }
}