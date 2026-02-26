#!/usr/bin/env bash
set -euo pipefail

REPO_ROOT="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"

# --- Godot binary resolution ---
if [[ -n "${GODOT_BIN:-}" && -x "${GODOT_BIN}" ]]; then
  : # use it
elif command -v godot >/dev/null 2>&1; then
  GODOT_BIN="$(command -v godot)"
elif [[ -x "/Applications/Godot_mono.app/Contents/MacOS/Godot" ]]; then
  GODOT_BIN="/Applications/Godot_mono.app/Contents/MacOS/Godot"
elif [[ -x "$HOME/Applications/Godot_mono.app/Contents/MacOS/Godot" ]]; then
  GODOT_BIN="$HOME/Applications/Godot_mono.app/Contents/MacOS/Godot"
elif [[ -x "/opt/godot/godot" ]]; then
  GODOT_BIN="/opt/godot/godot"
else
  # Only required for Godot modes – fail later if needed
  GODOT_BIN=""
fi

RUNNER_PATH="$REPO_ROOT/ForgeRunner"

DEFAULT_UI="file://$REPO_ROOT/docs/Default/app.sml"
SAMPLE_UI="file://$REPO_ROOT/docs/ForgeDesigner/app.sml"
DOCKING_SAMPLE_UI="file://$REPO_ROOT/samples/docking_demo.sml"

require_godot() {
  if [[ -z "$GODOT_BIN" ]]; then
    echo "ERROR: Godot binary not found. Set GODOT_BIN=/path/to/godot" >&2
    exit 1
  fi
}

generate_version() {
  local year month day hour min
  year=$(date +%Y); month=$(date +%m); day=$(date +%d)
  hour=$(date +%H); min=$(date +%M)
  local major=$(( (year - 2014) / 10 ))
  local year_part=$(( (year - 2014) - 10 ))
  local v="${major}.${year_part}${month}.${day}${hour}${min}"
  echo "${v:0:11}"
}

MODE="${1:-}"

if [[ -z "$MODE" ]]; then
  echo "Bitte Modus wählen:"
  echo "  1) default   -> docs/Default/app.sml"
  echo "  2) designer  -> docs/ForgeDesigner/app.sml"
  echo "  3) docking   -> samples/docking_demo.sml"
  echo "  4) none      -> ohne URL-Override"
  echo "  5) docs      -> SML/SMS Docs generieren (headless)"
  echo "  6) build     -> App bauen"
  echo "  7) theme     -> theme.tres aus theme.sml generieren (headless)"
  echo "  8) manifest  -> manifest.sml für alle Docs generieren"
  echo "  9) publish   -> manifest + git commit + git push"
  echo " 10) export    -> macOS Release bauen (Godot export)"
  echo " 11) app       -> ForgeRunner.app starten (Release)"
  echo " 12) release   -> version setzen + export + tag + GitHub Release (default: pre)"
  read -r -p "Auswahl [1-12]: " CHOICE

  case "$CHOICE" in
    1) MODE="default" ;;
    2) MODE="designer" ;;
    3) MODE="docking" ;;
    4) MODE="none" ;;
    5) MODE="docs" ;;
    6) MODE="build" ;;
    7) MODE="theme" ;;
    8) MODE="manifest" ;;
    9) MODE="publish" ;;
    10) MODE="export" ;;
    11) MODE="app" ;;
    12) MODE="release" ;;
    *)
      echo "Ungültige Auswahl. Abbruch."
      exit 1
      ;;
  esac
fi

