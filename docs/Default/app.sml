Window {
    title: "NoCodeRunner"
    minSize: 800,400
    size: 1024, 768

    Panel {
        anchors: top | left | bottom
        width: 320
    }

    Panel {
        anchors: top | right | bottom
        x: 600
        width: 600
        
        Markdown {
            padding: 8,8,8,20
            text: "# Title
## Subtitle
### Sub Sub
Lorem *ipsum* **dolor**  
Next Line
Noch ne Line
- Item 1
- Item 2
- Item 3
```
fun test() {
     println(\"Hello world\")
}
```
:smile:"
        }
    }
}