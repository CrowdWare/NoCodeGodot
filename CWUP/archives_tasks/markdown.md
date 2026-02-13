# Implement Markdown Widget (Document Mode)

## Goal

Implement a Markdown widget for SML that:
	•	runs entirely in document mode
	•	generates standard document widgets (Label, Image, Row, Box, Spacer)
	•	does not interpret HTML
	•	supports a controlled subset of Markdown
	•	supports SML-style property blocks

Markdown must not introduce its own layout system.

⸻

## 1) Architecture

### 1.1 Inheritance
	•	Markdown inherits from Column
	•	layoutMode = document (fixed)
	•	supports scrollable

### 1.2 Rendering Model

Markdown:
```
Input text → Parse → Generate document widgets → Insert as children
```

### 1.3 Markdown Properties

Markdown supports exactly one content source.

Properties
	•	text: String
	•	src: String

Rules
	•	Exactly one of text or src must be provided.
	•	If both are provided → log warning and prefer text.
	•	If neither is provided → render nothing and log warning.
	•	src loads a UTF-8 text file.
	•	File loading errors must not crash rendering; log warning and render empty content.

Examples

Inline content:
```qml
Markdown {
    text: "
# Title
Hello world.
"
}
```

External file:

```qml
Markdown {
    src: "docs/chapter1.md"
}
```
As **src** we should use an URI resolver, which also is useful vor 
```qml
Image {src: "<imagepath>"}
Viewport3D {model: "<modelpath>"} 
Video { src: "<videopath>"}
```
The URI Resolver should be capable of the following:
```markdown
res:/...
user:/...
http://...
https://...
ipfs:/<CID>
```
All non local resources shall be loaded async and cached locally.

- local schemes (sync): res:/, user:/
- remote schemes (async + cache): ```http://, https://, ipfs:/<CID>```
- Relative path resolution: if Markdown.src is used, resolve relative URLs in Markdown (images, etc.) relative to the source file folder.

### Markdown must:
	•	not compute layout itself
	•	not position elements
	•	not use x/y/anchors
	•	rely entirely on document flow

⸻

## 2) Supported Markdown Syntax (v1)

### 2.1 Headings
```markdown
# Heading 1
## Heading 2
### Heading 3
```
Mapping:
```qml
Label {
    role: heading1|heading2|heading3
}
```

⸻

### 2.2 Paragraphs

Plain text blocks separated by empty line.

Mapping:
```qml
Label {
    role: paragraph
    wrap: true
}
```


⸻

### 2.3 Hard Line Break

Two trailing spaces at end of line:
```
Text␠␠
Next line
```

→ Inserts newline inside same Label.

No ```<br>``` support.

⸻

### 2.4 Bold / Italic
```markdown
**bold**
*italic*
***bolditalic***
```

Inline styling inside Label.

No nested styling required for v1.

⸻

### 2.5 Unordered List
```markdown
- Item 1
- Item 2
```
Mapping:
```qml
Row {
    Label { text: "•" }
    Label { text: "Item 1" }
}
```
Nested lists not required in v1.

⸻

### 2.6 Images
```markdown
![Alt Text](image.png)
```
Mapping:
```qml
Image {
    src: "image.png"
    alt: "Alt Text"
}
```

Relative paths in Markdown (e.g. image.png) resolve relative to the folder of Markdown.src.

Beispiel:
```markdown
	•	src: "res:/docs/book/ch1.md"
	•	![x](images/pic.png) → res:/docs/book/images/pic.png
```

### 2.7 Emojis
Syntax

Emojis use the following syntax:
```markdown
:emoji_name:
```
Examples:
```markdown
:smile:
:warning:
:rocket:
:heart:
```

### Behavior
	•	:emoji_name: is replaced with the corresponding Unicode emoji.
	•	Emoji replacement applies only in normal text (not in code fences).
	•	Unknown emoji names are rendered as plain text.
	•	No automatic conversion of :-) or other emoticons.
	•	Emojis are rendered via RichTextLabel using Unicode characters.

### Font Requirements
	•	Rendering must support font fallback for emoji glyphs.
	•	If an emoji glyph is not available in the active font:
	•	either rely on system fallback
	•	or log a warning in debug mode
	•	Rendering must never crash.

### 2.8 Code Fences

#### Syntax
Triple backticks define a code block:
````
```
code
```
````

Optional language identifier, which can use for syntax highlightning in V2.
````
```kotlin
fun main() {
    println("Hello")
}
```
````
#### Behavior
	•	All content between triple backticks is treated as raw text.
	•	No Markdown parsing inside code fences.
	•	No emoji replacement inside code fences.
	•	No property block parsing inside code fences.
	•	No HTML interpretation inside code fences.
	•	Preserve whitespace exactly as written.
	•	Preserve indentation exactly as written.

#### Mapping

Code fences map to:
```qml
Box {
    role: codeblock

    Label {
        role: code
        wrap: false
        font: monospace
    }
}
```
#### Layout Rules
	•	Code blocks participate in normal document flow.
	•	Code blocks may exceed horizontal width.
	•	Horizontal scrolling support is optional in v1.
	•	No syntax highlighting required in v1.
	•	Language identifier may be stored but not interpreted in v1.
    •	The closing fence must match the opening fence (same character and length)
⸻

## 3) Markdown Property Block Extension

### 3.1 Syntax

A property block directly following a Markdown element modifies that element:
```qml
![alt](img.png)
{
    align: center
    size: 320, 240
}
```

or 
```qml
## Header
{
    id: section1
}
```
### 3.2 Rules
	•	Block must appear immediately after the element.
	•	Block applies only to the previously generated widget.
	•	No nested blocks.
	•	Only one block per element.
	•	If invalid → log warning, continue rendering.

### 3.3 Supported Properties (v1)

For all widgets:
	•	id
	•	align: left|center|right

For Image:
	•	size: width, height

For Label:
	•	color: #AARRGGBB

⸻

## 4) HTML Handling
	•	< and > are treated as normal characters.
	•	No HTML parsing.
	•	<div>, <img>, <script> etc. render as plain text.
	•	No sanitizing required.

⸻

## 5) Explicit Non-Goals (v1)
	•	No HTML support
	•	No tables
	•	No blockquotes
	•	No nested lists
	•	No inline CSS
	•	No float layout
	•	No embedded scripts
	•	No responsive behavior

⸻

## 6) Error Handling
	•	Unknown Markdown constructs → render as plain text.
	•	Invalid property block → log warning and ignore block.
	•	Layout must never break.

⸻

## 7) Acceptance Criteria
	•	Markdown renders headings, paragraphs, lists, and images correctly.
	•	Property blocks modify only the preceding element.
	•	HTML tags render as plain text.
	•	Hard line breaks (two trailing spaces) work.
	•	Scrollable works via document container.
	•	No use of x/y/anchors inside Markdown.

⸻

## 8) Deliverables
	•	Markdown widget implementation
	•	Parser implementation
	•	Sample file: samples/markdown_demo.sml
	•	Unit test for:
	•	heading parsing
	•	property block binding
	•	hard line break handling
	•	HTML ignored