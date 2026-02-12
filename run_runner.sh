#!/usr/bin/env bash
set -euo pipefail

GODOT_BIN="/Applications/Godot_mono.app/Contents/MacOS/Godot"
REPO_ROOT="/Users/art/SourceCode/NoCodeGodot"
RUNNER_PATH="$REPO_ROOT/NoCodeRunner"

DEFAULT_UI="file://$REPO_ROOT/docs/Default/app.sml"
SAMPLE_UI="file://$REPO_ROOT/docs/NoCodeDesigner/app.sml"

MODE="${1:-}"

if [[ -z "$MODE" ]]; then
  echo "Bitte Startmodus wählen:"
  echo "  1) default  -> docs/Default/app.sml"
  echo "  2) Designer -> docs/NoCodeDesigner/app.sml"
  echo "  3) none     -> ohne URL-Override"
  read -r -p "Auswahl [1-3]: " CHOICE

  case "$CHOICE" in
    1) MODE="default" ;;
    2) MODE="sample" ;;
    3) MODE="none" ;;
    *)
      echo "Ungültige Auswahl. Abbruch."
      exit 1
      ;;
  esac
fi

case "$MODE" in
  default)
    echo "Starting NoCodeRunner with Default app.sml"
    exec "$GODOT_BIN" --path "$RUNNER_PATH" -- --url "$DEFAULT_UI"
    ;;
  sample)
    echo "Starting NoCodeRunner with NoCodeDesigner app.sml"
    exec "$GODOT_BIN" --path "$RUNNER_PATH" -- --url "$SAMPLE_UI"
    ;;
  none)
    echo "Starting NoCodeRunner without startup URL override (reset persisted startUrl)"
    exec "$GODOT_BIN" --path "$RUNNER_PATH" -- --reset-start-url
    ;;
  *)
    echo "Usage: $0 [default|sample|none]"
    exit 1
    ;;
esac