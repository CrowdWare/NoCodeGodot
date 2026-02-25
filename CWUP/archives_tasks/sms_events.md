# SMS Event Handlers

## Goal

Extend the SMS parser + interpreter to support event handler blocks so users can write:

```
on openWindow.pressed() {
    project.openEditor()
}

on mainWindow.sizeChanged(w, h) {
    log.info(w)
}
```

This is needed to bind Godot signals to SMS logic in a clean, readable way.

## Scope
	•	SMSParser + Interpreter only
	•	Keep syntax tiny and deterministic

⸻

## 1) Syntax

### 1.1 Event handler form
```
on <targetId>.<eventName>(<optionalParamList>) { <statements> }
```
Examples:
```
on open.pressed() { project.open() }

on mainWindow.sizeChanged(w, h) {
    log.info(w)
    super(w, h)
}
```

### 1.2 Identifiers (same rule as SML ids)
	•	lowerCamelCase
	•	Regex: ^[a-z][a-zA-Z0-9]*$

### 1.3 Event names
	•	Also lowerCamelCase identifier
	•	Examples: pressed, toggled, sizeChanged, tabChanged, itemSelected

### 1.4 Parameters
	•	Optional
	•	If present: comma-separated identifiers
	•	No default values, no types
	•	Example: (w, h) or (id) or ()

⸻

## 2) Parser Changes

### 2.1 New AST node

Add an AST node for event handlers, e.g.:
	•	EventHandler(targetId: String, eventName: String, params: List<String>, body: Block)

### 2.2 Top-level parsing

Event handlers are allowed at top level (like functions today, if you have them).
Parser must collect them into the program AST.

### 2.3 Conflicts

Ensure **on** is a reserved keyword and cannot be used as an identifier.

⸻

## 3) Interpreter / Runtime Changes

### 3.1 Handler registry

Interpreter must build a registry:

Key:
	•	targetId + "." + eventName

Value:
	•	handler AST + parameter list

Example keys:
	•	open.pressed
	•	mainWindow.sizeChanged

### 3.2 Invoke API

Add an interpreter entrypoint used by the host (Godot glue) to call events:
```
InvokeEvent(targetId, eventName, args[])
```
Behavior:
	•	Find matching handler
	•	Bind args to params by position
	•	Execute handler body
	•	If no handler exists: no-op (return false)

### 3.3 super(...)

Support calling the default handler from inside an event handler:
	•	super() or super(a,b) allowed only inside an on ... {} handler
	•	Interpreter should not crash if super is used outside; produce a runtime error.

Execution model:
	•	super(...) calls a host-provided callback for that event.
	•	Provide a hook like: SetSuperDispatcher(Func<string targetId, string eventName, object[] args, void>)

If no dispatcher is set:
	•	super(...) becomes a no-op or throws a clear runtime error (choose one and document it).

⸻

## 4) Validation Rules

### 4.1 Duplicate handlers

If the same <id>.<event> is defined twice:
	•	Parser error (fail fast) OR
	•	last one wins (not recommended)

Pick parser error.

## 4.2 Parameter mismatch on invoke

If invoked with different arg count:
	•	Missing args become null OR runtime error

Pick runtime error with clear message:
	•	Event mainWindow.sizeChanged expects 2 args, got 1

⸻

## 5) Test Cases

Must parse
```
on open.pressed() { project.open() }

on mainWindow.sizeChanged(w, h) {
    log.info(w)
    super(w, h)
}
```
Must fail (parse)
```
on Open.pressed() { }          // wrong casing (id)
on open.Pressed() { }          // wrong casing (event)
on open.pressed(,) { }         // invalid params
on open.pressed(a b) { }       // missing comma
on open.pressed(a,) { }        // trailing comma
```

Must fail (runtime)
	•	super() used outside handler
	•	invoked arg count mismatch

⸻

## Definition of Done
	•	Parser accepts on <id>.<event>(...) { ... } blocks.
	•	Interpreter registers handlers and exposes InvokeEvent(id, event, args).
	•	super(...) works inside event handlers via a host callback hook.
	•	Unit tests cover parsing + runtime validation cases above.