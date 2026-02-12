# Dual Layout Properties (Rect Model: x/y/width/height + position/size)

## Overview

Introduce a unified internal Rect model that supports two equivalent SML syntaxes:
	•	WinForms-style:
	•	x, y, width, height
	•	Godot-style:
	•	position, size

Both syntaxes must map to the same internal data structure without creating multiple layout systems.

The goal is:
	•	Familiarity for WinForms developers
	•	Familiarity for Godot developers
	•	Single internal representation
	•	Deterministic behavior
	•	No layout abstraction layer

⸻

## 1. SML Examples

Variant A – WinForms Style
```qml
Panel {
    x: 10
    y: 20
    width: 200
    height: 100
}
```
Variant B – Godot Style
```qml
Panel {
    position: 10, 20
    size: 200, 100
}
```
Mixed (Allowed)
```qml
Panel {
    position: 10, 20
    width: 300
}
```

⸻

## 2. Internal Data Model

Rect Structure
```csharp
public struct Rect
{
    public float X;
    public float Y;
    public float Width;
    public float Height;

    public Rect(float x, float y, float width, float height)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

    public Rect(Vector2 position, Vector2 size)
    {
        X = position.X;
        Y = position.Y;
        Width = size.X;
        Height = size.Y;
    }

    public Vector2 Position
    {
        get => new Vector2(X, Y);
        set
        {
            X = value.X;
            Y = value.Y;
        }
    }

    public Vector2 Size
    {
        get => new Vector2(Width, Height);
        set
        {
            Width = value.X;
            Height = value.Y;
        }
    }
}
```

⸻

## 3. Parsing Rules

When parsing a node:
	•	If x is present → set Rect.X
	•	If y is present → set Rect.Y
	•	If width is present → set Rect.Width
	•	If height is present → set Rect.Height
	•	If position is present → set Rect.Position
	•	If size is present → set Rect.Size

⸻

## 4. Conflict Resolution Rule

If both syntaxes are used:

The last parsed value wins.

Example:
```qml
Panel {
    x: 10
    position: 50, 60
}
```
Result:
	•	X = 50
	•	Y = 60

Deterministic and simple.

No warnings required.

⸻

## 5. Renderer Mapping (Godot Integration)

For Control nodes:
```csharp
control.Position = rect.Position;
control.Size = rect.Size;
```
For Node2D:
```csharp
node2D.Position = rect.Position;
```
(Width/Height may be ignored if not applicable.)

No intermediate layout system must be introduced.

⸻

## 6. Design Constraints
	•	No separate layout engine
	•	No additional abstraction layer
	•	No Compose-style modifiers
	•	Rect is a pure data structure
	•	No memory overlays / unions
	•	No duplicate storage

⸻

## 7. Acceptance Criteria
	1.	Both syntax styles are supported.
	2.	Mixed usage behaves deterministically.
	3.	No duplicate internal layout storage exists.
	4.	Renderer maps directly to Godot properties.
	5.	No new layout abstraction layer is introduced.
	6.	Behavior is identical regardless of syntax used.

⸻

## 8. Philosophy Alignment

This task ensures:
	•	SML remains engine-aligned (Godot-native concepts).
	•	SML stays V1-stable (no grammar changes required).
	•	No virtual layout layer is introduced.
	•	Developer familiarity is preserved across ecosystems.

⸻

## Summary

This feature provides syntactic flexibility while maintaining a single, deterministic internal layout model.

It reduces adoption friction without increasing architectural complexity.