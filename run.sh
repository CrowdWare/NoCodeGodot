#!/usr/bin/env bash
set -euo pipefail

REPO_ROOT="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
LOCAL_RUN_INCLUDE="$REPO_ROOT/run.local.sh"

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
POSER_UI="file://$REPO_ROOT/ForgePoser/app.sml"

OS_NAME="$(uname -s)"

require_godot() {
  if [[ -z "$GODOT_BIN" ]]; then
    echo "ERROR: Godot binary not found. Set GODOT_BIN=/path/to/godot" >&2
    exit 1
  fi
}

build_native_lib() {
  local name="$1"
  local src_dir="$2"
  local build_dir="$3"

  if ! command -v cmake >/dev/null 2>&1; then
    echo "ERROR: cmake not found. Install cmake to build ${name}." >&2
    return 1
  fi

  if [[ ! -d "$src_dir" ]]; then
    echo "ERROR: Missing native source directory for ${name}: $src_dir" >&2
    return 1
  fi

  mkdir -p "$build_dir"
  echo "Configuring ${name}..."
  cmake -S "$src_dir" -B "$build_dir" -DCMAKE_BUILD_TYPE=Release
  echo "Building ${name}..."
  cmake --build "$build_dir" --config Release
}

setup_tools() {
  local auto_install="${1:-false}"
  local missing=()

  local have_godot="no"
  local have_dotnet="no"
  local have_mono="no"

  if [[ -n "$GODOT_BIN" ]]; then have_godot="yes"; else missing+=("godot"); fi
  if command -v dotnet >/dev/null 2>&1; then have_dotnet="yes"; else missing+=("dotnet"); fi
  if command -v mono >/dev/null 2>&1; then have_mono="yes"; else missing+=("mono"); fi

  echo "Forge setup check (${OS_NAME})"
  echo "  Godot:  ${have_godot}"
  echo "  dotnet: ${have_dotnet}"
  echo "  mono:   ${have_mono}"

  if [[ ${#missing[@]} -eq 0 ]]; then
    echo "All required tools are available."
    return 0
  fi

  echo
  echo "Missing tools: ${missing[*]}"

  if [[ "$auto_install" != "true" ]]; then
    echo "Tip: run './run.sh setup --install=true' for automated install (where supported)."
  fi

  case "$OS_NAME" in
    Darwin)
      echo
      echo "Recommended (macOS + Homebrew):"
      echo "  brew install --cask godot-mono"
      echo "  brew install dotnet mono"

      if [[ "$auto_install" == "true" ]]; then
        if ! command -v brew >/dev/null 2>&1; then
          echo "ERROR: Homebrew not found. Install Homebrew first: https://brew.sh/" >&2
          return 1
        fi

        echo "Running auto-install via Homebrew..."
        set +e
        brew install --cask godot-mono
        if [[ $? -ne 0 ]]; then
          echo "WARN: 'godot-mono' install failed. Trying 'godot' cask..."
          brew install --cask godot
        fi
        brew install dotnet mono
        local brew_status=$?
        set -e
        if [[ $brew_status -ne 0 ]]; then
          echo "ERROR: One or more Homebrew install commands failed." >&2
          return 1
        fi
      fi
      ;;
    Linux)
      echo
      echo "Recommended (Linux):"
      echo "  Install Godot Mono, .NET SDK and Mono runtime via your distro package manager."
      echo "  Examples:"
      echo "    Ubuntu/Debian: sudo apt update && sudo apt install -y mono-complete dotnet-sdk-8.0"
      echo "    Fedora:        sudo dnf install -y mono-complete dotnet-sdk-8.0"
      echo "    Arch:          sudo pacman -S --needed mono dotnet-sdk"

      if [[ "$auto_install" == "true" ]]; then
        echo "Auto-install on Linux is distro-specific. Please run the appropriate command manually."
        return 1
      fi
      ;;
    *)
      echo
      echo "Unsupported OS for automated setup. Please install: Godot Mono, dotnet, mono."
      if [[ "$auto_install" == "true" ]]; then
        return 1
      fi
      ;;
  esac

  echo
  echo "Setup check finished."
}

load_local_run_include() {
  if [[ -f "$LOCAL_RUN_INCLUDE" ]]; then
    # Local maintainer overrides are optional and intentionally not versioned.
    # shellcheck source=/dev/null
    source "$LOCAL_RUN_INCLUDE"
  fi
}

try_local_mode_override() {
  if ! declare -F forge_local_handle_mode >/dev/null 2>&1; then
    return 1
  fi

  case "$MODE" in
    release|docs|test|export|app|poser)
      forge_local_handle_mode "$MODE" "${POSITIONAL_ARGS[@]:1}"
      return $?
      ;;
    *)
      return 1
      ;;
  esac
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

