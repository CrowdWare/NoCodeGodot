#!/usr/bin/env bash
set -euo pipefail
echo ""
echo "███████╗ ██████╗ ██████╗  ██████╗ ███████╗   ██╗  ██╗██████╗"
echo "██╔════╝██╔═══██╗██╔══██╗██╔════╝ ██╔════╝   ██║  ██║██╔══██╗"
echo "█████╗  ██║   ██║██████╔╝██║  ███╗█████╗     ███████║██║  ██║"
echo "██╔══╝  ██║   ██║██╔══██╗██║   ██║██╔══╝     ╚════██║██║  ██║"
echo "██║     ╚██████╔╝██║  ██║╚██████╔╝███████╗        ██║██████╔╝"
echo "╚═╝      ╚═════╝ ╚═╝  ╚═╝ ╚═════╝ ╚══════╝        ╚═╝╚═════╝"
echo "Built with love, coffee, and a stubborn focus on simplicity."
echo ""
REPO_ROOT="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
OS_NAME="$(uname -s)"
LOCAL_RUN_INCLUDE="$REPO_ROOT/run.local.sh"
LOCAL_GODOT_CPP_PATH_FILE="$REPO_ROOT/.godot_cpp_dir"
NATIVE_HOST_DIR="$REPO_ROOT/ForgeRunner.Native/host"

# Load optional local overrides/env (git-ignored).
if [[ -f "$LOCAL_RUN_INCLUDE" ]]; then
  # shellcheck source=/dev/null
  source "$LOCAL_RUN_INCLUDE"
fi

# Allow persisting GODOT_CPP_DIR in a git-ignored local file.
if [[ -z "${GODOT_CPP_DIR:-}" && -f "$LOCAL_GODOT_CPP_PATH_FILE" ]]; then
  GODOT_CPP_DIR="$(head -n 1 "$LOCAL_GODOT_CPP_PATH_FILE" | tr -d '\r')"
  export GODOT_CPP_DIR
fi

build_native_lib() {
  local name="$1"
  local src_dir="$2"
  local build_dir="$3"
  local with_tests="${4:-false}"

  if ! command -v cmake >/dev/null 2>&1; then
    echo "ERROR: cmake not found. Install cmake to build ${name}." >&2
    return 1
  fi

  if [[ ! -d "$src_dir" ]]; then
    echo "ERROR: Missing source directory for ${name}: $src_dir" >&2
    return 1
  fi

  mkdir -p "$build_dir"
  echo "Configuring ${name}..."
  if [[ "$with_tests" == "true" ]]; then
    cmake -S "$src_dir" -B "$build_dir" -DCMAKE_BUILD_TYPE=Release -DBUILD_TESTING=ON
  else
    cmake -S "$src_dir" -B "$build_dir" -DCMAKE_BUILD_TYPE=Release
  fi
  echo "Building ${name}..."
  cmake --build "$build_dir" --config Release
}

