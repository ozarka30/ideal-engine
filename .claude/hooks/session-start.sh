#!/bin/bash
# Remote sessions (Claude Code on the web) start from a bare container: fetch Godot 4.7.2 mono into the
# cache, warm the Godot MCP server's package, install jsonschema for the generators, and export GODOT_PATH.
set -euo pipefail
if [ "${CLAUDE_CODE_REMOTE:-}" != "true" ]; then
  exit 0
fi
cd "$CLAUDE_PROJECT_DIR"
pip install -q jsonschema >/dev/null 2>&1 || true
GODOT_BIN="$(tools/dev/godot.sh)"
echo "export GODOT_PATH=\"$GODOT_BIN\"" >> "$CLAUDE_ENV_FILE"
CACHE="${GODOT_CACHE:-$HOME/.cache/companywars/godot}/mcp"
if [ ! -f "$CACHE/node_modules/@coding-solo/godot-mcp/build/index.js" ]; then
  mkdir -p "$CACHE"
  (cd "$CACHE" && npm install --silent --no-audit --no-fund "@coding-solo/godot-mcp@0.1.1")
fi
echo "session-start: Godot at $GODOT_BIN"
