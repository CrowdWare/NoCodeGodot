# Remote Content Caching for ForgeRunner

## Goal

Forge-Runner loads remote content (e.g., from GitHub Pages) and caches it locally so that:
	•	repeated starts are fast,
	•	offline start works,
	•	updates download only changed files.

## Scope
	•	All configuration/indexing is SML.
	•	Runtime uses C# and Reflection-based plugin loading (already working).
	•	This task focuses on remote content + caching + boot order.

⸻

## Remote Entry Files (SML)

### manifest.sml (required for caching)

The remote “app root” must include a manifest.sml which describes:
	•	a stable cache key (appId)
	•	the entry SML file
	•	the list of files with hashes

### Example structure:

```qml
Manifest {
    version: "2026-02-10-01"      // optional but recommended
    entry: "app.sml"

    Files {
        File { path: "app.sml" hash: "sha256:..." size: 1234 }
        File { path: "pages/home.sml" hash: "sha256:..." size: 2345 }
        File { path: "assets/logo.png" hash: "sha256:..." size: 3456 }
        // ...
    }
}
```
Notes:
	•	hash is required (sha256).
	•	size is optional but helpful for progress-bar/logging.
	•	ProgressBar shall be shown if whole content is larger than X bytes.

---

## Caching Rules

### Cache Layout

Cache is separated per hash(canonicalUrl):
	•	cache/{urlHash}/manifest.sml
	•	cache/{urlHash}/files/<path...>
	•	Optional versioning:
	•	cache/{urlHash}/versions/<version>/... (for rollback)

### Download Strategy
	1.	Always fetch manifest.sml first.
	2.	Compare the fetched manifest with the cached manifest:
	•	If version unchanged AND/OR all file hashes identical → no file downloads.
	•	If changed → download only files whose hash differs or are missing in cache.
	3.	After download:
	•	validate sha256 hash
	•	if mismatch: re-download once
	•	if still mismatch: fallback to last known good cached version (if available)

### Offline Behavior
	•	If offline and cached content exists → start from cache.
	•	If offline and no cache exists → show a minimal built-in fallback SML screen:
	•	“Offline and no cached content available.”

### Purge / Limits
	•	Support clearing cache:
	•	Clear cache for current appId
	•	Clear all caches
	•	Add a configurable maxCacheMB (e.g., 500 MB) and eviction strategy:
	•	simplest: keep last N versions per appId
	•	or LRU across appIds

⸻

## Boot / Start Sequence (URL Override for Desktop Icons)

### Goal

Default behavior loads CrowdWare content from GitHub Pages, but users can override via CLI so they can create multiple desktop icons with different start URLs.

### CLI Parameters
	•	--url <remoteBaseOrManifestUrl>
	•	If a base URL is provided, resolve manifest.sml as <baseUrl>/manifest.sml.
	•	If a direct manifest.sml URL is provided, use it directly.
	•	Optional:
	•	--clear-cache
	•	--reset-start-url

### Boot Order
	1.	--url parameter (highest priority)
	2.	persisted settings.startUrl
	3.	default CrowdWare URL (GitHub Pages) https://crowdware.github.io/Forge/SampleProject/UI.sml
	4.	built-in fallback screen (only if everything else fails)

⸻

## Default CrowdWare App Content (SML)

### Purpose

The default CrowdWare app should behave like a website / “feasibility studio”:
	•	lists Books, Apps, Reels
	•	can be extended with Documentation
	•	provides “Load URL…” and “Set as Start Content”

This is content, not runner logic. Runner must simply support loading it.

⸻

## Logging (Mandatory)

### On each start, log:
	•	BootSource: paramUrl | settingsStartUrl | defaultCrowdWare | fallback
	•	Cache: hit | miss
	•	Manifest: unchanged | changed | missing
	•	Downloads: <count> and <bytes>
	•	Offline: true | false

⸻

# Acceptance Criteria (Must Prove)
	1.	Online first start → downloads manifest + files → launches entry app.sml.
	2.	Offline start after caching → launches from cache.
	3.	Remote update (hash change) → downloads only changed files.
	4.	Unchanged manifest → no file downloads (manifest check only).
	5.	--url overrides default and settings, enabling multiple desktop icons per URL.
	6.	Cache clear works and forces a fresh download afterwards.