#!/bin/bash
# PostToolUse hook: Unity 관련 소스 편집 후 빠른 compile check를 자동 실행한다.

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=scripts/lib/unityctl_common.sh
source "$SCRIPT_DIR/../../scripts/lib/unityctl_common.sh"

INPUT=$(cat)
FILE_PATH=$(echo "$INPUT" | python3 -c "import sys,json; print(json.load(sys.stdin).get('tool_input',{}).get('file_path',''))" 2>/dev/null || true)

if [[ -z "${FILE_PATH:-}" ]]; then
  exit 0
fi

case "$FILE_PATH" in
  *.cs|*.uxml|*.uss|*.json)
    ;;
  *)
    exit 0
    ;;
esac

PROJECT_DIR="${CLAUDE_PROJECT_DIR:-/Users/family/jason/FR5UNITY/robotapp}"
UNITYCTL_RETRY_SECONDS="${UNITYCTL_RETRY_SECONDS:-10}"
COMPILE_TIMEOUT_SECONDS="${COMPILE_TIMEOUT_SECONDS:-30}"

UNITYCTL_BIN="$(unityctl_resolve_bin "${UNITYCTL_BIN:-}")" || true
if [[ -z "${UNITYCTL_BIN:-}" ]]; then
  echo "unity compile hook skipped: unityctl not found" >&2
  exit 0
fi

echo "## Post-edit Unity compile check..." >&2
TMP_JSON="$(mktemp "${TMPDIR:-/tmp}/post-edit-unity-compile.XXXXXX.json")"
trap 'rm -f "$TMP_JSON"' EXIT

compile_output=""
if unityctl_run_retry compile_output "$COMPILE_TIMEOUT_SECONDS" "$UNITYCTL_RETRY_SECONDS" "$UNITYCTL_BIN" check --project "$PROJECT_DIR" --type compile --json; then
  printf '%s\n' "$compile_output" >"$TMP_JSON"
  unityctl_print_compile_summary "$TMP_JSON" >&2
else
  echo "unity compile hook: compile check failed" >&2
  printf '%s\n' "$compile_output" >&2
fi

exit 0
