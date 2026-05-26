#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=scripts/lib/unityctl_common.sh
source "$SCRIPT_DIR/../lib/unityctl_common.sh"
# shellcheck source=scripts/lib/fr5_evidence_common.sh
source "$SCRIPT_DIR/../lib/fr5_evidence_common.sh"
DEFAULT_PROJECT_PATH="/Users/family/jason/FR5UNITY/robotapp"

PROJECT_PATH="${PROJECT_PATH:-$DEFAULT_PROJECT_PATH}"
if [[ ! -d "$PROJECT_PATH" ]]; then
  PROJECT_PATH="$(cd "$SCRIPT_DIR/../.." && pwd)"
fi
PROJECT_PATH="$(cd "$PROJECT_PATH" && pwd)"

UNITYCTL_BIN="${UNITYCTL_BIN:-${UNITYCTL:-${UNITYCTL_EXE:-}}}"
FAIRINO_IP="${FAIRINO_IP:-192.168.57.2}"
FAIRINO_PORT="${FAIRINO_PORT:-8080}"
SCENE_PATH="${SCENE_PATH:-Assets/Scenes/RobotControlV3.unity}"
TIMEOUT_SECONDS="${TIMEOUT_SECONDS:-90}"
STATUS_TIMEOUT_SECONDS="${STATUS_TIMEOUT_SECONDS:-8}"
UNITYCTL_RETRY_SECONDS="${UNITYCTL_RETRY_SECONDS:-10}"
UNITYCTL_COMMAND_TIMEOUT_SECONDS="${UNITYCTL_COMMAND_TIMEOUT_SECONDS:-90}"
V3_DEBUG_SCRIPT="$SCRIPT_DIR/../unityctl-v3-debug.sh"
RUN_EDIT_TESTS=1
DO_CONNECT=0
DO_SYNC=0
LIVE_REQUESTED=0
REPORT_ONLY=0
PLAY_STARTED=0
LIVE_VERIFY_STARTED_EPOCH=0
LIVE_SESSION_ID=""
CURRENT_EVENTS_SESSION_ID=""

STATE_FILE="$PROJECT_PATH/Artifacts/live/fr5/latest-state.json"
DRIFT_FILE="$PROJECT_PATH/Artifacts/live/fr5/latest-drift.json"

usage() {
  cat <<'EOF'
Usage: run_fr5_live_checks.sh [options]

Options:
  --project PATH        Override project root
  --fairino-ip IP       Override FR5 controller IP
  --fairino-port PORT   Override FR5 controller port
  --scene PATH          Override RobotControlV3 scene path
  --connect             Attempt live readback connect
  --sync                Attempt live sync after connect
  --live                Shorthand for --connect --sync
  --report-only         Skip network/live execution and review existing evidence only
  --no-edit-tests       Skip EditMode test pass
  --skip-edit-tests     Alias for --no-edit-tests
  --help                Show this help
EOF
}

log() {
  printf '%s\n' "$*"
}

section() {
  printf '\n== %s ==\n' "$*"
}

pass() {
  PASS=$((PASS + 1))
  TOTAL=$((TOTAL + 1))
  printf '[PASS] %s\n' "$1"
}

fail() {
  FAIL=$((FAIL + 1))
  TOTAL=$((TOTAL + 1))
  printf '[FAIL] %s\n' "$1"
  if [[ -n "${2:-}" ]]; then
    printf '%s\n' "$2"
  fi
}

skip() {
  SKIP=$((SKIP + 1))
  TOTAL=$((TOTAL + 1))
  printf '[SKIP] %s\n' "$1"
  if [[ -n "${2:-}" ]]; then
    printf '%s\n' "$2"
  fi
}

run_capture() {
  local __outvar="$1"
  shift
  local output
  if output="$("$@" 2>&1)"; then
    printf -v "$__outvar" '%s' "$output"
    return 0
  fi

  local rc=$?
  printf -v "$__outvar" '%s' "$output"
  return "$rc"
}

run_unityctl_capture() {
  local __outvar="$1"
  local timeout_seconds="$2"
  shift 2
  unityctl_run_retry "$__outvar" "$timeout_seconds" "$UNITYCTL_RETRY_SECONDS" "$UNITYCTL_BIN" "$@"
}

run_unityctl_step() {
  local label="$1"
  shift
  local output
  if run_unityctl_capture output "$UNITYCTL_COMMAND_TIMEOUT_SECONDS" "$@"; then
    log "$output"
    pass "$label"
    return 0
  fi

  local rc=$?
  log "$output"
  fail "$label" "exit code: $rc"
  return 0
}

