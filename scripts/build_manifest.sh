#!/usr/bin/env bash
set -euo pipefail

REPO_ROOT="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
SCRIPT="$REPO_ROOT/scripts/generate_manifest.py"

echo "Generating manifest for docs/ForgeDesigner..."
python3 "$SCRIPT" --root "$REPO_ROOT/docs/ForgeDesigner" --entry app.sml --output manifest.sml

echo "Generating manifest for docs/Default..."
python3 "$SCRIPT" --root "$REPO_ROOT/docs/Default" --entry app.sml --output manifest.sml

echo "Generating manifest for docs/TechDemos/DownloadSplashDemo..."
python3 "$SCRIPT" --root "$REPO_ROOT/docs/TechDemos/DownloadSplashDemo" --entry app.sml --output manifest.sml

echo "Generating manifest for docs/TechDemos/DownloadWindowOverlayDemo..."
python3 "$SCRIPT" --root "$REPO_ROOT/docs/TechDemos/DownloadWindowOverlayDemo" --entry app.sml --output manifest.sml

echo "Generating manifest for docs/TechDemos/DownloadTerminalDemo..."
python3 "$SCRIPT" --root "$REPO_ROOT/docs/TechDemos/DownloadTerminalDemo" --entry app.sml --output manifest.sml

echo "Manifest generation completed."
