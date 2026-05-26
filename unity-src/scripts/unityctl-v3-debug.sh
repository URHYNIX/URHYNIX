#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=scripts/lib/unityctl_common.sh
source "$SCRIPT_DIR/lib/unityctl_common.sh"

PROJECT_DIR="${PROJECT_DIR:-$(unityctl_default_project)}"
UNITYCTL_RETRY_SECONDS="${UNITYCTL_RETRY_SECONDS:-10}"
EXEC_TIMEOUT_SECONDS="${EXEC_TIMEOUT_SECONDS:-45}"
ACTION=""
RAW_CODE=""
PLAIN_OUTPUT=0

usage() {
  cat <<'EOF'
Usage:
  scripts/unityctl-v3-debug.sh <action> [options]
  scripts/unityctl-v3-debug.sh raw-code --code 'KineTutor3D.App.SceneNavigator.LoadByName("RobotControlV3")'

Actions:
  runtime-summary    Get `RobotControlV3DebugBridge.GetV3RuntimeSummary()`
  panel-summary      Get `RobotControlV3DebugBridge.GetPanelControllerSummary()`
  route-v3           Load `RobotControlV3`
  mock-off           Set debug runtime to live mode
  connect-default    Run default FR5 connect path
  primary-action     Run primary debug action
  sync-state         Run `SyncCurrentStateForDebug()`
  refresh-evidence   Run `RefreshLiveEvidenceForDebug()`
  gate-summary       Run `GetTinyMoveJGateSummaryForDebug()`
  set-poll-33ms      Run `SetLivePollIntervalForDebug(0.033)`
  reset-readback-probe
                     Run `ResetLiveReadbackProbeForDebug()`
  readback-probe-summary
                     Run `GetLiveReadbackProbeSummaryForDebug()`
  force-read-failures-2
                     Run `ForceNextReadFailuresForDebug(2)`
  raw-code           Run arbitrary Unity C# snippet via `--code`

Options:
  --project PATH     Unity project root
  --unityctl PATH    unityctl executable path
  --code TEXT        Required for `raw-code`
  --plain            Print raw unityctl output without pass/fail wrapper
  -h, --help         Show this help
EOF
}

fail() {
  printf '[FAIL] %s\n' "$*" >&2
}

pass() {
  printf '[PASS] %s\n' "$*"
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
    --code)
      RAW_CODE="$2"
      shift 2
      ;;
    --plain)
      PLAIN_OUTPUT=1
      shift
      ;;
    -h|--help)
      usage
      exit 0
      ;;
    *)
      if [[ -z "$ACTION" ]]; then
        ACTION="$1"
        shift
      else
        fail "Unknown argument: $1"
        usage
        exit 1
      fi
      ;;
  esac
done

if [[ -z "$ACTION" ]]; then
  usage
  exit 1
fi

PROJECT_DIR="$(cd "$PROJECT_DIR" && pwd)"
UNITYCTL_BIN="$(unityctl_resolve_bin "${UNITYCTL_BIN:-}")" || {
  fail "unityctl not found. Set UNITYCTL_BIN or put unityctl on PATH."
  exit 127
}

case "$ACTION" in
  runtime-summary)
    CODE='KineTutor3D.App.RobotControlV3DebugBridge.GetV3RuntimeSummary()'
    ;;
  panel-summary)
    CODE='KineTutor3D.App.RobotControlV3DebugBridge.GetPanelControllerSummary()'
    ;;
  route-v3)
    CODE='KineTutor3D.App.SceneNavigator.LoadByName("RobotControlV3")'
    ;;
  mock-off)
    CODE='KineTutor3D.App.RobotControlV3DebugBridge.SetMockModeForDebug(false)'
    ;;
  connect-default)
    CODE='KineTutor3D.App.RobotControlV3DebugBridge.ConnectDefaultForDebug()'
    ;;
  primary-action)
    CODE='KineTutor3D.App.RobotControlV3DebugBridge.ExecutePrimaryActionForDebug()'
    ;;
  sync-state)
    CODE='KineTutor3D.App.RobotControlV3DebugBridge.SyncCurrentStateForDebug()'
    ;;
  refresh-evidence)
    CODE='KineTutor3D.App.RobotControlV3DebugBridge.RefreshLiveEvidenceForDebug()'
    ;;
  gate-summary)
    CODE='KineTutor3D.App.RobotControlV3DebugBridge.GetTinyMoveJGateSummaryForDebug()'
    ;;
  set-poll-33ms)
    CODE='KineTutor3D.App.RobotControlV3DebugBridge.SetLivePollIntervalForDebug(0.033)'
    ;;
  reset-readback-probe)
    CODE='KineTutor3D.App.RobotControlV3DebugBridge.ResetLiveReadbackProbeForDebug()'
    ;;
  readback-probe-summary)
    CODE='KineTutor3D.App.RobotControlV3DebugBridge.GetLiveReadbackProbeSummaryForDebug()'
    ;;
  force-read-failures-2)
    CODE='KineTutor3D.App.RobotControlV3DebugBridge.ForceNextReadFailuresForDebug(2)'
    ;;
  raw-code)
    CODE="$RAW_CODE"
    ;;
  *)
    fail "Unknown action: $ACTION"
    usage
    exit 1
    ;;
esac

if [[ -z "$CODE" ]]; then
  fail "--code is required for raw-code"
  exit 1
fi

output=""
if unityctl_exec_code output "$UNITYCTL_BIN" "$PROJECT_DIR" "$EXEC_TIMEOUT_SECONDS" "$UNITYCTL_RETRY_SECONDS" "$CODE"; then
  if [[ "$PLAIN_OUTPUT" -eq 0 ]]; then
    pass "$ACTION"
  fi
  printf '%s\n' "$output"
else
  if [[ "$PLAIN_OUTPUT" -eq 0 ]]; then
    fail "$ACTION"
  fi
  printf '%s\n' "$output" >&2
  exit 1
fi
