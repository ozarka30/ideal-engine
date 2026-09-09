#!/usr/bin/env bash
# Prints the path of a Godot 4.7.2 mono editor binary, downloading one into a cache if none is found.
#   tools/dev/godot.sh              -> prints the binary path
#   tools/dev/godot.sh --templates  -> also installs the matching export templates (~1 GB; only exports need them)
# Resolution order: $GODOT_PATH, a `godot` on PATH that reports 4.7.2 mono, then the cache
# (${GODOT_CACHE:-~/.cache/companywars/godot}). Downloads are Linux x86_64 only; elsewhere set GODOT_PATH.
set -euo pipefail
VERSION="4.7.2"
CACHE="${GODOT_CACHE:-$HOME/.cache/companywars/godot}"
BASE="https://github.com/godotengine/godot/releases/download/${VERSION}-stable"

want_templates=false
[ "${1:-}" = "--templates" ] && want_templates=true

find_binary() {
  if [ -n "${GODOT_PATH:-}" ] && [ -x "$GODOT_PATH" ]; then echo "$GODOT_PATH"; return; fi
  if command -v godot >/dev/null 2>&1 && godot --version 2>/dev/null | grep -q "^${VERSION}.stable.mono"; then command -v godot; return; fi
  local cached
  cached=$(ls "$CACHE"/Godot_v${VERSION}-stable_mono_linux_x86_64/Godot_v${VERSION}-stable_mono_linux.x86_64 2>/dev/null || true)
  [ -n "$cached" ] && { echo "$cached"; return; }
  echo ""
}

bin=$(find_binary)
if [ -z "$bin" ]; then
  if [ "$(uname -s)" != "Linux" ]; then
    echo "tools/dev/godot.sh: no Godot ${VERSION} mono found; install it and set GODOT_PATH" >&2
    exit 1
  fi
  mkdir -p "$CACHE"
  zip="$CACHE/editor.zip"
  echo "tools/dev/godot.sh: downloading Godot ${VERSION} mono to $CACHE" >&2
  curl -fsSL -o "$zip" "$BASE/Godot_v${VERSION}-stable_mono_linux_x86_64.zip"
  unzip -q -o "$zip" -d "$CACHE" && rm -f "$zip"
  bin=$(find_binary)
  chmod +x "$bin"
fi

if $want_templates; then
  tdir="$HOME/.local/share/godot/export_templates/${VERSION}.stable.mono"
  if [ ! -f "$tdir/linux_release.x86_64" ]; then
    echo "tools/dev/godot.sh: downloading export templates ${VERSION}" >&2
    tpz="$CACHE/templates.tpz"
    curl -fsSL -o "$tpz" "$BASE/Godot_v${VERSION}-stable_mono_export_templates.tpz"
    mkdir -p "$tdir" "$CACHE/tpl"
    unzip -q -o "$tpz" -d "$CACHE/tpl"
    mv "$CACHE/tpl/templates/"* "$tdir/"
    rm -rf "$CACHE/tpl" "$tpz"
  fi
fi
echo "$bin"
