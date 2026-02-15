# Refactoring – Runtime Simplification

## 1. Remove Custom Docking
	•	Delete all custom docking logic.
	•	Remove:
	•	Split calculations
	•	Area enums
	•	Width/Offset management
	•	Custom resize handles
	•	Keep only Godot-native controls.

## Definition of Done:
	•	No custom docking code remains.
	•	UI builds using Godot TabContainer, PanelContainer, SplitContainer only.

⸻

## 2. Layout System
	•	Remove layout modes app and document.
	•	Use Godot layout system exclusively:
	•	Anchors
	•	SizeFlags
	•	Containers

## Definition of Done:
	•	No layout mode branching exists in runtime.
	•	SML properties map directly to Godot properties.

⸻

## 3. Controls Mapping
	•	Control names in SML must match Godot class names.
	•	Properties must match Godot property names and types.
	•	Remove all name-mapping code.

## Definition of Done:
	•	No property translation layer exists.
	•	SML acts purely as serialization.

⸻

## 4. Full Demo

Provide:
	•	demo.sml
	•	demo.sms

Showing:
	•	Menu
	•	Tab drag
	•	Panel move
	•	Events

⸻

## 5. Events (Enhanced Event Resolution)

### Goal

When refactoring SML → Godot 1:1 controls, event binding must support:
	•	automatic generated IDs if no id is provided, and
	•	elegant ```on <id>.<event>()``` handlers when id is provided by the user.

That allows both:
	•	quick container-based events with parameters, and
	•	elegant element-specific events when the user knows id.

⸻

## Event Resolution Rules
### 1.	If a control has an explicit id in SML:
```qml
Button { id: save }
```
→ events fire as:
```
on save.pressed() {
    ...
}
```
without requiring parameters, because the id provides context.

2.	If a control does NOT have an id, the event is bound to the container signal with parameters:
```
on fileMenu.idPressed(id) {
    log.info(id)
}
```
because no element id was given.

3.	For list/collection controls (PopupMenu, TabContainer, ItemList, Tree), if the user omits id, container signals with positional identifiers are supported.
4.	The engine internally resolves:
```
PopupMenu.id_pressed(int internalId) → elementId (if present) → SMS handler
```
→ if an element has a user-assigned id, then fire:
```
<elementId>.pressed()
```
→ otherwise the handler signature remains:
```
on containerSignal(parameter)
```

⸻

### Tasks

#### 5.1 Build the Signal Mapping Registry
	•	Define a mapping table: GodotSignal → SMS eventName + parameter mapping
	•	For controls with child element IDs (e.g. PopupMenu/Items), include internal ID → SML id mapping.

#### 5.2 Connect Godot Signals to SMS Handlers
	•	For each instantiated node from SML with or without id automatically connect the appropriate Godot signals.
	•	When an SMS handler exists for the resolved event, sms.Invoke(...) with the right JS-like event (idless or id-based).
Example:
| Godot Signal | SMS Style |
|- |- |
| Button.pressed | ```<id>.pressed()``` |
| PopupMenu.id_pressed(int) | ```<elementId>.pressed()``` (if id exists) |
| | otherwise ```on popup.idPressed(id)``` |
| TabContainer.tab_changed(int) | if item has id: ```<id>.tabChanged()``` |
| | else: ```on tabs.tabChanged(index)``` |
| ItemList.item_selected | similar pattern |

#### 5.3 Update Demo .sml + .sms
	•	Include examples of both:
	•	event handlers with explicit IDs
	•	container events with parameters
	•	Show usage of both patterns.

⸻

## Definition of Done
	•	Event resolution supports:
	•	on <id>.<event>() { } when id is present
	•	container events when no id is present
	•	No leftover Godot signal names leaked into SMS (use normalized event names)
	•	Demo covers both scenarios
	•	Event binding works with generated internal IDs and user-provided IDs