build_forge_runner_native_host() {
  local src_dir="$REPO_ROOT/ForgeRunner.Native"
  local build_dir="$src_dir/build"
  local out_dir="$src_dir/dist"
  local host_dir="$src_dir/host"

  if [[ -z "${GODOT_CPP_DIR:-}" ]]; then
    echo "ERROR: GODOT_CPP_DIR is required to build ForgeRunner.Native." >&2
    echo "       Example: export GODOT_CPP_DIR=/absolute/path/to/godot-cpp" >&2
    echo "       Or store it once in $LOCAL_GODOT_CPP_PATH_FILE" >&2
    return 1
  fi

  if ! command -v cmake >/dev/null 2>&1; then
    echo "ERROR: cmake not found." >&2
    return 1
  fi

  mkdir -p "$build_dir"
  echo "Configuring ForgeRunner.Native..."
  cmake -S "$src_dir" -B "$build_dir" \
    -DCMAKE_BUILD_TYPE=Release \
    -DGODOT_CPP_DIR="$GODOT_CPP_DIR"
  echo "Building ForgeRunner.Native..."
  cmake --build "$build_dir" --config Release

  mkdir -p "$out_dir"
  local artifact=""
  local exe_artifact=""
  case "$OS_NAME" in
    Darwin)
      artifact="$build_dir/libforge_runner_native.dylib"
      exe_artifact="$build_dir/forge-runner-native"
      ;;
    Linux)
      artifact="$build_dir/libforge_runner_native.so"
      exe_artifact="$build_dir/forge-runner-native"
      ;;
    *)
      artifact="$build_dir/forge_runner_native.dll"
      exe_artifact="$build_dir/forge-runner-native.exe"
      ;;
  esac

  if [[ ! -f "$artifact" ]]; then
    echo "ERROR: ForgeRunner.Native artifact not found: $artifact" >&2
    return 1
  fi

  cp "$artifact" "$out_dir/"
  echo "ForgeRunner.Native artifact copied to $out_dir/$(basename "$artifact")"
  mkdir -p "$host_dir"
  cp "$artifact" "$host_dir/"
  echo "ForgeRunner.Native artifact copied to $host_dir/$(basename "$artifact")"

  if [[ ! -f "$exe_artifact" ]]; then
    echo "ERROR: ForgeRunner.Native executable not found: $exe_artifact" >&2
    return 1
  fi

  cp "$exe_artifact" "$out_dir/"
  chmod +x "$out_dir/$(basename "$exe_artifact")" || true
  echo "ForgeRunner.Native executable copied to $out_dir/$(basename "$exe_artifact")"
}

usage() {
  cat <<USAGE
Usage:
  ./run.sh [runner-args...]   Run ForgeRunner.Native with passed arguments (for example --url ...)
  ./run.sh run [runner-args]  Same as above (explicit mode)
  ./run.sh none               Run ForgeRunner.Native without arguments (runtime may restore last URL)
  ./run.sh default            Run local Default app (file://)
  ./run.sh designer           Run local ForgeDesigner app (file://)
  ./run.sh remote-default     Run hosted Default app (AppServer manifest)
  ./run.sh remote-designer    Run hosted ForgeDesigner app (AppServer manifest)
  ./run.sh poser              Run local ForgePoser app
  ./run.sh url <url>          Run any app by URL (file:// or http(s)://)
  ./run.sh docs               Generate SML/SMS documentation (headless Godot)
  ./run.sh build              Build native stack (default)
  ./run.sh build-native       Same as build
  ./run.sh build-host         Build ForgeRunner.Native only
  ./run.sh test               Run native tests (SMLCore.Native + SMSCore.Native)
  ./run.sh clean              Remove native build/dist folders

Environment:
  FORGE_RUNNER_NATIVE_BIN     Optional explicit native runner executable path
  GODOT_CPP_DIR               Required for build-host/build
  ./.godot_cpp_dir            Optional file with one line: absolute GODOT_CPP_DIR
  FORGE_APP_SERVER_BASE_URL   Base URL for hosted app manifests (default: https://crowdware.github.io/Forge)
USAGE
}

resolve_native_runner_bin() {
  if [[ -n "${FORGE_RUNNER_NATIVE_BIN:-}" ]]; then
    echo "$FORGE_RUNNER_NATIVE_BIN"
    return 0
  fi

  local dist="$REPO_ROOT/ForgeRunner.Native/dist"
  case "$OS_NAME" in
    Darwin|Linux)
      echo "$dist/forge-runner-native"
      ;;
    *)
      echo "$dist/forge-runner-native.exe"
      ;;
  esac
}

resolve_native_host_lib() {
  local src_dir="$REPO_ROOT/ForgeRunner.Native"
  case "$OS_NAME" in
    Darwin)
      if [[ -f "$src_dir/dist/libforge_runner_native.dylib" ]]; then
        echo "$src_dir/dist/libforge_runner_native.dylib"
      else
        echo "$src_dir/build/libforge_runner_native.dylib"
      fi
      ;;
    Linux)
      if [[ -f "$src_dir/dist/libforge_runner_native.so" ]]; then
        echo "$src_dir/dist/libforge_runner_native.so"
      else
        echo "$src_dir/build/libforge_runner_native.so"
      fi
      ;;
    *)
      if [[ -f "$src_dir/dist/forge_runner_native.dll" ]]; then
        echo "$src_dir/dist/forge_runner_native.dll"
      else
        echo "$src_dir/build/forge_runner_native.dll"
      fi
      ;;
  esac
}

