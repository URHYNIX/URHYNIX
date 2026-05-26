#!/usr/bin/env bash
set -euo pipefail

PROJECT_PATH="${PROJECT_PATH:-/Users/family/jason/FR5UNITY/robotapp}"
UNITYCTL_BIN="${UNITYCTL_BIN:-unityctl}"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
V3_DEBUG_SCRIPT="$SCRIPT_DIR/../unityctl-v3-debug.sh"
# shellcheck source=scripts/lib/fr5_evidence_common.sh
source "$SCRIPT_DIR/../lib/fr5_evidence_common.sh"

STATE_FILE="$PROJECT_PATH/Artifacts/live/fr5/latest-state.json"
DRIFT_FILE="$PROJECT_PATH/Artifacts/live/fr5/latest-drift.json"

log() {
  printf '%s\n' "$*"
}

pass() {
  printf '[PASS] %s\n' "$1"
}

fail() {
  printf '[FAIL] %s\n' "$1"
  if [[ -n "${2:-}" ]]; then
    printf '%s\n' "$2"
  fi
}

skip() {
  printf '[SKIP] %s\n' "$1"
  if [[ -n "${2:-}" ]]; then
    printf '%s\n' "$2"
  fi
}

run_action() {
  "$V3_DEBUG_SCRIPT" --plain --project "$PROJECT_PATH" --unityctl "$UNITYCTL_BIN" "$1"
}

echo "== Restart / Live Sync =="
"/Users/family/.codex/skills/unity-fr5-restart-live/scripts/restart_v3_live_loop.sh" --connect --sync

echo "== Post-Restart Compile =="
"$UNITYCTL_BIN" check --project "$PROJECT_PATH" --type compile --json

echo "== Normal 33ms Probe =="
run_action set-poll-33ms
run_action reset-readback-probe
sleep 2
run_action readback-probe-summary

echo "== Forced Error Fallback =="
run_action set-poll-33ms
run_action force-read-failures-2
sleep 1
run_action readback-probe-summary
sleep 1
run_action readback-probe-summary

echo "== Gate / Evidence =="
run_action gate-summary
run_action refresh-evidence
fr5_print_json_summary "latest-state.json" "$STATE_FILE" "$(fr5_state_summary_patterns)"
fr5_print_json_summary "latest-drift.json" "$DRIFT_FILE" "$(fr5_drift_summary_patterns)"
fr5_check_context_gate "$STATE_FILE"
fr5_assert_connected "$STATE_FILE"
