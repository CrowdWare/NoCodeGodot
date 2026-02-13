# Implement TreeView (V1)

## Overview

Implement a declarative TreeView control in SML.

The TreeView:
	•	Renders hierarchical Item nodes
	•	Supports structured, non-rendered payload via lowercase data {} blocks
	•	Uses convention-based reflection for selection events
	•	Requires no parser changes
	•	Respects the global lowercase meta-node rule

⸻

## 1. SML Structure

Example
```qml
TreeView {
    id: projectTree

    Item {
        id: 0
        text: "Root"
        expanded: true

        Item {
            id: 1
            text: "Goblin"

            data {
                Enemy {
                    health: 599
                    damage: 20
                }
            }
        }

        Item {
            id: 2
            text: "Branch 2"
        }
    }
}
```


⸻

## 2. TreeView Rules

Allowed Properties
	•	id: Id (unique identifier)
	•	width, height, layout properties (standard SML control properties)

⸻

## 3. Item Rules

Each Item supports:

Required
	•	id: Id (unique identifier)
	•	text: string (display label)

Optional
	•	expanded: bool (default: false)

Allowed Children
	•	Item { ... } (0..n nested children)
	•	data { ... } (0..1 meta payload block)

No other child nodes are allowed.

⸻

## 4. Lowercase Meta Rule

The global SML rule applies:

Any node whose name starts with a lowercase letter is a non-rendered meta node.

Therefore:
	•	data {} is parsed but not rendered
	•	Nodes inside data {} are also not rendered
	•	They exist only in the AST

This requires no parser modification.

Rendering logic must skip lowercase nodes.

⸻

## 5. Data Block Rules

data {}:
	•	May appear at most once per Item
	•	Must contain exactly one root node
	•	That root node represents the domain payload

Valid:
```qml
data {
    Enemy {
        health: 599
    }
}
```
Invalid:
```qml
data {
    Enemy { }
    Loot { }
}
```
(empty or multiple roots → parser error)

⸻

## 6. Runtime Model

### TreeViewItem Class
```java
class TreeViewItem
{
    public int Id;
    public string Text;
    public bool Expanded;

    public SmlNode Data;   // Root node inside data { }

    public List<TreeViewItem> Children;
}
```
Notes:
	•	Data may be null
	•	Data.Name represents type (e.g., “Enemy”)
	•	Data contains structured SML properties

⸻

## 7. Rendering Behavior

Renderer must:
	1.	Traverse only uppercase nodes
	2.	Render only Item nodes
	3.	Skip lowercase nodes entirely
	4.	Build internal TreeViewItem structure from parsed AST

Rendering must not depend on data.

⸻

## 8. Selection Event (Convention-Based)

TreeView uses reflection-based convention.

Default Event
```js
void treeViewItemSelected(Id id, TreeViewItem item)
```
### ID-Based Event

If TreeView has:
```js
TreeView { id: projectTree }
```
System first attempts:
```js
void projectTreeItemSelected(Id id, TreeViewItem item)
```
If not found, fallback to:
```js
void treeViewItemSelected(Id id, TreeViewItem item)
```
If neither exists, do nothing.

⸻

## 9. Selection Behavior

When an item is selected:
	1.	Resolve the corresponding TreeViewItem
	2.	Invoke reflection method
	3.	Pass full item object (including Data)

Example usage:
```js
void projectTreeItemSelected(Id id, TreeViewItem item)
{
    if (item.Data != null && item.Data.Name == "Enemy")
    {
        int health = item.Data.GetInt("health");
    }
}
```

⸻

## 10. Constraints
	•	Item.id must be unique within TreeView
	•	Only Item and lowercase meta nodes allowed as children
	•	data must contain exactly one root node
	•	Lowercase nodes must never render UI
	•	No string-based callback bindings

⸻

## 11. Scope (V1)

Included:
	•	Hierarchical rendering
	•	Expand/collapse state
	•	Selection event
	•	Structured data payload
	•	Lowercase meta handling
	•	Reflection-based event invocation

Not included:
	•	Multi-selection
	•	Drag & drop
	•	Lazy loading
	•	Inline editing
	•	Virtualization
	•	Keyboard navigation
	•	Context menu

⸻

## 12. Acceptance Criteria
	1.	Nested items render correctly.
	2.	Expand/collapse works.
	3.	Lowercase data {} is not rendered.
	4.	Data payload is accessible in selection event.
	5.	ID-based event naming works.
	6.	Fallback event naming works.
	7.	No parser modification required.
	8.	No UI artifacts from meta nodes.
	9.	Syntax highlighting distinguishes lowercase meta nodes.

⸻

## Summary

TreeView V1 provides:
	•	Declarative hierarchical UI
	•	Structured domain payload via data
	•	Clear separation between view and model
	•	Convention-based event system
	•	Zero parser complexity increase

This aligns with SML design philosophy:
	•	Declarative
	•	Deterministic
	•	Minimal
	•	Extensible