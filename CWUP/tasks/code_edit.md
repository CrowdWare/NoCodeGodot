# Implement CodeEditor Syntax System (V1)

## Overview

Implement a deterministic, extension-driven syntax system for CodeEditor.

The system must:
	•	Default to auto
	•	Always recompute syntax on load() and save_as()
	•	Support sml, cs, and markdown
	•	Support custom rule files via res:/...
	•	Never contain hidden or implicit behavior
	•	Be layout-safe and hot-switch safe

This defines V1 behavior.

⸻

## 1. Syntax Property

Definition
```qml
syntax: "..."
```

	•	Type: String
	•	Optional
	•	Default value: "auto"

Supported Values (V1)
	•	"auto"
	•	"sml"
	•	"cs"
	•	"markdown"
	•	"res:/syntax/<custom>.cs" (custom rule file)

If syntax is omitted → treat as "auto".


⸻

## 2. Core Rule (Mandatory Behavior)

Hard Rule

On every:
	•	load(path)
	•	save_as(path)

The editor must always recompute and overwrite the active syntax from the file extension.

There are no exceptions.

This includes:
	•	Overwriting "sml"
	•	Overwriting "markdown"
	•	Overwriting "res:/syntax/mysml.cs"

File extension always wins.

⸻

## 3. Extension Mapping (V1)

Mapping must be case-insensitive.

| Extension | Resulting Syntax | Rule File |
| - | - | - |
| .sml | sml | res:/syntax/sml_syntax.cs |
| .cs | cs | res:/syntax/cs_syntax.cs |
| .md | markdown | res:/syntax/markdown_syntax.cs |
| .markdown | markdown | res:/syntax/markdown_syntax.cs |
| unknown | plain_text | no highlighter |

If extension is unknown:
	•	Active syntax becomes plain_text
	•	Highlighter is removed

⸻

## 4. Behavior Without File (Unsaved Buffer)

If the editor has no associated path:

If syntax is:
    •	"sml" → use SML highlighter
    •	"cs" → use CS highlighter
    •	"markdown" → use Markdown highlighter
    •	"res:/..." → load that rule file
    •	"auto" → plain_text

Once a load() or save_as() happens, extension logic overrides everything.


⸻

## 5. Custom Rule Files

If:
```qml
syntax: "res:/syntax/mysml.cs"
```

Then:
	•	Load rule file directly
	•	No mapping logic applied

However:

If load() or save_as() is executed later:

→ extension mapping overrides this value.

⸻

## 6. Hot-Switch Procedure

On syntax change (triggered by load() or save_as()):
	1.	Determine syntax from extension
	2.	Dispose old highlighter instance
	3.	Create new highlighter instance
	4.	Assign to CodeEdit
	5.	Force redraw

Must:
	•	NOT change layout size
	•	NOT reset layout mode
	•	NOT collapse to 1–2 pixels
	•	NOT cause visual flicker

Scroll position preservation is optional but recommended.

⸻

## 7. Layout Requirement (Related)

CodeEditor must:
	•	Default to layoutMode: document
	•	Expand to full available size
	•	Resize correctly with window/container
	•	Never render as minimal 1–2px control

⸻

## 8. Rule File Loading Logic

Implementation logic:
```js
if syntax startsWith("res:/")
    loadRuleSet(syntax)
else if syntax == "sml"
    loadRuleSet("res:/syntax/sml_syntax.cs")
else if syntax == "cs"
    loadRuleSet("res:/syntax/cs_syntax.cs")
else if syntax == "markdown"
    loadRuleSet("res:/syntax/markdown_syntax.cs")
else
    removeHighlighter() // plain_text
```
During load/save_as:
```js
detectedSyntax = extensionMapping(path)
apply detectedSyntax
```

⸻

## 9. Acceptance Criteria

The following must work:

### Scenario A – Default Auto

```qml
CodeEditor {
    text: "hello"
}
```

	•	Starts as plain_text
	•	load("file.md") → markdown
	•	save_as("file.cs") → cs

### Scenario B – Explicit Markdown Initially
```qml
CodeEditor {
    text: "hello"
    syntax: "markdown"
}
```
	•	Starts with markdown highlighting
	•	save_as("test.cs") → switches to cs
	•	load("file.sml") → switches to sml

⸻

### Scenario C – Custom Syntax Initially
```qml
CodeEditor {
    syntax: "res:/syntax/mysml.cs"
}
```

Starts with custom rules
	•	load("file.md") → switches to markdown
	•	save_as("file.cs") → switches to cs


⸻

### Scenario D – Unknown Extension
```qml
load("file.xyz")
```

	•	Syntax becomes plain_text
	•	No crash
	•	No stale highlighting

⸻

## 10. Out of Scope (V1)
	•	Syntax locking
	•	Embedded languages
	•	Mixed modes
	•	Language inference without extension
	•	Multiple rule inheritance
	•	Advanced LSP integration
	•	Diagnostics markers

⸻

Final Design Principles
	•	Deterministic
	•	Extension-driven
	•	No hidden magic
	•	No partial override behavior
	•	No mixed logic
	•	No special cases