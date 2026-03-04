# SMS Parser: Multiline Function Calls in Argument Lists

## Problem
In `ForgePoser/main.sms` a multiline function call failed to parse:

```sms
var resultPath = ai.stylizeImage(
    capturedPath,
    styledPath,
    aiPrompt,
    aiStyleImagePath,
    aiExtraImagePath,
    aiNegativePrompt,
    "grok-2-image-1212")
```

Runtime error was raised at parse time (`Error at line 127, column 3`).

## Goal
The SMS parser must support multiline function calls with argument lists over multiple lines.

## Scope
- Parser/Lexer in `SMSCore`.
- Preserve existing syntax and behavior.
- No regressions for current one-line calls and method calls.

## Expected Behavior
The following must parse and execute:
- Single-line call: `foo(a, b, c)`
- Multiline call with indentation
- Trailing spaces/newlines between `(`, arguments, commas, and `)`
- Nested calls split across multiple lines

## Acceptance Criteria
- Multiline argument list parses without error.
- Existing SMS tests remain green.
- Add parser tests for:
  - multiline global function call
  - multiline object method call (`obj.call(...)`)
  - multiline nested call expression

## Notes
This is required for readability of longer AI-related calls (e.g. `ai.stylizeImage(...)`, `ai.stylizeVideo(...)`).
