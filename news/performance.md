# Forge Runtime Performance: C# → C++ Migration

The Forge runtime was recently migrated from **C#** to **C++**. This change significantly reduced the runtime size and improved execution speed while removing the dependency on the C# / Mono layer.

## Key Results

| Metric | Before | After |
|------|------|------|
| Runtime language | C# | C++ |
| Runtime size | ~41k lines | ~19k lines |
| Performance | baseline | ~7× faster (early tests) |

## Motivation

Forge aims to keep the runtime **small and native**, while pushing most application logic into structured scripts.

Instead of building applications on top of browser runtimes or large frameworks, Forge uses:

- **SML (Simple Markup Language)** for application structure and UI
- **SMS (Simple Multiplatform Script)** for application logic
- a **small native runtime** for execution

Migrating the runtime to C++ made it possible to:

- remove the C# / Mono layer
- reduce runtime size
- improve execution speed
- simplify integration with native systems

## Example

One example tool built on top of Forge is a Pose Editor.

The application logic is mostly written in SMS:

PoseEditor.sms (~1800 lines)

while lower-level functionality remains in the native runtime.

### SML + SMS Example

A simple Forge application might look like this.

**SML (structure / UI):**

```qml
Window {
    Button {
        id: runButton
        text: "Run"
    }
}
```

**SMS (logic):**

```kotlin
on runButton.clicked {
    lo.success("Run button pressed")
}
```

The UI structure is defined in **SML**, while the behavior is implemented in **SMS**. The runtime connects both layers at execution time.

## Architecture Overview

Forge applications follow a simple structure:

SML → Application structure  
SMS → Application logic  
Forge Runner → Native runtime

Example workflow:

edit app.sml  
forge run  
application runs in the native runtime

## Status

Forge is still an early-stage project, but the migration to C++ represents a major milestone toward a smaller and faster runtime.

## Repository

(Add repository link here)

Feedback and ideas are welcome.