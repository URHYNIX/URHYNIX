#!/bin/bash
# KineTutor3D 통합 검증 스크립트
# dotnet build + unityctl 기본 검증 + 코딩 규칙 검증
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PROJECT_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"
# shellcheck source=scripts/lib/unityctl_common.sh
source "$SCRIPT_DIR/lib/unityctl_common.sh"
PASS=0
FAIL=0
VALIDATE_MODE="strict"

usage() {
    cat <<'EOF'
Usage:
  scripts/validate.sh [--fast|--strict]

Modes:
  --fast    Skip dotnet build and EditMode matrix. Best for agent inner loop.
  --strict  Run dotnet build + unityctl quick verify + coding rules + EditMode matrix.

Default:
  --strict
EOF
}

check() {
    local name="$1"
    shift
    echo -n "[$name] "
    if "$@" > /dev/null 2>&1; then
        echo "PASS"
        PASS=$((PASS + 1))
    else
        echo "FAIL"
        FAIL=$((FAIL + 1))
    fi
}

while [[ $# -gt 0 ]]; do
    case "$1" in
        --fast)
            VALIDATE_MODE="fast"
            shift
            ;;
        --strict)
            VALIDATE_MODE="strict"
            shift
            ;;
        -h|--help)
            usage
            exit 0
            ;;
        *)
            echo "Unknown argument: $1" >&2
            usage >&2
            exit 1
            ;;
    esac
done

echo "═══════════════════════════════════════════"
echo "  KineTutor3D Validation Pipeline"
echo "═══════════════════════════════════════════"
echo "Mode: $VALIDATE_MODE"
echo ""

# Step 1: dotnet build (if available)
if [[ "$VALIDATE_MODE" == "strict" ]] && command -v dotnet &> /dev/null; then
    echo "── Step 1: dotnet build ──"
    check "dotnet build" dotnet build "$PROJECT_DIR/robotapp.slnx"
elif [[ "$VALIDATE_MODE" == "fast" ]]; then
    echo "── Step 1: dotnet build (SKIPPED - fast mode) ──"
else
    echo "── Step 1: dotnet build (SKIPPED - dotnet not found) ──"
fi

echo ""

# Step 2: unityctl agent loop
if UNITYCTL_BIN="$(unityctl_resolve_bin "${UNITYCTL_BIN:-}")"; then
    echo "── Step 2: unityctl quick verify ──"
    check "unityctl status/compile/console" bash "$SCRIPT_DIR/unityctl-agent-verify.sh" --project "$PROJECT_DIR"
else
    echo "── Step 2: unityctl checks (SKIPPED - unityctl not found) ──"
fi

echo ""

# Step 3: Coding rules (pre-commit-check.sh)
echo "── Step 3: Coding rules ──"
if [ -x "$SCRIPT_DIR/pre-commit-check.sh" ]; then
    check "pre-commit-check" "$SCRIPT_DIR/pre-commit-check.sh"
else
    echo "  pre-commit-check.sh not found or not executable — SKIPPED"
fi

echo ""

# Step 4: unityctl EditMode tests (if available)
if [[ "$VALIDATE_MODE" == "strict" ]] && UNITYCTL_BIN="$(unityctl_resolve_bin "${UNITYCTL_BIN:-}")"; then
    echo "── Step 4: EditMode matrix via unityctl ──"
    check "run-tests edit matrix" bash "$SCRIPT_DIR/tests/run_editmode_matrix.sh"
elif [[ "$VALIDATE_MODE" == "fast" ]]; then
    echo "── Step 4: EditMode matrix (SKIPPED - fast mode) ──"
else
    echo "── Step 4: EditMode tests (SKIPPED - unityctl not found) ──"
fi

echo ""
echo "═══════════════════════════════════════════"
echo "  Results: $PASS passed, $FAIL failed"
echo "═══════════════════════════════════════════"

if [ "$FAIL" -gt 0 ]; then
    exit 1
fi
