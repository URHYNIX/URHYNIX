#!/usr/bin/env bash

# Shared FR5 evidence parsing helpers.
# Callers are expected to define `log`, `pass`, `fail`, and `skip`.

fr5_state_summary_patterns() {
  printf '%s\n' \
    '"clientMode"[[:space:]]*:[[:space:]]*"[^"]+"' \
    '"sdkLoadStatus"[[:space:]]*:[[:space:]]*"[^"]+"' \
    '"sdkRuntime"[[:space:]]*:[[:space:]]*"[^"]+"' \
    '"sdk"[[:space:]]*:[[:space:]]*"[^"]+"' \
    '"toolId"[[:space:]]*:[[:space:]]*[0-9]+' \
    '"userId"[[:space:]]*:[[:space:]]*[0-9]+' \
    '"coordSystem"[[:space:]]*:[[:space:]]*"[^"]+"'
}

fr5_drift_summary_patterns() {
  printf '%s\n' \
    '"severity"[[:space:]]*:[[:space:]]*"[^"]+"' \
    '"maxJointDeg"[[:space:]]*:[[:space:]]*[0-9.eE+-]+' \
    '"maxTcpMm"[[:space:]]*:[[:space:]]*[0-9.eE+-]+' \
    '"maxTcpRotDeg"[[:space:]]*:[[:space:]]*[0-9.eE+-]+' \
    '"liveBlockedReason"[[:space:]]*:[[:space:]]*"[^"]*"'
}

fr5_print_json_summary() {
  local label="$1"
  local file="$2"
  local fields="$3"

  if [[ ! -f "$file" ]]; then
    skip "$label" "missing: $file"
    return 0
  fi

  pass "$label" "$file"
  log "  path: $file"
  while IFS= read -r pattern; do
    [[ -n "$pattern" ]] || continue
    grep -E "$pattern" "$file" || true
  done <<<"$fields"
}

fr5_require_fresh_file() {
  local file="$1"
  local started_epoch="$2"
  local label="$3"

  if [[ ! -f "$file" ]]; then
    fail "$label" "missing file: $file"
    return 1
  fi

  local modified_epoch
  modified_epoch="$(stat -f '%m' "$file" 2>/dev/null || true)"
  if [[ -z "$modified_epoch" ]]; then
    fail "$label" "could not read mtime: $file"
    return 1
  fi

  if [[ "$modified_epoch" -lt "$started_epoch" ]]; then
    fail "$label" "stale file: $file (mtime=$modified_epoch < started=$started_epoch)"
    return 1
  fi

  pass "$label"
  log "mtime=$modified_epoch file=$file"
  return 0
}

fr5_extract_json_string() {
  local file="$1"
  local key="$2"
  rg -o "\"$key\"[[:space:]]*:[[:space:]]*\"[^\"]+\"" "$file" 2>/dev/null | tail -1 | sed -E "s/.*\"$key\"[[:space:]]*:[[:space:]]*\"([^\"]+)\".*/\\1/" || true
}

fr5_extract_json_number() {
  local file="$1"
  local key="$2"
  rg -o "\"$key\"[[:space:]]*:[[:space:]]*[0-9]+" "$file" 2>/dev/null | tail -1 | rg -o '[0-9]+' | tail -1 || true
}

fr5_find_latest_session_file_after() {
  local project_path="$1"
  local suffix="$2"
  local started_epoch="$3"
  local latest_file=""
  local latest_mtime=0
  local file

  while IFS= read -r file; do
    [[ -n "$file" ]] || continue
    local file_mtime
    file_mtime="$(stat -f '%m' "$file" 2>/dev/null || true)"
    [[ -n "$file_mtime" ]] || continue
    if [[ "$file_mtime" -ge "$started_epoch" && "$file_mtime" -ge "$latest_mtime" ]]; then
      latest_mtime="$file_mtime"
      latest_file="$file"
    fi
  done < <(find "$project_path/Artifacts/live/fr5/sessions" -name "*-${suffix}.ndjson" -print 2>/dev/null)

  printf '%s' "$latest_file"
}

