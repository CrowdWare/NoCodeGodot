# Manifest Specification (Forge-Runner)

This document defines the manifest formats accepted by the Runner.

## Root Node

The root node must be:

```sml
Manifest { ... }
```

## Supported Formats

The Runner supports two manifest styles:

1. **Current Files format** (`Files` + `File`)
2. **Legacy Asset format** (`Asset` entries directly under `Manifest`)

Both may contain optional global metadata.

---

## Global Properties (Manifest)

| Property | Type | Required | Notes |
|---|---|---:|---|
| `version` | `int` or `string` | no | Parsed as string in runtime metadata |
| `entry` | `string` | no | Preferred entry key |
| `entryPoint` | `string` | no | Legacy alias for `entry` |
| `baseUrl` | `string` (absolute URL) | no | Base URL for resolving relative asset URLs |

If neither `entry` nor `entryPoint` is present, runtime defaults to `app.sml` as fallback entry expectation.

---

## Current Format (`Files`)

```sml
Manifest {
    version: "auto-3a12aaa8039065a1"
    entry: "app.sml"

    Files {
        File { path: "app.sml" hash: "sha256:<hex>" size: 2240 }
        File { path: "assets/models/Idle.glb" hash: "sha256:<hex>" size: 8693124 }
    }
}
```

### File Properties

| Property | Type | Required | Notes |
|---|---|---:|---|
| `path` | `string` | yes | Relative path in package |
| `hash` | `string` | yes | SHA-256 (`sha256:<hex>` accepted) |
| `size` | `int`/`long` | no | Planned byte size for progress calculation |
| `url` | `string` | no | Optional absolute/relative override URL |

Runtime behavior:

- `id` is implicit and set to `path` for `File` entries.
- If `url` is omitted, `url = path` and is resolved via `baseUrl` or manifest URL location.

---

## Legacy Format (`Asset`)

```sml
Manifest {
    version: 1
    baseUrl: "https://example.com/content/"
    entryPoint: "ui/main.sml"

    Asset {
        id: "ui-main"
        path: "ui/main.sml"
        hash: "sha256:<hex>"
        type: "ui"
        size: 1024
    }
}
```

### Asset Properties

| Property | Type | Required | Notes |
|---|---|---:|---|
| `id` | `string` | yes | Logical identifier |
| `path` | `string` | yes | Relative package path |
| `hash` | `string` | yes | SHA-256 (`sha256:<hex>` accepted) |
| `url` | `string` | no | Optional absolute/relative override URL |
| `type` | `string` | no | Optional semantic type (`ui`, `model`, ...) |
| `size` | `int`/`long` | no | Planned byte size |

---

## URL Resolution

For relative `url` (or `path` used as URL):

1. If `baseUrl` exists and is absolute, resolve against `baseUrl`.
2. Otherwise resolve against the absolute manifest URL.

Absolute URLs are kept unchanged.

---

## Validation / Constraints

- Root must be `Manifest`.
- Unknown nodes are warned, not fatal.
- `hash` is required per asset entry.
- Relative asset paths must be safe (no `..` segments in cache path normalization).
- Numeric parsing in SML runtime is integer-oriented (no float literals in manifest fields).

---

## Cache + Sync Semantics

During sync:

- Compare each asset by `path + hash + file-exists`.
- Download only missing/changed entries.
- Verify SHA-256; retry download once on mismatch.
- Persist metadata atomically (`metadata.json`).
- Persist cached manifest atomically.

---

## Generator

Manifest generation helper script:

```bash
python /Users/art/SourceCode/Forge/scripts/generate_manifest.py --root docs/Default --entry app.sml
```

Default excludes include `.import` and `.cs`.
