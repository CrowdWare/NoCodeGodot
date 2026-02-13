# Implement Id (Symbol IDs) + Scope + Mapping (V1)

## Overview

Implement a QML-style id system for SML:
	•	In SML, id values are identifiers/symbols, not strings and not numbers.
	•	At runtime, each id is mapped to a compact integer for fast comparisons.
	•	id uniqueness is enforced within a defined scope.
	•	The scope for uniqueness is per .cs file (the logic file associated with a .sml view).

This enables deterministic event routing and fast control lookup without string comparisons.

⸻

## 1) SML Syntax

id is an Identifier (Symbol)

Valid:
```qml
Window {
    id: mainWindow
}

Item {
    id: 3
}
```
Invalid:
```qml
id: "mainWindow"   // not allowed
id: main-window    // not allowed (dash)
```
Identifier Format (V1)

id must match:
	•	^([A-Za-z_][A-Za-z0-9_]*|\d+)$
	•	case-sensitive

⸻

## 2) Scope Rule

ID Scope = per .cs file
	•	All id: symbols used in the .sml view(s) driven by the same .cs file must be unique.
	•	Duplicate id within the same .cs scope → error.
	•	Same id name may appear in another .cs scope without conflict.

This mirrors the principle that IDs are only meaningful within their “context/scope.”  ￼

⸻

## 3) Runtime Type: Id

Implement a lightweight Id value type used everywhere in runtime APIs:

```csharp
public readonly struct Id
{
    public readonly int Value;
    public Id(int value) => Value = value;

    public override string ToString() => Value.ToString();
}
```

Notes:
	•	Value is the internal numeric handle.
	•	No string name stored in release builds (optional debug feature below).

### Optional Debug Mode (Nice-to-have)

Allow storing the original name for logging only (not required for V1):
	•	IdDebugName table in the scope (int → string)
	•	Id.ToString() can return Name(Value) when debug enabled

⸻

## 4) ID Table / Allocation

For each .cs scope, maintain:
	•	Dictionary<string, int> nameToValue
	•	List<string> valueToName (optional; for debug)

Allocation rules:
	•	First seen symbol gets next integer starting at 1
	•	0 is reserved for “no id / unset”
	•	Duplicate symbol within scope → error

Pseudo:
```code
GetOrCreateId("projectTree"):
  if exists -> return existing
  else:
    value = ++counter
    store mapping
    return value
```


⸻

## 5) Where Id is Stored

Every renderable element may have an id.

At parse/build time, for each node:
	•	If id: present → convert identifier to Id
	•	If missing → Id.Value = 0

Minimum storage in runtime node model:
	•	Id ControlId (for controls like TreeView, Button, etc.)
	•	Additionally, in domain objects (e.g., TreeViewItem), Id ItemId (recommended)

⸻

## 6) Validation & Errors

Parser/Loader must error on:
	•	invalid id token (string literal, number, invalid characters)
	•	duplicated id symbol within the same .cs scope

Error messages should include:
	•	file name (.sml)
	•	line number (if available)
	•	duplicated id name

⸻

## 7) Reflection/Event System Integration (Dependency for TreeView)

TreeView and other controls will call script methods via reflection. This task defines the ID parameter type used by events.

Standard event signature target (example for TreeView)
```csharp
void treeViewItemSelected(Id treeViewId, TreeViewItem item)
```
TreeView will pass its own control Id as first parameter.

Important: Reflection lookup should support Id parameters (struct) cleanly.

Performance requirement (small but important)

Cache reflection lookups (MethodInfo) per .cs scope and method name to avoid repeated expensive lookups. 


⸻

## 8) Acceptance Criteria
	1.	id: someName parses as an identifier (symbol), not a string.
	2.	"quoted" ids are rejected.
	3.	Numeric ids are rejected.
	4.	Duplicate ids within the same .cs scope produce a deterministic error.
	5.	Each id maps to an internal integer Id.Value starting from 1; missing id results in Id.Value = 0.
	6.	Runtime APIs can accept Id as a parameter (no boxing errors / no type mismatch).
	7.	Reflection event invocation can find methods with Id parameters and should cache method resolution.

⸻

## 9) Out of Scope (V1)
	•	Cross-scope global IDs
	•	Hierarchical scoping rules
	•	Automatic id generation when id is missing
	•	Expressions in id values
	•	Aliasing / re-exporting IDs across files