# Runtime Lifecycle (ForgeRunner)

This document describes the startup/runtime flow from process start to interactive UI.

## Lifecycle Diagram

```text
┌──────────────────────────────┐
│ Process Start (Main._Ready)  │
└──────────────┬───────────────┘
               │
               ▼
      Load startup settings
               │
               ▼
      Resolve startup URL
   (manifest URL or direct UI)
               │
               ▼
      EnableStartupSync ?
         ├─ no ──► legacy/direct UI resolve
         └─ yes
               │
               ▼
      Determine manifest mode
         ├─ direct UI URL ─► resolve/cache via URI resolver
         └─ manifest URL
               │
               ▼
        Load + parse manifest
               │
               ▼
          Build sync plan
               │
               ▼
      Download changed assets
      + hash verify + cache write
               │
               ▼
        Resolve cached entry SML
               │
               ▼
        Parse SML + preprocess
      (Markdown nodes, URI resolve)
               │
               ▼
       Build Godot control tree
      (NodeFactory + PropertyMap)
               │
               ▼
          Attach UI to scene
       (layout/fixed scaling mode)
               │
               ▼
         Runtime interaction
      (actions, camera, animation)
```

## Step-by-Step

1. **Bootstrap (`Main._Ready`)**
   - load startup settings from `user://startup_settings.sml`
   - configure logger
   - resolve startup URL from params/settings/export defaults

2. **Startup Mode Selection**
   - if sync disabled: load direct/legacy URL path
   - if sync enabled: decide between manifest mode and direct UI mode

3. **Manifest Sync (if manifest mode)**
   - fetch and parse manifest
   - compute sync plan
   - optionally show progress overlay for larger downloads
   - download only changed files
   - verify SHA-256 (single retry)
   - persist metadata and manifest atomically

4. **Fallback Strategy**
   - on manifest failure: attempt cached entry fallback
   - if cache unavailable: use embedded `res://fallback/app.sml`

5. **UI Load / Build**
   - `SmlUiLoader` parses SML
   - preprocesses markdown nodes to runtime nodes
   - resolves asset URIs (`appres://`, `res://`, `user://`, `http(s)://`, `ipfs:/`)
   - `SmlUiBuilder` creates controls recursively
   - `NodePropertyMapper` applies properties + metadata

6. **Attach + Layout Runtime**
   - attach built control tree to canvas layer
   - apply scaling mode (`layout` or `fixed`)
   - apply runtime layout pass and viewport resize handling

7. **Interaction Runtime**
   - central `UiActionDispatcher` routes action/id/page/web intents
   - viewport/camera/animation action handlers execute per control metadata

## Logging Expectations

Typical startup logs include:

- boot source
- manifest/cache/download summary
- offline detection
- UI load result/failure
- scaling state snapshots