GODOT_ARGS=()
FORGE_RUNNER_ARGS=(--sms-native=true)
SETUP_INSTALL="false"
POSITIONAL_ARGS=()
for arg in "$@"; do
  case "$arg" in
    --verbose)
      GODOT_ARGS+=("$arg")
      ;;
    --debug=*)
      FORGE_RUNNER_ARGS+=("$arg")
      ;;
    --sml-native=*)
      FORGE_RUNNER_ARGS+=("$arg")
      ;;
    --sms-native=*)
      if [[ "$arg" != "--sms-native=true" ]]; then
        echo "Ignoring $arg (native SMS is mandatory)."
      fi
      ;;
    --install=true|--install)
      SETUP_INSTALL="true"
      ;;
    *)
      POSITIONAL_ARGS+=("$arg")
      ;;
  esac
done

MODE="${POSITIONAL_ARGS[0]:-}"
load_local_run_include

if [[ -z "${SMS_NATIVE_LIB_DIR:-}" ]]; then
  if [[ -d "$REPO_ROOT/SMSCore.Native/build" ]]; then
    export SMS_NATIVE_LIB_DIR="$REPO_ROOT/SMSCore.Native/build"
  fi
fi

if [[ -z "${SML_NATIVE_LIB_DIR:-}" ]]; then
  if [[ -d "$REPO_ROOT/SMLCore.Native/build" ]]; then
    export SML_NATIVE_LIB_DIR="$REPO_ROOT/SMLCore.Native/build"
  fi
fi

if [[ -z "$MODE" ]]; then
  echo "Bitte Modus wählen:"
  echo "  1) default   -> docs/Default/app.sml"
  echo "  2) designer  -> docs/ForgeDesigner/app.sml"
  echo "  3) poser     -> ForgePoser/app.sml"
  echo "  4) docking   -> samples/docking_demo.sml"
  echo "  5) none      -> ohne URL-Override"
  echo "  6) setup     -> benötigte Tools prüfen/installieren"
  echo "  7) docs      -> SML/SMS Docs generieren (headless)"
  echo "  8) theme     -> theme.tres aus theme.sml generieren (headless)"
  echo "  9) build     -> App bauen"
  echo " 10) export    -> macOS Release bauen (Godot export)"
  echo " 11) test      -> UnitTests"
  echo " 12) manifest  -> manifest.sml für alle Docs generieren"
  echo " 13) pub       -> manifest + git commit + git push (msg required)"
  echo " 14) app       -> ForgeRunner.app starten (Release)"
  echo " 15) release   -> version setzen + export + tag + GitHub Release (default: pre)"
  read -r -p "Auswahl [1-15]: " CHOICE

  case "$CHOICE" in
    1) MODE="default" ;;
    2) MODE="designer" ;;
    3) MODE="poser" ;;
    4) MODE="docking" ;;
    5) MODE="none" ;;
    6) MODE="setup" ;;
    7) MODE="docs" ;;
    8) MODE="theme" ;;
    9) MODE="build" ;;
    10) MODE="export" ;;
    11) MODE="test" ;;
    12) MODE="manifest" ;;
    13) MODE="pub" ;;
    14) MODE="app" ;;
    15) MODE="release" ;;
    *)
      echo "Ungültige Auswahl. Abbruch."
      exit 1
      ;;
  esac
fi

if try_local_mode_override; then
  exit 0
fi

