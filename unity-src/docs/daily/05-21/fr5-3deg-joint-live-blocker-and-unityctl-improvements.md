# FR5 3deg Joint Live Blocker and unityctl Improvements

Date: 2026-05-21 KST

## Request

- Confirm current FR5 connection state.
- Test moving every joint by 3 degrees.
- Give the operator UI steps to reproduce the same 3 degree movement.
- Capture skill and `unityctl` improvement candidates separately.

## Evidence Checked

- Network baseline:
  - `nc -vz -G 2 192.168.57.2 8080`: succeeded.
  - `ping -c 2 -W 1000 192.168.57.2`: 0% packet loss.
- Unity baseline:
  - `unityctl status --project /Users/family/jason/FR5UNITY/robotapp --wait --json`: `Playing`, `bridgeLoaded=true`, `ipcPipePresent=true`.
- Initial live evidence:
  - `latest-state.json`: `connected=true`, `enabled=true`, `mode=0`, `toolId=1`, `userId=1`, `coordSystem=Base`, `fault=0/0`.
  - `latest-drift.json`: `severity=ok`.

## Motion Attempt Result

The all-joint 3 degree test was stopped before any hardware MoveJ was dispatched.

Sequence attempted:

1. `SetShellSelection("NavMotion", "TabJointJog", "BottomTabJointJog")`
2. `SetLiveSessionModeForDebug("tiny-movej-only")`
3. `SetShellSpeedPercentForDebug(10)`
4. `SetShellJogIncrementForDebug(1)`
5. `PreviewJointDeltaForDebug(1, 3.0)`
6. `ApplyJointJogForDebug()`
7. `ConfirmPopupForDebug()`

The popup-confirm path worked, but the live gate blocked execution:

- Blocked reason: `state readback failed: 로봇 비활성 상태입니다. Enable 버튼을 눌러주세요. / latest-state freshness failed / latest-drift freshness failed`
- Block artifact: `/Users/family/jason/FR5UNITY/robotapp/Artifacts/robotcontrolv3-live-tiny-movej-blocked.txt`
- Follow-up `SyncCurrentStateForDebug()` returned the same disabled message.
- `EnableServoForDebug()` attempted `RobotEnable(1)` but returned `code=-2`, despite cached state still showing `enabled=true`.

## Operator UI Guide After Enable Is Cleared

Use this only after the status area no longer reports the Enable blocker and a fresh `현재 위치 읽기` succeeds.

1. In `RobotControlV3`, stay on the live FR5 page.
2. Press `연결` if disconnected.
3. Press `현재 위치 읽기`.
4. Confirm the status shows live/auto-ready: connected, enabled, `mode=0`, fault `0/0`.
5. Open `Motion`.
6. Select `Joint Jog`.
7. Set speed to `10`.
8. Keep jog increment at `1`.
9. For each joint row `J1` through `J6`, add `+3.0` degrees to the currently displayed value.
10. Prefer one axis at a time for the first manual reproduction:
    - Change only `J1` by `+3.0`.
    - Press the joint apply button.
    - In the popup, press `이동 실행`.
    - Press `현재 위치 읽기` and verify the readback moved about `+3.0`.
    - Repeat for `J2` through `J6`.

## Skill Improvement Applied

Updated `/Users/family/.codex/skills/fr5-tiny-joint-live-success-pattern/SKILL.md`:

- Removed stale wording that treated only `J5/J6` as proven.
- Added a caution that `RunLiveJointDeltaQaForDebug(...)` can be preflight-only when it returns `dispatch=held`.
- Added a stop rule for the exact mismatch seen here: cached `latest-state.json` says enabled, but `SyncCurrentStateForDebug()` or the live gate reports disabled/not-ready.

## Success-Pattern Skill Candidates

- `fr5-joint-delta-product-ui-success-pattern`: product-like `PreviewJointDeltaForDebug -> ApplyJointJogForDebug -> popup confirm -> Sync/Refresh` path for true hardware motion, separated from preflight helpers.
- `fr5-enabled-truth-mismatch-recovery`: recovery path for `latest-state enabled=true` versus immediate sync/gate disabled errors.
- `fr5-all-joint-delta-expansion-pattern`: only after a clean run proves `J1-J6 +3deg` with post-run readback deltas.

## unityctl Improvement Candidates

- Add a single `unityctl-v3-debug.sh joint-delta-live --axis N --delta 3 --confirm` action that performs sync, refresh, preview, apply, popup confirm, post-sync, and artifact capture in one deterministic flow.
- Add `joint-delta-preflight` and `joint-delta-live` as separate command names so preflight-only output cannot be mistaken for hardware movement.
- Add a compact freshness/readiness probe that returns one line: `connected/enabled/mode/fault/fresh/syncOk/blocker`.
- Add a dedicated recovery action for cached-enabled but sync-disabled mismatch: clear pending preview, reconnect, refresh evidence, and stop with an operator-facing Enable instruction if the SDK still reports disabled.
