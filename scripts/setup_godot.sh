#!/usr/bin/env bash
# setup_godot.sh — Download and verify the exact Godot version required by Forge.
#
# Usage:  ./scripts/setup_godot.sh
#
# Downloads Godot into $REPO_ROOT/.godot-bin/ (git-ignored).
# Subsequent calls to run.sh pick up the binary automatically.
#
# Override the install directory:
#   GODOT_BIN_DIR=/your/path ./scripts/setup_godot.sh

set -euo pipefail

REPO_ROOT="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
INSTALL_DIR="${GODOT_BIN_DIR:-"$REPO_ROOT/.godot-bin"}"

# ---------------------------------------------------------------------------
# Required version — update these together with third_party/godot-cpp
# ---------------------------------------------------------------------------
GODOT_VERSION="4.6"
GODOT_TAG="4.6-stable"
GODOT_RELEASE_BASE="https://github.com/godotengine/godot/releases/download/${GODOT_TAG}"

# SHA-512 hashes (from $GODOT_RELEASE_BASE/SHA512-SUMS.txt)
SHA512_MACOS="85cb900331d6e2c99543b8130ad698c91508e87ec54fedef0a62c1a71b2eceb8d9ededb760109468734da0773c0a8261fda25cfe3345775b31412a944b371dad"
SHA512_LINUX_X64="a132863e12fe4230eca9259dffefccaf98eeda79d05e082ebb8dd73d44a1a511181ff8f04a15bf4989fb5c91bd480fc21159b0753938c562ed85c2941c7a5777"
SHA512_LINUX_ARM64="820d1535b5be2be3e8b4eead9747a360b521079276ae2b9f1105e7222b17b97824cb4acbea182e520ff1ab26c84cf0ad9cf739d9e22c546776aba936763ab7ff"

# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------
info()  { echo "[setup-godot] $*"; }
error() { echo "[setup-godot] ERROR: $*" >&2; exit 1; }

check_sha512() {
  local file="$1" expected="$2"
  local actual
  if command -v shasum >/dev/null 2>&1; then
    actual="$(shasum -a 512 "$file" | awk '{print $1}')"
  elif command -v sha512sum >/dev/null 2>&1; then
    actual="$(sha512sum "$file" | awk '{print $1}')"
  else
    error "Neither shasum nor sha512sum found — cannot verify download."
  fi
  if [[ "$actual" != "$expected" ]]; then
    error "SHA-512 mismatch for $(basename "$file")!\n  expected: $expected\n  got:      $actual"
  fi
  info "Checksum OK."
}

godot_version_matches() {
  local bin="$1"
  local ver
  ver="$("$bin" --version 2>/dev/null | tr -d '\r\n')" || return 1
  [[ "$ver" == "${GODOT_VERSION}.stable.mono"* ]]
}

# ---------------------------------------------------------------------------
# Detect platform
# ---------------------------------------------------------------------------
OS="$(uname -s)"
ARCH="$(uname -m)"

case "$OS" in
  Darwin)
    ASSET="Godot_v${GODOT_TAG}_mono_macos.universal.zip"
    SHA512="$SHA512_MACOS"
    ;;
  Linux)
    case "$ARCH" in
      arm64|aarch64) ASSET="Godot_v${GODOT_TAG}_mono_linux_arm64.zip"; SHA512="$SHA512_LINUX_ARM64" ;;
      *)             ASSET="Godot_v${GODOT_TAG}_mono_linux_x86_64.zip"; SHA512="$SHA512_LINUX_X64" ;;
    esac
    ;;
  *)
    error "Unsupported OS: $OS. Please install Godot ${GODOT_VERSION} manually."
    ;;
esac

# ---------------------------------------------------------------------------
# Check if already installed
# ---------------------------------------------------------------------------
case "$OS" in
  Darwin) GODOT_BIN="$INSTALL_DIR/Godot_mono.app/Contents/MacOS/Godot" ;;
  Linux)  GODOT_BIN="$INSTALL_DIR/Godot_v${GODOT_TAG}_mono_linux_$([ "$ARCH" = arm64 ] || [ "$ARCH" = aarch64 ] && echo arm64 || echo x86_64)/Godot_v${GODOT_TAG}_mono_linux_$([ "$ARCH" = arm64 ] || [ "$ARCH" = aarch64 ] && echo arm64 || echo x86_64)" ;;
esac

if [[ -x "$GODOT_BIN" ]] && godot_version_matches "$GODOT_BIN"; then
  info "Godot ${GODOT_VERSION} already installed at $GODOT_BIN"
  info "Set GODOT_BIN=$GODOT_BIN  (or let run.sh pick it up automatically)"
  exit 0
fi

# ---------------------------------------------------------------------------
# Download
# ---------------------------------------------------------------------------
mkdir -p "$INSTALL_DIR"
DOWNLOAD_URL="${GODOT_RELEASE_BASE}/${ASSET}"
ARCHIVE="$INSTALL_DIR/$ASSET"

if [[ ! -f "$ARCHIVE" ]]; then
  info "Downloading $ASSET ..."
  if command -v curl >/dev/null 2>&1; then
    curl -L --progress-bar --fail -o "$ARCHIVE" "$DOWNLOAD_URL"
  elif command -v wget >/dev/null 2>&1; then
    wget -q --show-progress -O "$ARCHIVE" "$DOWNLOAD_URL"
  else
    error "Neither curl nor wget found — cannot download Godot."
  fi
else
  info "Archive already downloaded: $ARCHIVE"
fi

# ---------------------------------------------------------------------------
# Verify
# ---------------------------------------------------------------------------
info "Verifying checksum ..."
check_sha512 "$ARCHIVE" "$SHA512"

# ---------------------------------------------------------------------------
# Extract
# ---------------------------------------------------------------------------
info "Extracting ..."
unzip -q -o "$ARCHIVE" -d "$INSTALL_DIR"
rm -f "$ARCHIVE"

case "$OS" in
  Darwin)
    # The zip contains Godot_mono.app
    chmod +x "$GODOT_BIN"
    # macOS quarantine — remove the xattr so it runs without a Gatekeeper dialog
    xattr -rd com.apple.quarantine "$INSTALL_DIR/Godot_mono.app" 2>/dev/null || true
    ;;
  Linux)
    chmod +x "$GODOT_BIN"
    ;;
esac

# ---------------------------------------------------------------------------
# Verify installed binary
# ---------------------------------------------------------------------------
if ! godot_version_matches "$GODOT_BIN"; then
  error "Installed binary reports unexpected version: $("$GODOT_BIN" --version 2>/dev/null)"
fi

info "Godot ${GODOT_VERSION} installed successfully."
info ""
info "  Binary : $GODOT_BIN"
info ""
info "run.sh will find it automatically, or set:"
info "  export GODOT_BIN=\"$GODOT_BIN\""
