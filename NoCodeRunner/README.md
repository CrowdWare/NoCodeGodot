# NoCodeRunner – Technical Documentation

## Overview

NoCodeRunner is the runtime environment for NoCode projects created with the NoCodeDesigner.
Instead of using Godot’s native .tscn scene files, the system relies on SML (Simple Markup Language) as a declarative, portable UI and scene description format.

The NoCodeRunner loads SML documents from a remote source (e.g. GitHub Pages), resolves and caches assets locally, parses SML at runtime, and dynamically builds the corresponding Godot Node tree using C#.

All runtime logic is implemented directly in C# using Godot’s API.

---

## Key Goals
	•	Runtime-generated UI & scenes (no precompiled .tscn)
	•	Remote-first workflow (SML + assets hosted on GitHub Pages or similar)
	•	Deterministic asset loading & caching
	•	Clear separation of concerns
	•	SML → structure & configuration
	•	C# → behavior & logic
	•	Godot → rendering, input, animation, physics
	•	Same SML works in Designer and Runner

---

## High-Level Architecture
```
┌────────────────────────────────┐
│   Remote Hosting               │
│  (GitHub Pages, CDN)           │
│                                │
│  - manifest.sml                │
│  - ui.sml                      │
│  - assets (.glb, video, .png)  │
└───────────┬────────────────────┘
            │ HTTP
            ▼
┌──────────────────────────┐
│      NoCodeRunner        │
│                          │
│  Manifest Loader         │
│  Asset Cache Manager     │
│  SML Parser              │
│  Node Factory            │
│  C# Logic Layer          │
│                          │
└───────────┬──────────────┘
            ▼
┌──────────────────────────┐
│        Godot Engine      │
│                          │
│  Nodes / UI / 3D / Video │
│  Input / Animation       │
│  Vulkan Renderer         │
│                          │
└──────────────────────────┘
```
---

## Manifest System

### Purpose

The manifest is the entry point for every NoCode project.
It describes which assets exist, where they are located, and when they were last modified.

The Runner uses the manifest to:
	•	Determine which assets must be downloaded
	•	Avoid unnecessary network requests
	•	Maintain a persistent on-disk cache

### Manifest Responsibilities
	•	Asset list with:
	•	Logical ID
	•	Remote URL
	•	Last modified timestamp or hash
	•	Asset type (UI, model, video, texture, etc.)
	•	Optional versioning info
	•	Optional entry points (e.g. main: "ui.sml")

### Runtime Flow
	1.	Runner downloads manifest.sml
	2.	Compares manifest entries with local cache metadata
	3.	Downloads only changed or missing assets
	4.	Updates local cache index
	5.	Continues with SML parsing and rendering

---

## Asset Caching Strategy
	•	Assets are cached on disk
	•	Cache key = logical asset ID
	•	Cache metadata stores:
	•	lastModified / hash
	•	local file path
	•	Cache survives application restarts
	•	Corrupted or outdated assets are re-fetched automatically

This enables:
	•	Fast startup after first run
	•	Offline usage once cached
	•	Stateless remote hosting

---

## SML Runtime Parsing

### General Concept

SML is parsed at runtime and converted into a live Godot scene graph.
	•	No .tscn files
	•	No Godot editor scenes
	•	Everything is built dynamically

### Parsing Pipeline
```
SML Text
  ↓
Tokenizer
  ↓
AST / Node Description
  ↓
Godot Node Factory
  ↓
Live Node Tree
```

---

## Supported UI & Scene Nodes

### UI Nodes
| SML Element | Godot Node |
|-|-|
|Button|Button|
|Label|Label|
|TextEdit|TextEdit|
|Box|Control / Panel|
|Row|HBoxContainer|
|Column|VBoxContainer|
|Tab|TabContainer|
|Video|VideoStreamPlayer|

