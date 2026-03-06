#!/usr/bin/env bash
set -euo pipefail

REPO_ROOT="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
OS_NAME="$(uname -s)"

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

  if [[ -z "${GODOT_CPP_DIR:-}" ]]; then
    echo "ERROR: GODOT_CPP_DIR is required to build ForgeRunner.Native." >&2
    echo "       Example: export GODOT_CPP_DIR=/absolute/path/to/godot-cpp" >&2
    return 1
  fi

  build_native_lib "ForgeRunner.Native" "$src_dir" "$build_dir" false

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
  ./run.sh build              Build native stack (default)
  ./run.sh build-native       Same as build
  ./run.sh build-host         Build ForgeRunner.Native only
  ./run.sh test               Run native tests (SMLCore.Native + SMSCore.Native)
  ./run.sh clean              Remove native build/dist folders

Environment:
  FORGE_RUNNER_NATIVE_BIN     Optional explicit native runner executable path
  GODOT_CPP_DIR               Required for build-host/build
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
  echo "  2) build         -> Native Stack bauen (SMLCore.Native, SMSCore.Native, ForgeCli.Native, ForgeRunner.Native)"
  echo "  3) build-host    -> nur ForgeRunner.Native bauen"
  echo "  4) test          -> Native Tests (SMLCore.Native + SMSCore.Native)"
  echo "  5) clean         -> Native Build-Artefakte entfernen"
  echo "  6) help          -> Hilfe anzeigen"
  read -r -p "Auswahl [1-6] (Default 1): " CHOICE || true
  CHOICE="$(printf '%s' "${CHOICE:-}" | tr -d '[:space:]')"
  if [[ -z "$CHOICE" ]]; then
    CHOICE="1"
  fi

  case "$CHOICE" in
    1|none) MODE="none" ;;
    2|build|build-native) MODE="build" ;;
    3|build-host|build-native-host) MODE="build-host" ;;
    4|test|test-native) MODE="test" ;;
    5|clean) MODE="clean" ;;
    6|help|-h|--help) MODE="help" ;;
    *)
      echo "Ungültige Auswahl. Abbruch."
      exit 1
      ;;
esac
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
  help|-h|--help)
    usage
    ;;
  *)
    echo "ERROR: Unknown mode '$MODE'." >&2
    usage
    exit 1
    ;;
esac