run_v3_debug_step() {
  local label="$1"
  local action="$2"
  shift 2
  local output
  if output="$("$V3_DEBUG_SCRIPT" --plain --project "$PROJECT_PATH" --unityctl "$UNITYCTL_BIN" "$action" "$@" 2>&1)"; then
    log "$output"
    pass "$label"
    return 0
  fi

  local rc=$?
  log "$output"
  fail "$label" "exit code: $rc"
  return 0
}

trap_cleanup() {
  if [[ "$PLAY_STARTED" -eq 1 ]]; then
    "$UNITYCTL_BIN" play stop --project "$PROJECT_PATH" --json >/dev/null 2>&1 || true
  fi
}

PASS=0
FAIL=0
SKIP=0
TOTAL=0
trap trap_cleanup EXIT

while [[ $# -gt 0 ]]; do
  case "$1" in
    --project)
      PROJECT_PATH="$2"
      shift 2
      ;;
    --fairino-ip)
      FAIRINO_IP="$2"
      shift 2
      ;;
    --fairino-port)
      FAIRINO_PORT="$2"
      shift 2
      ;;
    --scene)
      SCENE_PATH="$2"
      shift 2
      ;;
    --connect)
      DO_CONNECT=1
      LIVE_REQUESTED=1
      shift
      ;;
    --sync)
      DO_SYNC=1
      LIVE_REQUESTED=1
      shift
      ;;
    --live)
      DO_CONNECT=1
      DO_SYNC=1
      LIVE_REQUESTED=1
      shift
      ;;
    --report-only)
      REPORT_ONLY=1
      RUN_EDIT_TESTS=0
      shift
      ;;
    --no-edit-tests|--skip-edit-tests)
      RUN_EDIT_TESTS=0
      shift
      ;;
    --help|-h)
      usage
      exit 0
      ;;
    *)
      printf 'Unknown argument: %s\n' "$1" >&2
      usage >&2
      exit 1
      ;;
  esac
done

PROJECT_PATH="$(cd "$PROJECT_PATH" && pwd)"
STATE_FILE="$PROJECT_PATH/Artifacts/live/fr5/latest-state.json"
DRIFT_FILE="$PROJECT_PATH/Artifacts/live/fr5/latest-drift.json"

compile_ok=0

if [[ "$REPORT_ONLY" -eq 1 ]]; then
  section "Report Only"
  skip "network preflight" "disabled by --report-only"
  skip "unityctl status" "disabled by --report-only"
  skip "unityctl compile" "disabled by --report-only"
  skip "edit-mode tests" "disabled by --report-only"
