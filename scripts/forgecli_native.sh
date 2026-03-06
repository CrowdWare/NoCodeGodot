#!/usr/bin/env bash
set -euo pipefail

REPO_ROOT="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
CLI_NATIVE_DIR="$REPO_ROOT/ForgeCli.Native"
CLI_NATIVE_BUILD_DIR="$CLI_NATIVE_DIR/build"
CLI_NATIVE_BIN="$CLI_NATIVE_BUILD_DIR/forgecli-native"

ensure_cli_native_built() {
  if [[ -x "$CLI_NATIVE_BIN" ]]; then
    return 0
  fi

  if ! command -v cmake >/dev/null 2>&1; then
    echo "ERROR: cmake not found. Install cmake to build ForgeCli.Native." >&2
    exit 1
  fi

  echo "ForgeCli.Native binary missing, building it now..."
  cmake -S "$CLI_NATIVE_DIR" -B "$CLI_NATIVE_BUILD_DIR" -DCMAKE_BUILD_TYPE=Release
  cmake --build "$CLI_NATIVE_BUILD_DIR" --config Release
}

ensure_native_env_defaults() {
  if [[ -z "${SML_NATIVE_LIB_DIR:-}" && -d "$REPO_ROOT/SMLCore.Native/build" ]]; then
    export SML_NATIVE_LIB_DIR="$REPO_ROOT/SMLCore.Native/build"
  fi
  if [[ -z "${SMS_NATIVE_LIB_DIR:-}" && -d "$REPO_ROOT/SMSCore.Native/build" ]]; then
    export SMS_NATIVE_LIB_DIR="$REPO_ROOT/SMSCore.Native/build"
  fi
}

MODE="${1:-}"
if [[ -z "$MODE" ]]; then
  echo "Usage: $0 [new|validate] [args...]" >&2
  exit 1
fi
shift || true

case "$MODE" in
  new)
    ensure_cli_native_built
    exec "$CLI_NATIVE_BIN" new "$@"
    ;;
  validate)
    ensure_cli_native_built
    ensure_native_env_defaults
    exec "$CLI_NATIVE_BIN" validate "$@"
    ;;
  *)
    echo "Unknown mode '$MODE'. Usage: $0 [new|validate] [args...]" >&2
    exit 1
    ;;
esac