resolve_godot_bin() {
  if [[ -n "${GODOT_BIN:-}" && -x "$GODOT_BIN" ]]; then
    echo "$GODOT_BIN"
    return 0
  fi
  if command -v godot4 >/dev/null 2>&1; then
    command -v godot4
    return 0
  fi
  if command -v godot >/dev/null 2>&1; then
    command -v godot
    return 0
  fi
  if [[ -x "/Applications/Godot.app/Contents/MacOS/Godot" ]]; then
    echo "/Applications/Godot.app/Contents/MacOS/Godot"
    return 0
  fi
  if [[ -x "/Applications/Godot_mono.app/Contents/MacOS/Godot" ]]; then
    echo "/Applications/Godot_mono.app/Contents/MacOS/Godot"
    return 0
  fi
  if [[ -x "$HOME/Applications/Godot.app/Contents/MacOS/Godot" ]]; then
    echo "$HOME/Applications/Godot.app/Contents/MacOS/Godot"
    return 0
  fi
  if [[ -x "$HOME/Applications/Godot_mono.app/Contents/MacOS/Godot" ]]; then
    echo "$HOME/Applications/Godot_mono.app/Contents/MacOS/Godot"
    return 0
  fi
  return 1
}

try_local_mode_override() {
  if ! declare -F forge_local_handle_mode >/dev/null 2>&1; then
    return 1
  fi

  case "$MODE" in
    release|docs|test|export|app|poser|publish|upd)
      forge_local_handle_mode "$MODE" "$@"
      return $?
      ;;
    *)
      return 1
      ;;
  esac
}

run_native_window_host() {
  local url="$1"
  if [[ ! -d "$NATIVE_HOST_DIR" ]]; then
    echo "ERROR: Native host project not found: $NATIVE_HOST_DIR" >&2
    return 1
  fi

  local host_lib
  host_lib="$(resolve_native_host_lib)"
  if [[ ! -f "$host_lib" ]]; then
    echo "ERROR: ForgeRunner.Native host library not found: $host_lib" >&2
    echo "Build it via './run.sh build-host' first." >&2
    return 1
  fi

  cp "$host_lib" "$NATIVE_HOST_DIR/" || true

  local godot_bin
  if ! godot_bin="$(resolve_godot_bin)"; then
    echo "ERROR: Godot binary not found." >&2
    echo "Set GODOT_BIN or install a 'godot4'/'godot' executable in PATH." >&2
    return 1
  fi

  echo "Starting ForgeRunner.Native window host with url: $url"
  FORGE_RUNNER_URL="$url" \
  FORGE_RUNNER_APPRES_ROOT="$REPO_ROOT/ForgeRunner.Native" \
    exec "$godot_bin" --path "$NATIVE_HOST_DIR"
}

MODE="${1:-}"
if [[ -n "$MODE" ]]; then
  shift || true
fi

RUNNER_ARGS=()
if [[ -n "$MODE" && "${MODE:0:1}" == "-" ]]; then
  RUNNER_ARGS+=("$MODE" "$@")
  MODE="run"
elif [[ "$MODE" == "run" ]]; then
  RUNNER_ARGS=("$@")
fi