case "$MODE" in
  default)
    require_godot
    echo "Starting ForgeRunner with docs/Default/app.sml"
    exec "$GODOT_BIN" --path "$RUNNER_PATH" -- --url "$DEFAULT_UI"
    ;;
  designer|sample)
    require_godot
    echo "Starting ForgeRunner with docs/ForgeDesigner/app.sml"
    exec "$GODOT_BIN" --path "$RUNNER_PATH" -- --url "$SAMPLE_UI"
    ;;
  docking)
    require_godot
    echo "Starting ForgeRunner with samples/docking_demo.sml"
    exec "$GODOT_BIN" --path "$RUNNER_PATH" -- --url "$DOCKING_SAMPLE_UI"
    ;;
  none)
    require_godot
    echo "Starting ForgeRunner ohne startup URL override"
    exec "$GODOT_BIN" --path "$RUNNER_PATH" -- --reset-start-url
    ;;
  docs)
    require_godot
    echo "Generating SML element docs..."
    "$GODOT_BIN" --headless --path "$RUNNER_PATH" --script "$REPO_ROOT/tools/generate_sml_element_docs.gd" -- --out "$REPO_ROOT/docs"

    echo "Generating SMS runtime function docs..."
    "$GODOT_BIN" --headless --path "$RUNNER_PATH" --script "$REPO_ROOT/tools/generate_sms_functions_docs.gd"

    echo "Generating SML resource system docs..."
    "$GODOT_BIN" --headless --path "$RUNNER_PATH" --script "$REPO_ROOT/tools/generate_sml_resources_docs.gd"

    echo "Documentation generation completed."
    exit 0
    ;;
  build)
    echo "Building the app..."
    dotnet build "$REPO_ROOT/ForgeRunner/ForgeRunner.csproj"
    ;;
  theme)
    require_godot
    echo "Generating theme.tres from theme.sml..."
    "$GODOT_BIN" --headless --path "$RUNNER_PATH" --script "$REPO_ROOT/tools/generate_theme.gd"
    echo "Theme generation completed."
    exit 0
    ;;
  manifest)
    bash "$REPO_ROOT/scripts/build_manifest.sh"
    ;;
  publish)
    bash "$REPO_ROOT/scripts/build_manifest.sh"

    cd "$REPO_ROOT"
    COMMIT_MSG="${2:-"manifest: rebuild $(date +%Y-%m-%d)"}"
    git add .
    git commit -m "$COMMIT_MSG"
    git push
    echo "Published."
    ;;
  export)
    require_godot
    echo "Exporting macOS release..."
    "$GODOT_BIN" --headless --path "$RUNNER_PATH" --export-release "macOS"
    echo "Export completed -> ForgeRunner.app"
    ;;
  app)
    APP="$REPO_ROOT/ForgeRunner.app/Contents/MacOS/ForgeRunner"
    if [[ ! -x "$APP" ]]; then
      echo "ERROR: ForgeRunner.app not found. Run './run.sh export' first." >&2
      exit 1
    fi
    echo "Starting ForgeRunner.app..."
    exec "$APP" --url "$DEFAULT_UI"
    ;;
  release)
    require_godot
    if ! command -v gh >/dev/null 2>&1; then
      echo "ERROR: gh CLI not found. Install via: brew install gh" >&2
      exit 1
    fi

    CHANNEL="${2:-pre}"   # pre | alpha | beta | stable
    VERSION="$(generate_version)"
    TAG="v$VERSION"

    case "$CHANNEL" in
      stable)
        PRERELEASE_FLAG=""
        TITLE="Forge $TAG"
        ;;
      *)
        PRERELEASE_FLAG="--prerelease"
        TITLE="Forge $TAG ($CHANNEL)"
        ;;
    esac

    echo "Release $TAG [$CHANNEL]"

    echo "Setting version in all projects..."
    bash "$REPO_ROOT/scripts/set_version.sh" "$VERSION"

    echo "Exporting macOS release..."
    "$GODOT_BIN" --headless --path "$RUNNER_PATH" --export-release "macOS"

    ZIP="$REPO_ROOT/ForgeRunner-${TAG}-macOS.zip"
    echo "Zipping ForgeRunner.app -> $(basename "$ZIP")..."
    cd "$REPO_ROOT"
    zip -r -q "$ZIP" ForgeRunner.app

    echo "Committing version bump + tagging $TAG..."
    git add ForgeRunner/ForgeRunner.csproj SMLCore/SMLCore.csproj SMSCore/SMSCore.csproj
    git commit -m "release: $TAG [$CHANNEL]"
    git tag "$TAG"
    git push
    git push origin "$TAG"

    echo "Creating GitHub Release $TAG..."
    gh release create "$TAG" "$ZIP" \
      --title "$TITLE" \
      --generate-notes \
      $PRERELEASE_FLAG

    rm "$ZIP"
    echo "Release $TAG [$CHANNEL] published."
    ;;
  *)
    echo "Usage: $0 [default|designer|docking|none|docs|build|theme|manifest|publish|export|app|release]"
    exit 1
    ;;
esac
