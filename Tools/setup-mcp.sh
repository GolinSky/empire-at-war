#!/bin/bash
# macOS bootstrap; run with: bash Tools/setup-mcp.sh [--check]
set -euo pipefail

CHECK_ONLY=false
RUNTIMES_ONLY=false
for ARG in "$@"; do
  case "$ARG" in
    --check) CHECK_ONLY=true ;;
    --runtimes-only) RUNTIMES_ONLY=true ;;
    *) echo "Usage: bash Tools/setup-mcp.sh [--check] [--runtimes-only]" >&2; exit 1 ;;
  esac
done

Fail() { echo "MCP setup failed: $*" >&2; exit 1; }
[ "$(uname -s)" = Darwin ] || Fail "Use Tools/setup-mcp.ps1 on Windows; this launcher supports macOS."
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
NODE_HOME="$HOME/.local/share/empire-at-war/node"
UNITY_HOME="${UNITY_CLI_HOME:-$HOME/.unity}"

# Discover existing per-user installations without displacing working PATH tools.
export PATH="$PATH:$NODE_HOME/bin:$HOME/.local/bin:$UNITY_HOME/bin:/opt/homebrew/bin:/usr/local/bin"
hash -r

NodeReady() {
  command -v node >/dev/null 2>&1 &&
    node -e 'process.exit(Number(process.versions.node.split(".")[0]) >= 20 ? 0 : 1)' &&
    command -v npm >/dev/null 2>&1 && npm --version >/dev/null 2>&1
}

UvReady() {
  command -v uv >/dev/null 2>&1 && uv --version >/dev/null 2>&1 &&
    command -v uvx >/dev/null 2>&1 && uvx --version >/dev/null 2>&1
}

UnityReady() {
  command -v unity >/dev/null 2>&1 && unity --version >/dev/null 2>&1
}

# An older system Node may precede a working installation from an earlier run.
if ! NodeReady && [ -x "$NODE_HOME/bin/node" ]; then
  export PATH="$NODE_HOME/bin:$PATH"
  hash -r
fi

if "$CHECK_ONLY"; then
  MISSING=false
  NodeReady || { echo "Missing or unusable: Node.js >=20 and npm" >&2; MISSING=true; }
  UvReady || { echo "Missing or unusable: uv and uvx" >&2; MISSING=true; }
  UnityReady || { echo "Missing or unusable: official Unity CLI" >&2; MISSING=true; }
  if "$MISSING"; then
    Fail "Run bash Tools/setup-mcp.sh to install prerequisites. --check changes nothing."
  fi
  if ! "$RUNTIMES_ONLY"; then
    node "$ROOT/Tools/setup-mcp.mjs" --check
  fi
  exit 0
fi

TEMP_DIR=""
trap 'if [ -n "$TEMP_DIR" ]; then rm -rf -- "$TEMP_DIR"; fi' EXIT
PrepareDownload() {
  command -v curl >/dev/null 2>&1 || Fail "curl is required to download runtimes."
  if [ -z "$TEMP_DIR" ]; then TEMP_DIR="$(mktemp -d "${TMPDIR:-/tmp}/empire-mcp.XXXXXX")"; fi
}