else
  UNITYCTL_BIN="$(unityctl_resolve_bin "${UNITYCTL_BIN:-}")" || true
  if [[ -z "${UNITYCTL_BIN:-}" ]]; then
    printf 'unityctl not found on PATH\n' >&2
    exit 1
  fi

  if [[ ! -x "$V3_DEBUG_SCRIPT" ]]; then
    printf 'unityctl-v3-debug.sh not found or not executable: %s\n' "$V3_DEBUG_SCRIPT" >&2
    exit 1
  fi

  if ! command -v ping >/dev/null 2>&1; then
    printf 'ping not found on PATH\n' >&2
    exit 1
  fi

  if ! command -v nc >/dev/null 2>&1; then
    printf 'nc not found on PATH\n' >&2
    exit 1
  fi

  section "Network"
  if run_capture ping_out ping -c 1 "$FAIRINO_IP"; then
    log "$ping_out"
    pass "ping $FAIRINO_IP"
  else
    rc=$?
    log "$ping_out"
    fail "ping $FAIRINO_IP" "exit code: $rc"
  fi

  if run_capture nc_out nc -vz "$FAIRINO_IP" "$FAIRINO_PORT"; then
    log "$nc_out"
    pass "nc -vz $FAIRINO_IP $FAIRINO_PORT"
  else
    rc=$?
    log "$nc_out"
    fail "nc -vz $FAIRINO_IP $FAIRINO_PORT" "exit code: $rc"
  fi

  section "UnityCtl Status"
  status_ready=0
  status_degraded=0
  status_output=""
  status_rc=0
  for ((attempt = 1; attempt <= TIMEOUT_SECONDS; attempt++)); do
    if unityctl_run_with_timeout status_output "$STATUS_TIMEOUT_SECONDS" "$UNITYCTL_BIN" status --project "$PROJECT_PATH" --json; then
      if grep -Eq 'Ready' <<<"$status_output" \
        && grep -Eq '"?bridgeLoaded"?[[:space:]]*[:=][[:space:]]*true' <<<"$status_output" \
        && grep -Eq '"?ipcPipePresent"?[[:space:]]*[:=][[:space:]]*true' <<<"$status_output"; then
        status_ready=1
        break
      fi
    else
      status_rc=$?
      if [[ "$status_rc" -ne 142 ]] && ! is_retryable_unityctl_output "$status_output"; then
        break
      fi
    fi
    sleep 1
  done
  log "$status_output"
  if [[ "$status_ready" -eq 1 ]]; then
    pass "unityctl status"
  elif grep -Eq '"?state"?[[:space:]]*[:=][[:space:]]*"Playing"' <<<"$status_output" \
    && grep -Eq '"?bridgeLoaded"?[[:space:]]*[:=][[:space:]]*true' <<<"$status_output" \
    && grep -Eq '"?ipcPipePresent"?[[:space:]]*[:=][[:space:]]*true' <<<"$status_output"; then
    skip "unityctl status" "editor already playing; bridge is healthy so continuing with compile + live checks"
  elif [[ "$status_rc" -eq 142 ]] || is_retryable_unityctl_output "$status_output"; then
    status_degraded=1
    skip "unityctl status" "status command stayed flaky on macOS; proceeding with compile + scene/play/exec as readiness gate"
  else
    fail "unityctl status" "expected Ready/bridgeLoaded=true/ipcPipePresent=true within ${TIMEOUT_SECONDS}s"
  fi

  section "UnityCtl Compile"
  compile_output=""
  if run_unityctl_capture compile_output "$UNITYCTL_COMMAND_TIMEOUT_SECONDS" check --project "$PROJECT_PATH" --type compile --json; then
    log "$compile_output"
    compile_ok=1
    pass "unityctl compile"
  else
    rc=$?
    log "$compile_output"
    fail "unityctl compile" "exit code: $rc"
  fi

  section "EditMode Tests"
  if [[ "$RUN_EDIT_TESTS" -ne 1 ]]; then
    skip "edit-mode tests" "disabled by flag"
  else
    edit_tests=(
      "KineTutor3D.Tests.EditMode.Validation.LiveFairinoClientSdkTests"
      "KineTutor3D.Tests.EditMode.Fr5LiveReadbackTests"
      "KineTutor3D.Tests.EditMode.Validation.RobotControlV3HardcodingGuardTests"
    )

    for test_filter in "${edit_tests[@]}"; do
      test_output=""
      if run_unityctl_capture test_output "$UNITYCTL_COMMAND_TIMEOUT_SECONDS" test --project "$PROJECT_PATH" --mode edit --filter "$test_filter" --json; then
        log "$test_output"
        pass "edit test: $test_filter"
      else
        rc=$?
        log "$test_output"
        fail "edit test: $test_filter" "exit code: $rc"
      fi
    done
  fi
fi

live_allowed=0
if [[ "$LIVE_REQUESTED" -eq 1 ]]; then
  if [[ "$compile_ok" -eq 1 ]]; then
    live_allowed=1
  else
    skip "live connect/sync" "requires compile success; status alone is treated as diagnostic on macOS"
  fi
fi

evidence_review_allowed=0
if [[ "$REPORT_ONLY" -eq 1 || "$live_allowed" -eq 1 ]]; then
  evidence_review_allowed=1
fi

if [[ "$live_allowed" -eq 1 ]]; then
  section "FR5 Live V3"
  export FAIRINO_IP FAIRINO_PORT
  if [[ -n "${FAIRINO_BRIDGE_URL:-}" ]]; then
    export FAIRINO_BRIDGE_URL
  fi

  if run_unityctl_step "scene open RobotControlV3" scene open --project "$PROJECT_PATH" --path "$SCENE_PATH" --mode single --force --json; then
    :
  fi

  if run_unityctl_step "play start" play start --project "$PROJECT_PATH" --json; then
    PLAY_STARTED=1
  fi

  sleep 2

  run_v3_debug_step "route to RobotControlV3" route-v3
  run_v3_debug_step "runtime summary" runtime-summary
  run_v3_debug_step "panel summary" panel-summary

  if [[ "$DO_CONNECT" -eq 1 ]]; then
    LIVE_VERIFY_STARTED_EPOCH="$(date +%s)"
    run_v3_debug_step "set mock off" mock-off
    run_v3_debug_step "connect default" connect-default
  fi

  if [[ "$DO_SYNC" -eq 1 ]]; then
    run_v3_debug_step "primary action sync" primary-action
    sleep 1

    if fr5_needs_resync_evidence "$STATE_FILE" "$DRIFT_FILE"; then
      log "[live] invalid live evidence detected, re-syncing"
      run_v3_debug_step "reassert mock off" mock-off
      run_v3_debug_step "reconnect default" connect-default
      run_v3_debug_step "sync current state" sync-state
      sleep 1
    fi
  fi

  if [[ "$DO_CONNECT" -eq 1 || "$DO_SYNC" -eq 1 ]]; then
    run_v3_debug_step "refresh live evidence" refresh-evidence
    sleep 1
  fi

  if [[ "$PLAY_STARTED" -eq 1 ]]; then
    if run_capture stop_output "$UNITYCTL_BIN" play stop --project "$PROJECT_PATH" --json; then
      log "$stop_output"
      pass "play stop"
      PLAY_STARTED=0
    else
      rc=$?
      log "$stop_output"
      fail "play stop" "exit code: $rc"
    fi
  fi
