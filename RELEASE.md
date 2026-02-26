# Release Workflow

## Prerequisites

```bash
brew install gh
gh auth login
```

## Quick Reference

| Command | Result |
|---|---|
| `./run.sh release` | Pre-release (default) |
| `./run.sh release alpha` | Pre-release, title "… (alpha)" |
| `./run.sh release beta` | Pre-release, title "… (beta)" |
| `./run.sh release stable` | Full release (no pre-release flag) |

## What `./run.sh release` does

1. **Generate version** – automatically from current date and time
   Format: `major.yearMonth.dayHourMin` (e.g. `1.202.261430`)
   Epoch starts at 2014, increments every 10 minutes.

2. **Set version** – writes version into all projects:
   - `ForgeRunner/ForgeRunner.csproj`
   - `SMLCore/SMLCore.csproj`
   - `SMSCore/SMSCore.csproj`

3. **macOS export** – Godot headless export → `ForgeRunner.app`

4. **Zip** – `ForgeRunner-v1.202.261430-macOS.zip`

5. **Git commit + tag + push**
   ```
   git add <csproj files>
   git commit -m "release: v1.202.261430 [beta]"
   git tag v1.202.261430
   git push + git push origin v1.202.261430
   ```

6. **Create GitHub Release** via `gh release create`
   - Release notes are auto-generated from commits (`--generate-notes`)
   - Zip is attached as a release asset
   - Pre-release flag is set unless channel is `stable`

7. **Cleanup** – zip is deleted after upload

## Set version manually

```bash
./scripts/set_version.sh 1.202.261430
```

## Other useful commands

```bash
./run.sh manifest            # Regenerate manifest.sml for all docs
./run.sh publish             # manifest + git add . + commit + push
./run.sh publish "fix: ..."  # publish with custom commit message
./run.sh export              # Build macOS app only (no release)
./run.sh app                 # Start ForgeRunner.app
```
