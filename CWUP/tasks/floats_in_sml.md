# Add Float Literal Support to SML Parser

## Goal

Extend the existing SML parser so it can parse floating-point numeric literals (e.g. 0.0, 1.25) in addition to integers. This is required to mirror Godot 1:1, e.g. anchorLeft: 0.0.

Scope
	•	SMLParser only (lexer/tokenizer + parser + AST value representation).
	•	Keep it simple: no exponent notation needed.

Requirements

## 1) Supported numeric formats

Parse these as numeric literals:
	•	Integers: 0, 12, 300
	•	Floats: 0.0, 1.25, 12.5, 123.456, .5

Not required (can be rejected):
	•	1.
	•	1e-3, 1E+5
	•	numeric suffixes (f, d)

## 2) Grammar / tokenization

Update tokenizer so a number token may contain one dot with digits on both sides:
	•	DIGITS ( "." DIGITS )?

Ensure . used for other syntax is not broken.

## 3) AST / Value model

Unify numeric literals into one value type (preferred):
	•	NumberLiteral(raw: String, value: Double, isFloat: Boolean)
	•	isFloat = raw contains '.'
	•	value parsed with invariant culture (dot as decimal separator)

Alternative (acceptable):
	•	Separate IntLiteral and FloatLiteral.

## 4) Parser behavior

Wherever numeric values are accepted today (properties like width: 300), also accept float values (e.g. anchorLeft: 0.0) without any additional syntax changes.

## 5) Error handling
	•	Invalid numbers like 12..3, 1. should produce a clear parse error with location.
	•	Very large numbers should error gracefully (no crash).

## Test Cases

### Must parse
```qml
Control { anchorLeft: 0.0 }
Control { anchorRight: 1.0 }
Control { width: 300 }
Control { opacity: 0.75 }
Control { anchorLeft: .5 }
```
### Must fail
```qml
Control { anchorLeft: 1. }
Control { anchorLeft: 1e-3 }
Control { anchorLeft: 12..3 }
```

## Definition of Done
	•	All existing integer-only SML files still parse unchanged.
	•	Float literals parse correctly and appear in the AST/value model.
	•	Unit tests added/updated to cover all “Must parse” and “Must fail” cases.