if ! NodeReady; then
  PrepareDownload
  case "$(uname -m)" in
    arm64) NODE_ARCH=arm64 ;;
    x86_64) NODE_ARCH=x64 ;;
    *) Fail "Unsupported macOS architecture: $(uname -m)" ;;
  esac
  echo "Installing Node.js 24 LTS and npm for this user..."
  curl -fsSL https://nodejs.org/dist/latest-v24.x/SHASUMS256.txt -o "$TEMP_DIR/SHASUMS256.txt"
  NODE_ARCHIVE="$(awk -v arch="$NODE_ARCH" '$2 ~ ("^node-v24\\.[0-9]+\\.[0-9]+-darwin-" arch "\\.tar\\.gz$") { print $2 }' "$TEMP_DIR/SHASUMS256.txt")"
  [ -n "$NODE_ARCHIVE" ] && [ "$(printf '%s\n' "$NODE_ARCHIVE" | wc -l | tr -d ' ')" = 1 ] || Fail "Node release manifest has no unique macOS archive."
  NODE_VERSION="${NODE_ARCHIVE#node-}"
  NODE_VERSION="${NODE_VERSION%-darwin-*}"
  curl -fsSL "https://nodejs.org/dist/$NODE_VERSION/$NODE_ARCHIVE" -o "$TEMP_DIR/$NODE_ARCHIVE"
  EXPECTED_HASH="$(awk -v file="$NODE_ARCHIVE" '$2 == file { print $1 }' "$TEMP_DIR/SHASUMS256.txt")"
  ACTUAL_HASH="$(shasum -a 256 "$TEMP_DIR/$NODE_ARCHIVE" | awk '{ print $1 }')"
  [ "$EXPECTED_HASH" = "$ACTUAL_HASH" ] || Fail "Node archive SHA-256 verification failed."
  mkdir -p "$NODE_HOME"
  tar -xzf "$TEMP_DIR/$NODE_ARCHIVE" -C "$NODE_HOME" --strip-components=1
  export PATH="$NODE_HOME/bin:$PATH"
  hash -r
  NodeReady || Fail "Installed Node/npm could not run."
fi

# Persist only our custom Node installation. Leave existing Node managers alone.
if [ "$(command -v node)" = "$NODE_HOME/bin/node" ]; then
  NODE_PATH_LINE='export PATH="$HOME/.local/share/empire-at-war/node/bin:$PATH"'
  for PROFILE in "$HOME/.zprofile" "$HOME/.bash_profile"; do
    if ! grep -Fqx "$NODE_PATH_LINE" "$PROFILE" 2>/dev/null; then
      # Creating .bash_profile must not suppress the existing login profile.
      if [ "$PROFILE" = "$HOME/.bash_profile" ] && [ ! -e "$PROFILE" ]; then
        if [ -f "$HOME/.bash_login" ]; then
          printf '%s\n' '[ ! -f "$HOME/.bash_login" ] || . "$HOME/.bash_login"' >> "$PROFILE"
        elif [ -f "$HOME/.profile" ]; then
          printf '%s\n' '[ ! -f "$HOME/.profile" ] || . "$HOME/.profile"' >> "$PROFILE"
        fi
      fi
      printf '\n# Empire at War MCP Node runtime\n%s\n' "$NODE_PATH_LINE" >> "$PROFILE"
    fi
  done
fi

if ! UvReady; then
  PrepareDownload
  echo "Installing uv and uvx for this user..."
  curl -fsSL https://astral.sh/uv/install.sh -o "$TEMP_DIR/install-uv.sh"
  UV_INSTALL_DIR="$HOME/.local/bin" sh "$TEMP_DIR/install-uv.sh"
  export PATH="$HOME/.local/bin:$PATH"
  hash -r
  UvReady || Fail "Installed uv/uvx could not run."
fi

if ! UnityReady; then
  PrepareDownload
  echo "Installing the official Unity CLI for this user..."
  curl -fsSL https://public-cdn.cloud.unity3d.com/hub/prod/cli/install.sh -o "$TEMP_DIR/install-unity.sh"
  UNITY_CLI_HOME="$UNITY_HOME" bash "$TEMP_DIR/install-unity.sh"
  export PATH="$UNITY_HOME/bin:$PATH"
  hash -r
  UnityReady || Fail "Installed Unity CLI could not run."
fi

echo "MCP runtimes are ready. Restart your terminal and Codex after a new runtime installation."
if ! "$RUNTIMES_ONLY"; then
  EAW_MCP_BOOTSTRAPPED=1 node "$ROOT/Tools/setup-mcp.mjs"
fi