if [[ -z "$MODE" ]]; then
  echo "Bitte Modus wählen (Native):"
  echo "  1) none          -> ForgeRunner.Native ohne Argumente starten (letzte URL durch Runtime)"
  echo "  2) default         -> Lokales Default (file://)"
  echo "  3) designer        -> Lokales ForgeDesigner (file://)"
  echo "  4) poser           -> Lokales ForgePoser (file://)"
  echo "  5) remote-default  -> Hosted Default (AppServer)"
  echo "  6) remote-designer -> Hosted ForgeDesigner (AppServer)"
  echo "  7) docs            -> SML/SMS Docs generieren (headless Godot)"
  echo "  8) build           -> Native Stack bauen (SMLCore.Native, SMSCore.Native, ForgeCli.Native, ForgeRunner.Native)"
  echo "  9) build-host      -> nur ForgeRunner.Native bauen"
  echo " 10) test            -> Native Tests (SMLCore.Native + SMSCore.Native)"
  echo " 11) clean           -> Native Build-Artefakte entfernen"
  echo " 12) upd             -> Lokaler Update-Override (run.local.sh)"
  echo " 13) help            -> Hilfe anzeigen"
  read -r -p "Auswahl [1-13] (Default 1): " CHOICE || true
  CHOICE="$(printf '%s' "${CHOICE:-}" | tr -d '[:space:]')"
  if [[ -z "$CHOICE" ]]; then
    CHOICE="1"
  fi

  case "$CHOICE" in
    1|none) MODE="none" ;;
    2|default) MODE="default" ;;
    3|designer) MODE="designer" ;;
    4|poser) MODE="poser" ;;
    5|remote-default) MODE="remote-default" ;;
    6|remote-designer) MODE="remote-designer" ;;
    7|docs) MODE="docs" ;;
    8|build|build-native) MODE="build" ;;
    9|build-host|build-native-host) MODE="build-host" ;;
   10|test|test-native) MODE="test" ;;
   11|clean) MODE="clean" ;;
   12|upd) MODE="upd" ;;
   13|help|-h|--help) MODE="help" ;;
    *)
      echo "Ungültige Auswahl. Abbruch."
      exit 1
      ;;
esac
fi

if try_local_mode_override "$@"; then
  exit 0
fi

