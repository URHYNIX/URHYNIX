#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=scripts/lib/unityctl_common.sh
source "$SCRIPT_DIR/lib/unityctl_common.sh"

PROJECT_DIR="${PROJECT_DIR:-$(unityctl_default_project)}"
UNITYCTL_RETRY_SECONDS="${UNITYCTL_RETRY_SECONDS:-10}"
STATUS_TIMEOUT_SECONDS="${STATUS_TIMEOUT_SECONDS:-8}"
COMPILE_TIMEOUT_SECONDS="${COMPILE_TIMEOUT_SECONDS:-30}"
CONSOLE_TIMEOUT_SECONDS="${CONSOLE_TIMEOUT_SECONDS:-15}"
TEST_TIMEOUT_SECONDS="${TEST_TIMEOUT_SECONDS:-90}"
RUN_STATUS=1
RUN_COMPILE=1
RUN_CONSOLE=1
TEST_FILTER=""
SHOW_HELP=0

usage() {
  cat <<'EOF'
Usage:
  scripts/unityctl-agent-verify.sh [options]

Options:
  --project PATH        Unity project root (default: repo root)
  --unityctl PATH       unityctl executable path
  --test-filter NAME    Run one extra EditMode filter after compile
  --no-status           Skip unityctl status
  --no-compile          Skip compile check
  --no-console          Skip console read
  -h, --help            Show this help

This is the shortest agent-facing verification loop:
  status -> compile -> console -> optional edit test
EOF
}

note() {
  printf '[INFO] %s\n' "$*"
}

pass() {
  printf '[PASS] %s\n' "$*"
}

fail() {
  printf '[FAIL] %s\n' "$*" >&2
}

while [[ $# -gt 0 ]]; do
  case "$1" in
    --project)
      PROJECT_DIR="$2"
      shift 2
      ;;
    --unityctl)
      UNITYCTL_BIN="$2"
      shift 2
      ;;
    --test-filter)
      TEST_FILTER="$2"
      shift 2
      ;;
    --no-status)
      RUN_STATUS=0
      shift
      ;;
    --no-compile)
      RUN_COMPILE=0
      shift
      ;;
    --no-console)
      RUN_CONSOLE=0
      shift
      ;;
    -h|--help)
      SHOW_HELP=1
      shift
      ;;
    *)
      fail "Unknown argument: $1"
      usage
      exit 1
      ;;
  esac
done

if [[ "$SHOW_HELP" -eq 1 ]]; then
  usage
  exit 0
fi

PROJECT_DIR="$(cd "$PROJECT_DIR" && pwd)"
UNITYCTL_BIN="$(unityctl_resolve_bin "${UNITYCTL_BIN:-}")" || {
  fail "unityctl not found. Set UNITYCTL_BIN or put unityctl on PATH."
  exit 127
}

note "Project: $PROJECT_DIR"
note "UnityCtl: $UNITYCTL_BIN"

if [[ "$RUN_STATUS" -eq 1 ]]; then
  status_output=""
  if unityctl_run_retry status_output "$STATUS_TIMEOUT_SECONDS" "$UNITYCTL_RETRY_SECONDS" "$UNITYCTL_BIN" status --project "$PROJECT_DIR" --json; then
    pass "unityctl status"
    printf '%s\n' "$status_output"
  else
    fail "unityctl status"
    printf '%s\n' "$status_output" >&2
    exit 1
  fi
fi

if [[ "$RUN_COMPILE" -eq 1 ]]; then
  compile_output=""
  if unityctl_run_retry compile_output "$COMPILE_TIMEOUT_SECONDS" "$UNITYCTL_RETRY_SECONDS" "$UNITYCTL_BIN" check --project "$PROJECT_DIR" --type compile --json; then
    pass "unityctl compile"
    printf '%s\n' "$compile_output"
  else
    fail "unityctl compile"
    printf '%s\n' "$compile_output" >&2
    exit 1
  fi
fi

if [[ "$RUN_CONSOLE" -eq 1 ]]; then
  console_output=""
  if unityctl_run_retry console_output "$CONSOLE_TIMEOUT_SECONDS" "$UNITYCTL_RETRY_SECONDS" "$UNITYCTL_BIN" console get-entries --project "$PROJECT_DIR" --limit 30 --json; then
    pass "unityctl console"
    printf '%s\n' "$console_output"
  else
    fail "unityctl console"
    printf '%s\n' "$console_output" >&2
    exit 1
  fi
fi

if [[ -n "$TEST_FILTER" ]]; then
  test_output=""
  if unityctl_run_retry test_output "$TEST_TIMEOUT_SECONDS" "$UNITYCTL_RETRY_SECONDS" "$UNITYCTL_BIN" test --project "$PROJECT_DIR" --mode edit --filter "$TEST_FILTER" --json; then
    pass "unityctl edit test: $TEST_FILTER"
    printf '%s\n' "$test_output"
  else
    fail "unityctl edit test: $TEST_FILTER"
    printf '%s\n' "$test_output" >&2
    exit 1
  fi
fi
