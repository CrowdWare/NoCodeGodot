# SML Parser Core
	•	Implement SML tokenizer
	•	Implement SML AST model
	•	Support nested nodes
	•	Support attributes & IDs
	•	Graceful handling of unknown nodes

In the folder SMLParser we have gor a sml inplementation in C++ as rederence.


Here is a sample.

```qml
Window {
	id: main
	title: "Hello World"
	left: 20
	top: 20
	width: 800
	height: 600
	spacing: 8

	Row {
		spacing: 8
		Label { text: "Hello" }
		Label { text: "SML" }
	}
}
```

A special property here is id: main which is an enum and will become an idenitifier to be referenced and should be registered before parsing.

```csharp
enum class PropertyKind {
    Action,
    Identifier
};
registerProperty("id", PropertyKind::Identifier);
```

And here we are using enums, in this case closeQuery.
```qml
MenuItem {
	label: "Exit"
	action: closeQuery
	useOnMac: false
}
```

Enums shall be registered before parsing and will be mapped to integer values.

```csharp
enum class ActionId {
    CloseQuery,
    OpenUrl,
    Navigate,
};

registerEnum("closeQuery", ActionId::CloseQuery);
```

---

Here is a special property named tuple. In SML we dont know about float values, so we use millimeters as integers instead, which will be mapped to Vector3.
```qml
Window {
	pos: 20,20	// pixels
}

Object3D {
	pos3D: 0,1200,0	// millimeters
}
```

Map these tuples to Vector3 in Godot.
```csharp
Vector3 toGodotPos(Int3 mm) {
    return Vector3(
        mm.x / 1000.0f,
        mm.y / 1000.0f,
        mm.z / 1000.0f
    );
}
```
