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
For this purpose we now have an own task: code_generator.md

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

## 6. Actions & Type Conversion (Call Semantics)

### Goal

SMS method calls must feel scripting-like while directly calling Godot methods without per-method adapters.

The runtime therefore performs automatic argument packing and type casting based on the target method signature.

This avoids generating wrapper code for every Godot method.

⸻

## Invocation Rules

When calling:
```
<id>.methodName(args...)
```

the runtime resolves the Godot method and then applies:

⸻

## 6.0 Method Resolution Pipeline

Runtime lookup order:
	1.	Convert methodName → snake_case
	2.	Collect all Godot methods with that name
	3.	Filter by argument count
	4.	Attempt type casting
	5.	Select first matching overload
	6.	Invoke via reflection

No hardcoded per-method mapping is allowed.

This guarantees forward compatibility with new Godot versions.

⸻

## 6.1 Argument Packing
If the target method expects a structured type:
| Expected Type | Accepted SMS Call |
| - | - |
| Vector2 | ```setPosition(10,20)``` OR ```setPosition(Vector2(10,20))``` |
| Vector3 | ```lookAt(0,1,0)``` OR ```lookAt(Vector3(0,1,0))``` |
| Color | ```setColor("#FF00FF")``` OR ```setColor(Color("#FF00FF"))``` |
| Rect2 | ```setRect(0,0,100,20)``` |


## Rule:

Multiple primitive parameters are automatically packed into the expected engine type.

⸻

## 6.2 String Casting
If a string is passed and a known engine type is expected:
```
addPreset("#FF00FF")
```
equals
```
addPreset(Color("#FF00FF"))
```

⸻

## 6.3 No Magic Setters
SMS never creates artificial setters/getters.

Allowed:
```
window.title = "New"
window.setTitle("New")
```
Not allowed:
```
window.title("New")   // invalid
label.text()          // invalid
```
Only real Godot methods are callable.

⸻

## 6.4 Return Values
Returned engine objects are exposed to SMS:
```
var c = picker.getPreset()
setPreset(c)
```

## 6.5 Name Mapping (Godot → SMS)

### Goal: No per-class mapping tables. 
Mapping must be purely rule-based.

### Canonical Names
	•	Godot canonical names are snake_case (set_position, button_down, size_flags_horizontal, …)
	•	SMS exposes names as lowerCamelCase (setPosition, buttonDown, sizeFlagsHorizontal, …)

### Mapping Rule
	•	snake_case → lowerCamelCase
	•	set_position → setPosition
	•	get_button_icon → getButtonIcon
	•	button_down → buttonDown
	•	If a name has no underscores, keep it and only lower the first character:
	•	Shortcut → shortcut
	•	RID → rID (edge case; acceptable)

No special casing per control.

⸻

## 6.6 Properties in SMS

Properties are never callable.

✅ Allowed:
```
var t = label.text
label.text = "Hello"
```
❌ Not allowed:
```
label.text()
label.text("Hello")
```
Property Resolution
When SMS sees obj.someProp or obj.someProp = value:
	1.	Convert someProp → some_prop using the inverse rule (lowerCamel → snake_case).
	2.	Check if the Godot property exists on the instance.
	3.	Use get() / set().

No magic setters, no implicit method bridging.

⸻

## 6.7 Methods vs Properties (No Ambiguity)
	•	obj.name always means property access.
	•	obj.name(...) always means method call.
	•	If name exists only as property → calling name(...) throws a runtime error.
	•	If name exists only as method → obj.name should throw “unknown property” (unless you intentionally allow method references, which we currently do not).

⸻

## 6.8 Overload Selection

When multiple Godot overloads exist:

Priority:
	1.	Exact type match
	2.	Castable match
	3.	Packed structure match (Vector/Color)
	4.	Fail with runtime error

Example:
```
setPosition(10,20)     → Vector2 overload
setPosition(vec)       → Vector2 overload
setPosition("10,20")   → invalid
```

⸻

## 6.9 Argument Packing & Casting (recap)
	•	If method expects Vector2 and SMS passes x,y → pack into Vector2(x,y)
	•	If method expects Color and SMS passes "#RRGGBBAA" → cast to Color

⸻

## 6.10 Codex / Tooling Hint

Code generators must not create wrappers for methods.

Instead they should rely on:

• reflection lookup
• name normalization
• runtime casting

The method system is intentionally data-driven and future-proof.

⸻

Definition of Done (extended)

• Rule-based mapping only
• Properties accessible only via property syntax
• Methods accessible only via call syntax
• Overloads resolved dynamically
• Vector/Color packing works
• Returned objects usable immediately
• No wrappers generated
• Compatible with new Godot methods automatically

---

## Final Definition of Done

• Any Godot method callable via reflection works automatically
• No per-method wrappers exist
• Vector/Color conversion works
• Property assignment independent of methods
• Runtime survives future Godot API additions without changes