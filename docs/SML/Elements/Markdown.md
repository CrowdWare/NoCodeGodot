# Markdown

## Inheritance

[Markdown](Markdown.md) → [Control](Control.md) → [CanvasItem](CanvasItem.md) → [Node](Node.md) → [Object](Object.md)

## Properties

This page lists **only properties declared by `Markdown`**.
Inherited properties are documented in: [Control](Control.md)

| Godot Property | SML Property | Type | Default |
|-|-|-|-|
| id | id | identifier | — |
| padding | padding | int / int,int / int,int,int,int | 0 |
| text | text | string | "" |
| src | src | string | "" |

> Note: `text` and `src` are alternative sources. Use one of them.

### Examples

```sml
Markdown { padding: 8,8,8,20; text: "# Header" }
Markdown { padding: 8; src: "res:/sample.md" }
```

## Events

This page lists **only signals declared by `Markdown`**.
Inherited signals are documented in: [Control](Control.md)

| Godot Signal | SMS Event | Params |
|-|-|-|
