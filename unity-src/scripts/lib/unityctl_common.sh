#!/usr/bin/env bash

# Shared helpers for agent-facing unityctl scripts.

unityctl_repo_root() {
  local source_path="${BASH_SOURCE[0]}"
  local script_dir
  script_dir="$(cd "$(dirname "$source_path")" && pwd)"
  cd "$script_dir/../.." && pwd
}

unityctl_default_project() {
  local root="${1:-$(unityctl_repo_root)}"
  printf '%s\n' "$root"
}

unityctl_resolve_bin() {
  local requested="${1:-${UNITYCTL_BIN:-${UNITYCTL:-${UNITYCTL_EXE:-}}}}"

  if [[ -n "$requested" ]]; then
    printf '%s\n' "$requested"
    return 0
  fi

  if command -v unityctl >/dev/null 2>&1; then
    command -v unityctl
    return 0
  fi

  return 1
}

unityctl_is_retryable_output() {
  local output="${1:-}"
  rg -q 'IPC is not ready yet|domain reload|Pipe closed before full message was read|statusCode"[[:space:]]*:[[:space:]]*103|statusCode[[:space:]]*:[[:space:]]*103|statusCode"[[:space:]]*:[[:space:]]*504|statusCode[[:space:]]*:[[:space:]]*504|Test run failed before execution: An unexpected error happened while running tests\.' <<<"$output"
}

unityctl_run_with_timeout() {
  local __outvar="$1"
  local timeout_seconds="$2"
  shift 2
  local output

  if output="$(python3 - "$timeout_seconds" "$@" 2>&1 <<'PY'
import subprocess
import sys

timeout = float(sys.argv[1])
cmd = sys.argv[2:]

try:
    completed = subprocess.run(
        cmd,
        stdout=subprocess.PIPE,
        stderr=subprocess.STDOUT,
        text=True,
        timeout=timeout,
    )
    sys.stdout.write(completed.stdout or "")
    raise SystemExit(completed.returncode)
except subprocess.TimeoutExpired as exc:
    if exc.stdout:
        sys.stdout.write(exc.stdout.decode() if isinstance(exc.stdout, bytes) else exc.stdout)
    sys.stdout.write(f"[timeout] {' '.join(cmd)} exceeded {timeout:.0f}s\n")
    raise SystemExit(142)
PY
)"; then
    printf -v "$__outvar" '%s' "$output"
    return 0
  fi

  local rc=$?
  printf -v "$__outvar" '%s' "$output"
  return "$rc"
}

unityctl_run_retry() {
  local __outvar="$1"
  local timeout_seconds="$2"
  local retry_seconds="$3"
  shift 3
  local output=""
  local rc=0
  local attempt

  for ((attempt = 1; attempt <= retry_seconds; attempt++)); do
    if unityctl_run_with_timeout output "$timeout_seconds" "$@"; then
      printf -v "$__outvar" '%s' "$output"
      return 0
    fi

    rc=$?
    if [[ "$rc" -eq 142 ]] || unityctl_is_retryable_output "$output"; then
      sleep 1
      continue
    fi

    break
  done

  printf -v "$__outvar" '%s' "$output"
  return "$rc"
}

unityctl_json_field() {
  local json_file="$1"
  local key="$2"

  python3 - "$json_file" "$key" <<'PY' 2>/dev/null || true
import json
import sys

path, key = sys.argv[1], sys.argv[2]
with open(path, "r", encoding="utf-8") as f:
    data = json.load(f)
value = data.get(key, "")
if isinstance(value, (dict, list)):
    print(json.dumps(value, ensure_ascii=False))
else:
    print(value)
PY
}

unityctl_print_compile_summary() {
  local json_file="$1"
  local message
  message="$(unityctl_json_field "$json_file" "message")"
  if [[ -n "$message" ]]; then
    printf 'unityctl compile: %s\n' "$message"
  else
    printf 'unityctl compile: completed\n'
  fi
}

unityctl_exec_code() {
  local __outvar="$1"
  local unityctl_bin="$2"
  local project_dir="$3"
  local timeout_seconds="$4"
  local retry_seconds="$5"
  local code="$6"

  unityctl_run_retry "$__outvar" "$timeout_seconds" "$retry_seconds" \
    "$unityctl_bin" exec --project "$project_dir" --code "$code" --json
}
