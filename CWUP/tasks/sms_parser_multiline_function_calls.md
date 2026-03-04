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


## Numbers
Because we are changing the parser/interpeter anyways. We should implement flaoting number, because we now work in a 3D environment. And need floats and Vec3D().
But instead of implementing Float I would prefer Double (64 bit) for future.
And when we alredy got Double we should make Integer also (64 bit) for future,

Add unit tests for all possible cases with number. Only accept simple syntax 1.0, .5, 3.14 and no exponential versions. 

## Acceptance Criteria
- Multiline argument list parses without error.
- Existing SMS tests remain green.
- Add parser tests for:
  - multiline global function call
  - multiline object method call (`obj.call(...)`)
  - multiline nested call expression

## Notes
This is required for readability of longer AI-related calls (e.g. `ai.stylizeImage(...)`, `ai.stylizeVideo(...)`).