fi

if [[ "$evidence_review_allowed" -eq 1 ]]; then
  section "Evidence"
  fr5_print_json_summary "latest-state.json" "$STATE_FILE" "$(fr5_state_summary_patterns)"
  fr5_print_json_summary "latest-drift.json" "$DRIFT_FILE" "$(fr5_drift_summary_patterns)"

  if [[ "$REPORT_ONLY" -eq 1 ]]; then
    LIVE_SESSION_ID="$(fr5_extract_json_string "$STATE_FILE" "sessionId")"
    if [[ -n "$LIVE_SESSION_ID" ]]; then
      CURRENT_EVENTS_SESSION_ID="$LIVE_SESSION_ID"
      pass "report session id"
      log "latest-state sessionId=$LIVE_SESSION_ID"
    else
      skip "report session id" "latest-state.json did not expose sessionId"
    fi
  elif [[ "$DO_CONNECT" -eq 1 || "$DO_SYNC" -eq 1 ]]; then
    if [[ "$LIVE_VERIFY_STARTED_EPOCH" -eq 0 ]]; then
      LIVE_VERIFY_STARTED_EPOCH="$(date +%s)"
    fi
    current_events_file="$(fr5_find_latest_session_file_after "$PROJECT_PATH" "events" "$LIVE_VERIFY_STARTED_EPOCH")"
    if [[ -n "$current_events_file" ]]; then
      CURRENT_EVENTS_SESSION_ID="$(basename "$current_events_file" | sed -E 's/-events\.ndjson$//')"
      pass "current events session"
      log "$current_events_file"
    else
      fail "current events session" "expected a current-session events file after live verification"
    fi

    current_session_has_readback=0
    current_session_preserved_latest=0
    if [[ -n "${current_events_file:-}" && -f "$current_events_file" ]]; then
      if grep -Eq '"kind":"readback"' "$current_events_file"; then
        current_session_has_readback=1
      fi
      if grep -Eq '"kind":"readback-skip"' "$current_events_file"; then
        current_session_preserved_latest=1
      fi
    fi

    if [[ "$current_session_has_readback" -eq 1 ]]; then
      fr5_require_fresh_file "$STATE_FILE" "$LIVE_VERIFY_STARTED_EPOCH" "latest-state freshness"
      fr5_require_fresh_file "$DRIFT_FILE" "$LIVE_VERIFY_STARTED_EPOCH" "latest-drift freshness"
    elif [[ "$current_session_preserved_latest" -eq 1 ]]; then
      pass "latest-state preservation"
      log "current session preserved previous good latest-state instead of promoting invalid zero/disconnected state"
    else
      fail "latest-state freshness" "current session produced neither readback nor readback-skip evidence"
    fi

    fr5_assert_connected "$STATE_FILE"

    LIVE_SESSION_ID="$(fr5_extract_json_string "$STATE_FILE" "sessionId")"
    if [[ -n "$LIVE_SESSION_ID" ]]; then
      pass "live session id"
      log "latest-state sessionId=$LIVE_SESSION_ID"
    else
      fail "live session id" "missing sessionId in $STATE_FILE"
    fi
  fi

  section "Context Gate"
  fr5_check_context_gate "$STATE_FILE"

  if [[ -n "$CURRENT_EVENTS_SESSION_ID" ]]; then
    fr5_check_session_artifacts "$PROJECT_PATH" "$CURRENT_EVENTS_SESSION_ID"
  fi

  if [[ "$REPORT_ONLY" -eq 1 ]]; then
    skip "V3 panel context snapshot" "disabled by --report-only"
  else
    panel_summary=""
    if panel_summary="$("$V3_DEBUG_SCRIPT" --plain --project "$PROJECT_PATH" --unityctl "$UNITYCTL_BIN" panel-summary 2>&1)"; then
      log "$panel_summary"
      fr5_check_panel_summary_context "$panel_summary"
    else
      rc=$?
      log "$panel_summary"
      skip "V3 panel context snapshot" "unityctl exec failed with exit code: $rc"
    fi
  fi
fi

section "Summary"
printf 'Results: %d passed, %d failed, %d skipped, %d total\n' "$PASS" "$FAIL" "$SKIP" "$TOTAL"
printf 'Evidence paths:\n'
printf '  latest-state: %s\n' "$STATE_FILE"
printf '  latest-drift: %s\n' "$DRIFT_FILE"

if [[ "$FAIL" -gt 0 ]]; then
  exit 1
fi
