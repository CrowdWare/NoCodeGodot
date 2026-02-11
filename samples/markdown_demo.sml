Window {
    title: "Markdown Demo"
    minSize: 900, 600
    layoutMode: document

    Markdown {
        scrollable: true
        text: "
# Markdown Demo
{
    id: title
}

This paragraph supports :rocket: emojis and **bold** / *italic* markers.
Line with hard break  
continues on next line.

## List Section
- First item
- Second item

![Sample Image](res://icon.svg)
{
    align: center
    size: 256, 256
}

```csharp
Console.WriteLine(\"Hello Markdown\");
```
"
    }
}