fr5_extract_coord_system() {
  local file="$1"
  rg -o '"coordSystem"[[:space:]]*:[[:space:]]*"[^"]+"' "$file" 2>/dev/null | tail -1 | sed -E 's/.*"coordSystem"[[:space:]]*:[[:space:]]*"([^"]+)".*/\1/' || true
}

fr5_check_context_gate() {
  local state_file="$1"
  local tool_id user_id coord_system

  tool_id="$(fr5_extract_json_number "$state_file" "toolId")"
  user_id="$(fr5_extract_json_number "$state_file" "userId")"
  coord_system="$(fr5_extract_coord_system "$state_file")"

  if [[ -n "$tool_id" && "$tool_id" -gt 0 ]]; then
    pass "toolId context"
    log "toolId=$tool_id"
  else
    fail "toolId context" "expected toolId > 0 in $state_file"
  fi

  if [[ -n "$user_id" && "$user_id" -gt 0 ]]; then
    pass "userId context"
    log "userId=$user_id"
  else
    fail "userId context" "expected userId > 0 in $state_file"
  fi

  if [[ "$coord_system" == "Base" || "$coord_system" == "Tool" || "$coord_system" == "User" ]]; then
    pass "TCP coord context"
    log "coordSystem=$coord_system"
  else
    fail "TCP coord context" "expected coordSystem Base|Tool|User in $state_file"
  fi
}

fr5_check_session_artifacts() {
  local project_path="$1"
  local session_id="$2"

  [[ -n "$session_id" ]] || return 0

  local readback_session_file="$project_path/Artifacts/live/fr5/sessions/${session_id}-readback.ndjson"
  local events_session_file="$project_path/Artifacts/live/fr5/sessions/${session_id}-events.ndjson"

  if [[ -f "$readback_session_file" ]]; then
    pass "session readback file"
    log "$readback_session_file"
  else
    if [[ -f "$events_session_file" ]] && grep -Eq '"kind":"readback-skip"' "$events_session_file"; then
      skip "session readback file" "current session preserved previous latest-state; no new readback file promoted"
    else
      fail "session readback file" "missing: $readback_session_file"
    fi
  fi

  if [[ -f "$events_session_file" ]]; then
    pass "session events file"
    log "$events_session_file"
    if grep -Eq '"kind":"readback"' "$events_session_file"; then
      pass "session readback event"
    elif grep -Eq '"kind":"readback-skip"' "$events_session_file"; then
      pass "session readback preservation event"
    else
      fail "session readback event" "expected readback or readback-skip event in $events_session_file"
    fi
  else
    fail "session events file" "missing: $events_session_file"
  fi
}

fr5_needs_resync_evidence() {
  local state_file="$1"
  local drift_file="$2"

  if [[ -f "$state_file" ]] && grep -q '"clientMode"[[:space:]]*:[[:space:]]*"mock"' "$state_file"; then
    return 0
  fi
  if [[ -f "$drift_file" ]] && grep -q '"severity"[[:space:]]*:[[:space:]]*"danger"' "$drift_file"; then
    return 0
  fi
  if [[ -f "$state_file" ]] && grep -q '"connected"[[:space:]]*:[[:space:]]*false' "$state_file"; then
    return 0
  fi
  if [[ -f "$state_file" ]] && grep -q '"toolId"[[:space:]]*:[[:space:]]*0' "$state_file"; then
    return 0
  fi
  if [[ -f "$state_file" ]] && grep -q '"userId"[[:space:]]*:[[:space:]]*0' "$state_file"; then
    return 0
  fi

  return 1
}

fr5_assert_connected() {
  local state_file="$1"

  if grep -Eq '"connected"[[:space:]]*:[[:space:]]*true' "$state_file"; then
    pass "latest-state connected"
  else
    fail "latest-state connected" "expected connected=true in $state_file"
  fi
}

fr5_check_panel_summary_context() {
  local panel_summary="$1"
  local label="${2:-V3 panel context snapshot}"

  if grep -Eq 'Tool:[[:space:]]*[0-9]{2}|tool=[0-9]{2}|user=[0-9]{2}|coord=Base|coord=Tool|coord=User' <<<"$panel_summary"; then
    pass "$label"
  else
    skip "$label" "panel summary did not expose explicit tool/user tokens, kept TcpJog + latest-state as source of truth"
  fi
}
