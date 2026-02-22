# Asset Pipeline (Blender → glTF → Runner)

This guide describes the practical asset flow used by Forge-Runner.

## 1) Author in Blender

- Build/rig/animate your model in Blender.
- Keep object and animation names stable (used in runtime lookups and debugging).
- Apply transforms where appropriate (`Ctrl+A`) before export.

## 2) Export to glTF / GLB

Recommended export target:

- **Format:** `glTF Binary (.glb)`

Typical export settings:

- Include: selected objects (if needed)
- Apply Modifiers: enabled
- Animation: enabled (if clips needed)
- +Y Up (default glTF convention)

Result: one `.glb` file (plus optional external textures if not embedded).

## 3) Place assets in published content tree

Put runtime content into a manifest root, e.g.:

```text
docs/Default/
  app.sml
  assets/models/Idle.glb
  assets/models/Opa.glb
```

Keep paths relative and stable; those paths are referenced in SML and manifest entries.

## 4) Reference assets from SML

Example for `Viewport3D`:

```sml
Viewport3D {
    id: "heroView"
    model: "assets/models/Idle.glb"
    playFirstAnimation: true
}
```

For markdown/image/video assets, use `src` / `source` URLs resolved by the runtime URI resolver.

## 5) Generate manifest

Use the manifest generator:

```bash
python /Users/art/SourceCode/Forge/scripts/generate_manifest.py --root docs/Default --entry app.sml
```

What it does:

- scans files recursively
- computes SHA-256 per file
- writes `manifest.sml` with `Files { File { path hash size } }`
- excludes `.import` and `.cs` by default

## 6) Publish content

Publish the manifest root (e.g. `docs/Default`) to static hosting (GitHub Pages/CDN/web server).

Runtime expects a reachable manifest URL (or direct UI URL in non-manifest mode).

## 7) Runner startup and caching

At startup in manifest mode:

1. load `manifest.sml`
2. build sync plan (`downloadCount`, `plannedBytes`)
3. download changed/missing files only
4. verify hash (single retry on mismatch)
5. store cache under `user://cache/<manifest-url-hash>/files`
6. load entry SML from local cache

## Common Pitfalls

- **Wrong relative paths:** Ensure SML paths match manifest `path` values.
- **Missing manifest update:** Re-generate manifest after changing any asset.
- **Hash mismatch:** Usually stale upload or modified file after manifest generation.
- **Godot import confusion:** Runtime loads cached files via file URLs; project editor imports are separate concerns.
