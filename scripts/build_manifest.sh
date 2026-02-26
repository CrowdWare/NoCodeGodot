#!/usr/bin/env bash
set -euo pipefail

REPO_ROOT="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
SCRIPT="$REPO_ROOT/scripts/generate_manifest.py"

echo "Generating manifest for docs/ForgeDesigner..."
python3 "$SCRIPT" --root "$REPO_ROOT/docs/ForgeDesigner" --entry app.sml --output manifest.sml

echo "Generating manifest for docs/Default..."
python3 "$SCRIPT" --root "$REPO_ROOT/docs/Default" --entry app.sml --output manifest.sml

echo "Manifest generation completed."
