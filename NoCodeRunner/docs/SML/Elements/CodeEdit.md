# CodeEdit

## Inheritance

[CodeEdit](CodeEdit.md) → [TextEdit](TextEdit.md) → [Control](Control.md) → [CanvasItem](CanvasItem.md) → [Node](Node.md) → [Object](Object.md)

## Collection Items

This control appears to manage internal **items** (collection-style API).
Items are typically not represented as child nodes/properties in Godot.
In SML, this will be represented via **pseudo child elements** (documented per control).

## Properties

This page lists **only properties declared by `CodeEdit`**.
Inherited properties are documented in: [TextEdit](TextEdit.md)

| Godot Property | SML Property | Type | Default |
|-|-|-|-|
| auto_brace_completion_enabled | autoBraceCompletionEnabled | bool | — |
| auto_brace_completion_highlight_matching | autoBraceCompletionHighlightMatching | bool | — |
| code_completion_enabled | codeCompletionEnabled | bool | — |
| gutters_draw_bookmarks | guttersDrawBookmarks | bool | — |
| gutters_draw_breakpoints_gutter | guttersDrawBreakpointsGutter | bool | — |
| gutters_draw_executing_lines | guttersDrawExecutingLines | bool | — |
| gutters_draw_fold_gutter | guttersDrawFoldGutter | bool | — |
| gutters_draw_line_numbers | guttersDrawLineNumbers | bool | — |
| gutters_line_numbers_min_digits | guttersLineNumbersMinDigits | int | — |
| gutters_zero_pad_line_numbers | guttersZeroPadLineNumbers | bool | — |
| indent_automatic | indentAutomatic | bool | — |
| indent_size | indentSize | int | — |
| indent_use_spaces | indentUseSpaces | bool | — |
| line_folding | lineFolding | bool | — |
| symbol_lookup_on_click | symbolLookupOnClick | bool | — |
| symbol_tooltip_on_hover | symbolTooltipOnHover | bool | — |

## Events

This page lists **only signals declared by `CodeEdit`**.
Inherited signals are documented in: [TextEdit](TextEdit.md)

| Godot Signal | SMS Event | Params |
|-|-|-|
| breakpoint_toggled | `on <id>.breakpointToggled(line) { ... }` | int line |
| code_completion_requested | `on <id>.codeCompletionRequested() { ... }` | — |
| symbol_hovered | `on <id>.symbolHovered(symbol, line, column) { ... }` | string symbol, int line, int column |
| symbol_lookup | `on <id>.symbolLookup(symbol, line, column) { ... }` | string symbol, int line, int column |
| symbol_validate | `on <id>.symbolValidate(symbol) { ... }` | string symbol |