case "$MODE" in
  run)
    native_runner_bin="$(resolve_native_runner_bin)"
    if [[ ! -x "$native_runner_bin" ]]; then
      echo "ERROR: ForgeRunner.Native executable not found: $native_runner_bin" >&2
      echo "Build it via './run.sh build-host' or set FORGE_RUNNER_NATIVE_BIN explicitly." >&2
      exit 1
    fi
    echo "Starting ForgeRunner.Native with arguments: ${RUNNER_ARGS[*]}"
    exec "$native_runner_bin" "${RUNNER_ARGS[@]}"
    ;;
  none)
    if [[ $# -gt 0 ]]; then
      echo "WARNING: mode 'none' ignores runner arguments: $*" >&2
    fi
    native_runner_bin="$(resolve_native_runner_bin)"
    if [[ ! -x "$native_runner_bin" ]]; then
      echo "ERROR: ForgeRunner.Native executable not found: $native_runner_bin" >&2
      echo "Build it via './run.sh build-host' or set FORGE_RUNNER_NATIVE_BIN explicitly." >&2
      exit 1
    fi
    echo "Starting ForgeRunner.Native (no arguments)..."
    exec "$native_runner_bin"
    ;;
  default)
    default_url="file://$REPO_ROOT/docs/Default/app.sml"
    run_native_window_host "$default_url"
    ;;
  designer)
    designer_url="file://$REPO_ROOT/docs/ForgeDesigner/app.sml"
    run_native_window_host "$designer_url"
    ;;
  remote-default)
    app_server_base_url="${FORGE_APP_SERVER_BASE_URL:-https://crowdware.github.io/Forge}"
    default_manifest_url="${app_server_base_url%/}/Default/manifest.sml"
    run_native_window_host "$default_manifest_url"
    ;;
  remote-designer)
    app_server_base_url="${FORGE_APP_SERVER_BASE_URL:-https://crowdware.github.io/Forge}"
    designer_manifest_url="${app_server_base_url%/}/ForgeDesigner/manifest.sml"
    run_native_window_host "$designer_manifest_url"
    ;;
  poser)
    poser_url="file://$REPO_ROOT/ForgePoser/app.sml"
    run_native_window_host "$poser_url"
    ;;
  url)
    if [[ $# -lt 1 ]]; then
      echo "ERROR: 'url' mode requires a URL argument, e.g. ./run.sh url http://localhost:8765/manifest.sml" >&2
      exit 1
    fi
    run_native_window_host "$1"
    ;;
  docs)
    docs_godot_bin=""
    if ! docs_godot_bin="$(resolve_godot_bin)"; then
      echo "ERROR: Godot binary not found." >&2
      echo "Set GODOT_BIN or install a 'godot4'/'godot' executable in PATH." >&2
      exit 1
    fi
    echo "Generating SML element docs..."
    "$docs_godot_bin" --headless --path "$REPO_ROOT/ForgeRunner.Native/host" --script "$REPO_ROOT/tools/generate_sml_element_docs.gd" -- --out "$REPO_ROOT/docs"
    echo "Generating SMS runtime function docs..."
    "$docs_godot_bin" --headless --path "$REPO_ROOT/ForgeRunner.Native/host" --script "$REPO_ROOT/tools/generate_sms_functions_docs.gd"
    echo "Generating SML resource system docs..."
    "$docs_godot_bin" --headless --path "$REPO_ROOT/ForgeRunner.Native/host" --script "$REPO_ROOT/tools/generate_sml_resources_docs.gd"
    echo "Documentation generation completed."
    ;;
  build|build-native)
    build_native_lib "SMLCore.Native" "$REPO_ROOT/SMLCore.Native" "$REPO_ROOT/SMLCore.Native/build" true
    build_native_lib "SMSCore.Native" "$REPO_ROOT/SMSCore.Native" "$REPO_ROOT/SMSCore.Native/build" true
    build_native_lib "ForgeCli.Native" "$REPO_ROOT/ForgeCli.Native" "$REPO_ROOT/ForgeCli.Native/build" false
    build_forge_runner_native_host
    ;;
  build-host|build-native-host)
    build_forge_runner_native_host
    ;;
  test|test-native)
    if [[ ! -f "$REPO_ROOT/SMLCore.Native/build/CTestTestfile.cmake" ]]; then
      echo "ERROR: SMLCore.Native tests are not configured. Run './run.sh build' first." >&2
      exit 1
    fi
    if [[ ! -f "$REPO_ROOT/SMSCore.Native/build/CTestTestfile.cmake" ]]; then
      echo "ERROR: SMSCore.Native tests are not configured. Run './run.sh build' first." >&2
      exit 1
    fi

    echo "Running SMLCore.Native spec tests..."
    ctest --test-dir "$REPO_ROOT/SMLCore.Native/build" --output-on-failure

    echo "Running SMSCore.Native spec tests..."
    ctest --test-dir "$REPO_ROOT/SMSCore.Native/build" --output-on-failure
    ;;
  clean)
    rm -rf "$REPO_ROOT/SMLCore.Native/build" \
           "$REPO_ROOT/SMSCore.Native/build" \
           "$REPO_ROOT/ForgeCli.Native/build" \
           "$REPO_ROOT/ForgeRunner.Native/build" \
           "$REPO_ROOT/ForgeRunner.Native/dist"
    echo "Native build artifacts cleaned."
    ;;
  upd)
    echo "ERROR: mode 'upd' is not handled by default run.sh. Add it to run.local.sh via forge_local_handle_mode()." >&2
    exit 1
    ;;
  help|-h|--help)
    usage
    ;;
  *)
    echo "ERROR: Unknown mode '$MODE'." >&2
    usage
    exit 1
    ;;
esac
