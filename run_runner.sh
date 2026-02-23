#!/usr/bin/env bash
set -euo pipefail

GODOT_BIN="/Applications/Godot_mono.app/Contents/MacOS/Godot"
REPO_ROOT="/Users/art/SourceCode/Forge"
RUNNER_PATH="$REPO_ROOT/ForgeRunner"

DEFAULT_UI="file://$REPO_ROOT/docs/Default/app.sml"
SAMPLE_UI="file://$REPO_ROOT/docs/ForgeDesigner/app.sml"
DOCKING_SAMPLE_UI="file://$REPO_ROOT/samples/docking_demo.sml"

MODE="${1:-}"

if [[ -z "$MODE" ]]; then
  echo "Bitte Startmodus wählen:"
  echo "  1) default  -> docs/Default/app.sml"
  echo "  2) Designer -> docs/ForgeDesigner/app.sml"
  echo "  3) docking  -> samples/docking_demo.sml"
  echo "  4) none     -> ohne URL-Override"
  echo "  5) docs     -> generate SML/SMS docs (headless)"
  echo "  6) build    -> buil app"
  read -r -p "Auswahl [1-6]: " CHOICE

  case "$CHOICE" in
    1) MODE="default" ;;
    2) MODE="sample" ;;
    3) MODE="docking" ;;
    4) MODE="none" ;;
    5) MODE="docs" ;;
    6) MODE="build" ;;
    *)
      echo "Ungültige Auswahl. Abbruch."
      exit 1
      ;;
  esac
fi

case "$MODE" in
  default)
    echo "Starting Forge-Runner with Default app.sml"
    exec "$GODOT_BIN" --path "$RUNNER_PATH" -- --url "$DEFAULT_UI"
    ;;
  sample)
    echo "Starting Forge-Runner with ForgeDesigner app.sml"
    exec "$GODOT_BIN" --path "$RUNNER_PATH" -- --url "$SAMPLE_UI"
    ;;
  docking)
    echo "Starting Forge-Runner with Docking sample from samples/docking_demo.sml"
    exec "$GODOT_BIN" --path "$RUNNER_PATH" -- --url "$DOCKING_SAMPLE_UI"
    ;;
  none)
    echo "Starting ForgeRunner without startup URL override (reset persisted startUrl)"
    exec "$GODOT_BIN" --path "$RUNNER_PATH" -- --reset-start-url
    ;;
  docs)
    echo "Generating SML element docs..."
    "$GODOT_BIN" --headless --path "$RUNNER_PATH" --script "$REPO_ROOT/tools/generate_sml_element_docs.gd" -- --out "$REPO_ROOT/docs"

    echo "Generating SMS runtime function docs..."
    "$GODOT_BIN" --headless --path "$RUNNER_PATH" --script "$REPO_ROOT/tools/generate_sms_functions_docs.gd"

    echo "Documentation generation completed."
    exit 0
    ;;
  build)
    echo "Building the app..."
    dotnet build "$REPO_ROOT/ForgeRunner/ForgeRunner.csproj"
    ;;
  *)
    echo "Usage: $0 [default|sample|docking|none|docs]"
    exit 1
    ;;
esac