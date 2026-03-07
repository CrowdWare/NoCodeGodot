# Native Asset Download + Cache Manager

## Goal
Port `AssetCacheManager.cs` to C++ so that `ForgeRunner.Native` can fetch remote
manifests + assets over HTTP/HTTPS, verify content hashes, store them in a local
cache directory, and serve them to the UI builder — matching the C# behaviour.

## Context
`ForgeRunner.Native/src/main.cpp` (1665 lines) already has download scaffolding
and a `ManifestInfo` struct, but the persistent disk-level cache (hash verification,
metadata index, atomic writes) and the HTTP fetch integration are incomplete.

`ForgeRunner.Native/src/forge_runner_main.cpp` currently only supports `file://` URLs
and cannot fetch `http://` / `https://` manifests at all.

## Subsystems to Port

### HTTP Fetch (libcurl or Godot HTTPClient)
- Prefer `godot::HTTPClient` (already in godot-cpp) to avoid adding libcurl as a
  build dependency.
- Support progress callbacks for the splash screen progress bar.
- Retry logic: 3 attempts with exponential back-off on 5xx / network errors.

### Cache Directory
- Location: OS-appropriate cache dir (e.g. `~/.cache/forge-runner/assets/` on Linux/macOS,
  `%LOCALAPPDATA%\ForgeRunner\cache\` on Windows).
- Index file: `cache/index.sml` — one entry per asset:
  `url`, `hash` (SHA-256 hex), `local_file`, `last_fetched`.
- Atomic writes: write to `.tmp` → fsync → rename.

### Hash Verification
- SHA-256 of downloaded bytes must match the manifest-provided hash.
- On mismatch: delete cached file, re-download once, hard-error if still mismatching.

### Asset Resolver Integration
- `RunnerUriResolver` in C#: maps `res://` paths to either:
  a) Files in the local cache dir after download, or
  b) Files relative to `FORGE_RUNNER_APPRES_ROOT` (local dev mode).
- Port to `UiBuilder::resolve_asset_path()` in `forge_ui_builder.cpp`.

### Manifest Loader
- Parse manifest SML: `files:` list with `url` + `hash` + `path` entries.
- Determine which files need downloading (missing or hash mismatch).
- Download only the delta; show per-file progress.

## Acceptance Criteria
- `ForgeRunner.Native` can start with `FORGE_RUNNER_URL=https://...` and display
  the app after downloading + caching all required assets.
- On second run, no network requests are made if hashes match.
- Hash mismatch forces re-download.
- Progress is forwarded to the splash-screen progress bar.

## Test Strategy — Mockup Web Server

For unit / integration tests a lightweight mock HTTP server should be set up
that serves static SML files and assets without requiring network access:

- Use Python's built-in `http.server` (one-liner) or a tiny C++ or Go server.
- Start it on a random port before each test run; shut it down after.
- Serve a fixture directory (`tests/fixtures/remote_app/`) containing:
  - `app.sml` — a minimal SML document
  - `manifest.sml` — a manifest referencing a small image and a CSS-like layout
  - `icon.png` — small test asset
- Test cases:
  1. First run: all files are downloaded and cached (index populated).
  2. Second run: no HTTP requests made (hashes match).
  3. Hash mismatch: stale cache entry is evicted and re-downloaded.
  4. Server unavailable: graceful error message, no crash.
- The server URL is passed via `FORGE_RUNNER_URL=http://localhost:{port}/app.sml`.

## Reference
- C#: `ForgeRunner/Runtime/Assets/AssetCacheManager.cs`
- C#: `ForgeRunner/Runtime/Assets/RunnerUriResolver.cs`
- C#: `ForgeRunner/Runtime/Manifest/ManifestLoader.cs`
- C++: `ForgeRunner.Native/src/main.cpp` (existing scaffold)
