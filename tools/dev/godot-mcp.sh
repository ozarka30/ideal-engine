#!/usr/bin/env bash
# Launches the Godot MCP server (Coding-Solo/godot-mcp) for Claude Code; .mcp.json points here.
# Resolves the Godot binary through tools/dev/godot.sh and installs the server into the cache on first use.
# Without a display (a remote session) it runs under xvfb-run so run_project can open a window.
set -euo pipefail
HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PKG="@coding-solo/godot-mcp@0.1.1"
CACHE="${GODOT_CACHE:-$HOME/.cache/companywars/godot}/mcp"
GODOT_PATH="$("$HERE/godot.sh")"
export GODOT_PATH
server="$CACHE/node_modules/@coding-solo/godot-mcp/build/index.js"
if [ ! -f "$server" ]; then
  mkdir -p "$CACHE"
  (cd "$CACHE" && npm install --silent --no-audit --no-fund "$PKG" >&2)
fi
if [ -z "${DISPLAY:-}" ] && command -v xvfb-run >/dev/null 2>&1; then
  # xvfb-run folds the child's stderr into stdout, which would corrupt the MCP stream: keep our stderr on fd 3.
  exec 3>&2
  exec xvfb-run -a -s "-screen 0 1280x720x24" sh -c 'exec node "$1" 2>&3' sh "$server"
fi
exec node "$server"
