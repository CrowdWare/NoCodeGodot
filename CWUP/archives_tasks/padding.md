# Parse padding Property

## Goal

Extend the SML parser to support multiple shorthand notations for the padding property.

Supported Syntax Variants

The parser must correctly handle the following formats:
```qml
padding: 8,8,8,8
padding: 8
padding: 8,8
```

## Expected Behavior

## 1. Four Values
```qml
padding: top, right, bottom, left
```
Example:
```qml
padding: 8,8,8,8
```
Assign values explicitly:
	•	top = 8
	•	right = 8
	•	bottom = 8
	•	left = 8

⸻

## 2. Single Value
```qml
padding: all
```

Example:
```qml
padding: 8
```

Apply value to all sides:
	•	top = 8
	•	right = 8
	•	bottom = 8
	•	left = 8

⸻

## 3. Two Values
```qml
padding: vertical, horizontal
```
Example:
```qml
padding: 8,8
```

	•	top = first value
	•	bottom = first value
	•	left = second value
	•	right = second value

⸻

## Validation Rules
	•	Accept only 1, 2, or 4 comma-separated numeric values.
	•	Reject 3 values.
	•	Trim whitespace.
	•	Values must be numeric (integer or float, depending on existing SML numeric rules).
	•	On invalid input, return a meaningful parser error.

⸻

## Implementation Notes
	•	Parsing should follow KISS principle.
	•	Avoid introducing array support if not already present in SML.
	•	Internally normalize all variants into a unified structure:

```
Padding(top, right, bottom, left)
```

## Supported Elements
All containers