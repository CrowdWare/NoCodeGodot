# Tech Demos

Small startup demos for validating root-specific download progress behavior.

## Demos

- `DownloadSplashDemo`
  - Root: `SplashScreen`
  - Progress: embedded `ProgressBar` inside splash UI.
- `DownloadWindowOverlayDemo`
  - Root: `Window`
  - Progress: startup overlay provided by runtime.
- `DownloadTerminalDemo`
  - Root: `Terminal`
  - Progress: console/log output (runtime progress milestones).

## Run

Example (from repository root):

```bash
./run.sh none --url "file://$PWD/docs/TechDemos/DownloadSplashDemo/app.sml"
./run.sh none --url "file://$PWD/docs/TechDemos/DownloadWindowOverlayDemo/app.sml"
./run.sh none --url "file://$PWD/docs/TechDemos/DownloadTerminalDemo/app.sml"
```

## Notes

- Regenerate manifests after editing demo files:

```bash
./run.sh manifest
```

- Root ready event convention:
  - Alias hooks exist for roots: `window.ready()`, `terminal.ready()`, `splash.ready()`.
  - Preferred hook is root-id based: `on <rootId>.ready()`.