case "$MODE" in
  default)
    require_godot
    echo "Starting ForgeRunner with docs/Default/app.sml"
    exec "$GODOT_BIN" "${GODOT_ARGS[@]-}" --path "$RUNNER_PATH" -- --url "$DEFAULT_UI" "${FORGE_RUNNER_ARGS[@]-}"
    ;;
  designer|sample)
    require_godot
    echo "Starting ForgeRunner with docs/ForgeDesigner/app.sml"
    exec "$GODOT_BIN" "${GODOT_ARGS[@]-}" --path "$RUNNER_PATH" -- --url "$SAMPLE_UI" "${FORGE_RUNNER_ARGS[@]-}"
    ;;
  docking)
    require_godot
    echo "Starting ForgeRunner with samples/docking_demo.sml"
    exec "$GODOT_BIN" "${GODOT_ARGS[@]-}" --path "$RUNNER_PATH" -- --url "$DOCKING_SAMPLE_UI" "${FORGE_RUNNER_ARGS[@]-}"
    ;;
  poser)
    require_godot
    echo "Starting ForgePoser..."
    exec "$GODOT_BIN" "${GODOT_ARGS[@]-}" --path "$RUNNER_PATH" -- --url "$POSER_UI" "${FORGE_RUNNER_ARGS[@]-}"
    ;;
  none)
    require_godot
    echo "Starting ForgeRunner ohne startup URL override"
    exec "$GODOT_BIN" "${GODOT_ARGS[@]-}" --path "$RUNNER_PATH" -- --reset-start-url "${FORGE_RUNNER_ARGS[@]-}"
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
    build_native_lib "SMLCore.Native" "$REPO_ROOT/SMLCore.Native" "$REPO_ROOT/SMLCore.Native/build"
    build_native_lib "SMSCore.Native" "$REPO_ROOT/SMSCore.Native" "$REPO_ROOT/SMSCore.Native/build"
    dotnet build "$REPO_ROOT/ForgeRunner/ForgeRunner.csproj"
    echo "Building ForgeCli.Native..."
    cmake -S "$REPO_ROOT/ForgeCli.Native" -B "$REPO_ROOT/ForgeCli.Native/build" -DCMAKE_BUILD_TYPE=Release
    cmake --build "$REPO_ROOT/ForgeCli.Native/build" --config Release
    ;;
  test)
    echo "Running ForgeRunner unit tests..."
    dotnet test "$REPO_ROOT/ForgeRunner.Tests/ForgeRunner.Tests.csproj"

    echo "Running ForgeAiLib unit tests..."
    dotnet test "$REPO_ROOT/ForgeAiLib.Tests/ForgeAiLib.Tests.csproj"
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
  pub)
    if [[ -z "${POSITIONAL_ARGS[1]:-}" ]]; then
      echo "ERROR: Missing commit message." >&2
      echo "Usage: ./run.sh pub \"something has changed\"" >&2
      exit 1
    fi

    bash "$REPO_ROOT/scripts/build_manifest.sh"

    cd "$REPO_ROOT"
    COMMIT_MSG="${POSITIONAL_ARGS[1]}"
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
    exec "$APP" #--url "$DEFAULT_UI"
    ;;
  release)
    require_godot
    if ! command -v gh >/dev/null 2>&1; then
      echo "ERROR: gh CLI not found. Install via: brew install gh" >&2
      exit 1
    fi
    if ! command -v hdiutil >/dev/null 2>&1; then
      echo "ERROR: hdiutil not found. DMG packaging requires macOS." >&2
      exit 1
    fi

    CHANNEL="${POSITIONAL_ARGS[1]:-pre}"   # pre | alpha | beta | stable
    VERSION="$(generate_version)"
    TAG="v$VERSION"
    NOTES_FILE="$REPO_ROOT/RELEASE_NOTES_PRE.md"

    case "$CHANNEL" in
      alpha)
        PRERELEASE_FLAG="--prerelease"
        TITLE="Forge $TAG ($CHANNEL)"
        NOTES_FILE="$REPO_ROOT/RELEASE_NOTES_ALPHA.md"
        ;;
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

    DMG="$REPO_ROOT/ForgeRunner-${TAG}-macOS.dmg"
    TMP_DMG_DIR="$REPO_ROOT/.release_dmg"
    echo "Packaging ForgeRunner.app -> $(basename "$DMG")..."
    cd "$REPO_ROOT"
    rm -rf "$TMP_DMG_DIR"
    mkdir -p "$TMP_DMG_DIR"
    cp -R ForgeRunner.app "$TMP_DMG_DIR/"
    hdiutil create -volname "ForgeRunner" -srcfolder "$TMP_DMG_DIR" -ov -format UDZO "$DMG" >/dev/null

    echo "Committing version bump + tagging $TAG..."
    git add ForgeRunner/ForgeRunner.csproj
    git commit -m "release: $TAG [$CHANNEL]"
    git tag "$TAG"
    git push
    git push origin "$TAG"

    echo "Creating GitHub Release $TAG..."
    NOTES_TEXT=""
    if [[ -f "$NOTES_FILE" ]]; then
      NOTES_TEXT="$(cat "$NOTES_FILE")"
    else
      echo "WARN: Notes file not found: $NOTES_FILE (continuing with generated notes only)."
    fi
    gh release create "$TAG" "$DMG" \
      --title "$TITLE" \
      --notes "$NOTES_TEXT" \
      --generate-notes \
      $PRERELEASE_FLAG

    rm -rf "$TMP_DMG_DIR"
    rm "$DMG"
    echo "Release $TAG [$CHANNEL] published."
    ;;
  setup)
    setup_tools "$SETUP_INSTALL"
    ;;
  *)
    echo "Usage: $0 [default|designer|poser|docking|none|setup|docs|theme|build|export|test|manifest|pub|app|release] [--debug=true] [--sml-native=true] [--verbose] [--install=true]"
    exit 1
    ;;
esac