Each node supports:
	•	Properties (text, size, alignment, visibility)
	•	IDs for event binding
	•	Optional actions (handled in C#)

---

## 3D & Media Nodes

### 3D Objects
	•	Loaded from .glb / .gltf
	•	Exported from Blender
	•	Instantiated as:
	•	Node3D
	•	MeshInstance3D
	•	AnimationPlayer (if present)

### Animation Controls
Built-in UI controls for:
	•	Play
	•	Stop
	•	Rewind
	•	Scrubbing (timeline)
	•	Perspective switching
	•	Free rotation using mouse (RMB drag)

### Camera Interaction
	•	RMB pressed → free orbit
	•	Scroll → zoom
	•	Optional preset viewpoints

---

## Input & Interaction Model
	•	Godot input system is used directly
	•	UI and 3D interactions coexist
	•	No scripting inside SML
	•	All logic lives in C#

## Example responsibilities:
	•	Button clicks → C# handlers
	•	Animation control → C# controller
	•	Camera movement → C# input logic

---

## Logic Layer (C#)

### There is no embedded scripting language.
	•	All behavior is implemented in C#
	•	SML only references logical actions or IDs
	•	Runner resolves actions at runtime

### Benefits:
	•	Full Godot API access
	•	Strong typing
	•	Easy debugging
	•	No sandbox complexity

---

## Error Handling & Robustness
	•	Unknown SML elements → warning, not crash
	•	Unknown properties → ignored with log entry
	•	Missing assets → placeholder + error message
	•	Manifest failure → startup abort with diagnostic info

---

## Logging & Diagnostics
	•	Structured logging categories:
	•	Manifest
	•	Assets
	•	SML
	•	UI
	•	3D
	•	Input
	•	Optional on-screen debug overlay
	•	Asset cache inspection mode

---

## Action strings (SML)

You now have three built-in clicked: action types:
	•	page:<path> → in-app navigation (loads another SML page)
	•	action:<name> → calls project logic in the single C# project file
	•	web:<url> → opens an external web page in the system browser

### Examples
```qml
Button { label: "Go to Bla" clicked: "page:xyz.sml" }

Button { label: "Save" clicked: "action:saveProfile" }

Button { label: "Open Website" clicked: "web:http://example.com/test.html" }
Button { label: "Open Website" clicked: "web:file:///home/user/test.html" }

```
--- 

## Runtime behavior

1) page:<path>

Intent: Replace the current page with another SML page.

Runner flow:
	1.	LoadPage(path)
	2.	Fetch from cache or remote (using manifest timestamps/hashes)
	3.	Parse SML → build Godot Node tree
	4.	Swap page root node
	5.	Notify project logic: OnPageLoaded(path) (optional but useful)

2) action:<name>

Intent: Delegate to project-specific behavior implemented in the single C# file.

Runner flow:
	1.	Extract action name
	2.	Call ProjectLogic.OnAction(name)

3) web:<url>

Intent: Open a URL externally (system default browser).

Runner flow (Godot/C#):
	•	Validate scheme (allow only http/https/file)
	•	Call Godot: OS.ShellOpen(url)

---

## Parsing rules (recommended)
	•	Split only on the first colon: kind:value
	•	kind is case-insensitive (page, action, web)
	•	value is kept as-is (may contain ://, query strings, etc.)
	•	Unknown kind → log warning and ignore (do not break UI)

---

## C# Action Binding / Dispatcher

The Runner now uses a **central action dispatcher** (`UiActionDispatcher`) that resolves actions in this order:

1. typed action in `clicked` (e.g. `web:https://...`, `page:...`, `action:...`)
2. typed action in `action`
3. plain `action` handler lookup
4. source `id` handler lookup
5. plain `clicked` as `id` handler lookup

If nothing matches, the dispatcher logs a warning (no hard crash).

### Bind SML IDs to C# handlers

You can register handlers in `Main` via `configureActions` (passed into `SmlUiLoader`):

- `RegisterActionHandlerIfMissing("save", handler)`
- `RegisterIdHandlerIfMissing("saveBtn", handler)`
- `SetPageHandlerIfMissing(path => ...)`

This keeps SML declarative while wiring behavior centrally in C#.

---

## Implemented bootstrap (Task: Manifest & Asset System)

The first backlog task is now implemented in the Runner as a startup pipeline:

1. `Main` loads `manifest.sml` from `ManifestUrl`
2. `ManifestLoader` parses and validates the SML manifest
3. `AssetCacheManager` compares remote entries with local cache metadata
4. only changed assets are downloaded
5. cache metadata is persisted to `user://cache/metadata.json`

### New runtime components

- `Runtime/Sml/SmlParser.cs` – lightweight SML parser (C++ parser semantics adapted to C#)
- `Runtime/Manifest/ManifestLoader.cs` – HTTP fetch + manifest validation
- `Runtime/Assets/AssetCacheManager.cs` – cache diff, delta download, metadata persistence
- `Runtime/Logging/RunnerLogger.cs` – structured subsystem logging wrapper

### Manifest schema

Root node must be `Manifest`.

Root properties:

- `version: int` (optional, default `1`)
- `baseUrl: string` (optional)
- `entryPoint: string` (optional)

Child nodes:

- `Asset { ... }`

Asset properties:

- `id: string` (required)
- `path: string` (required)
- `hash: string` (required, SHA-256; `sha256:` prefix is accepted)
- `url: string` (optional; if missing, `path` + `baseUrl`/manifest URL are used)
- `type: string` (optional)
- `size: int` (optional)

Numeric rule for SML in Runner:

- **Integer-only numeric values** (no float support).
- Example: `percent: 90` instead of `percent: 0.9`.

See `manifest.example.sml` for a concrete example.

### How to use

1. Set `ManifestUrl` on the `Main` node (in `main.tscn`) to your hosted `manifest.sml`
2. Keep `EnableStartupSync = true`
3. Start Runner → it syncs cache before continuing

If `ManifestUrl` is empty, startup sync is skipped with a warning.