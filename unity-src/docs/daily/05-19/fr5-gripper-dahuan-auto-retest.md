# FR5 Gripper DAHUAN Auto Retest

Date: 2026-05-19 (KST)

## Context

Operator changed the real teach pendant state from manual mode to auto mode and changed the gripper setting to `DAHUAN`.

Before that field change, the same Unity session could connect/sync but gripper close was not green: `100%` open reached SDK `position=96`, while repeated `0%` close attempts returned to SDK `position=96`.

## Fresh Evidence

- Network:
  - Mac Ethernet `192.168.57.10/24`
  - `ping 192.168.57.2`: 0% loss
  - `nc 192.168.57.2 8080`: success
- Unity/V3:
  - Unity play mode active
  - V3 runtime `connected=True`, `enabled=True`, `dryRun=False`
  - `SyncCurrentStateForDebug()` returned `[Sync] 현재 자세 동기화 완료`
  - `RefreshLiveEvidenceForDebug()` recorded `tool=01 user=01 coord=Base`
- Gripper SDK after pendant change:
  - `profile=(company=2; device=4; soft=0; bus=0; index=1)`
  - `activationFault=0`
  - `activationMask=1`
  - `positionFault=0`

## Result

Explicit `gripper-only` write:

- `SetGripperPositionForDebug(0)`:
  - after 5 seconds SDK readback = `position=0`, `done=1`
  - Easy Motion state = `requested=0; actual=0; raw=0; readback=True`
- `SetGripperPositionForDebug(100)`:
  - after 5 seconds SDK readback = `position=100`, `done=1`
  - Easy Motion state = `requested=100; actual=100; raw=100; readback=True`

Easy Motion apply path:

- `SetEasyMotionGripperInputForDebug(0, true)`:
  - after 5 seconds SDK readback = `position=0`, `done=1`
  - Easy Motion state = `requested=0; actual=0; raw=0; readback=True`
- `SetEasyMotionGripperInputForDebug(100, true)`:
  - after 5 seconds SDK readback = `position=100`, `done=1`
  - Easy Motion state = `requested=100; actual=100; raw=100; readback=True`

## Remaining Issues

- Command result still first reports `readback 확인 안 됨`; SDK/UI converge after a short delay.
- Easy Motion debug state can leave `pending=True` and stale `lastApply` after successful convergence.
- The Easy Motion apply smoke used an existing debug session approval, so a clean popup-open/confirm path still needs a fresh check.

## Current Interpretation

The latest blocker was not Unity connect/sync and not a permanent Easy Motion failure. With the real controller in auto mode and gripper set to `DAHUAN`, live gripper 0/100 operation and UI readback convergence are green for this narrow smoke.
