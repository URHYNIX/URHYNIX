---
title: "FR5 IPC Tiny Joint And Gripper Retest"
doc_type: "daily-log"
status: "active"
domain: "fr5-live"
audience: "human-and-agent"
last_updated: "2026-05-14"
---

# FR5 IPC Tiny Joint And Gripper Retest

## Summary

- Unity IPC hangs were traced to synchronous FAIRINO SDK readback/RPC calls blocking Unity responsiveness, not to the IPC bridge itself.
- The live tiny MoveJ debug path was stabilized by avoiding synchronous state readback during the fresh-evidence tiny MoveJ gate and by suppressing debug live polling during the motion smoke.
- Arm tiny MoveJ smoke passed on the current branch for `J1~J6`, `+1deg -> -1deg`, with `feedback=OK` on each command.
- Gripper was rechecked separately in `gripper-only` mode and is not current-green.

## Verification

- Unity scene: `RobotControlV3`
- FR5 connection: `192.168.57.2:8080`
- Tiny MoveJ setup:
  - `speed=10`
  - `increment=1`
  - session mode `tiny-movej-only`
  - cached live evidence fresh before each dispatch
- Tiny MoveJ result:
  - `J1 +1 / -1`: OK
  - `J2 +1 / -1`: OK
  - `J3 +1 / -1`: OK
  - `J4 +1 / -1`: OK
  - `J5 +1 / -1`: OK
  - `J6 +1 / -1`: OK

## Gripper Result

- Gripper was kept separate from arm motion and tested only in `gripper-only`.
- Current session preflight returned:
  - `ConnectedServoOff`
  - `enabled=False`
  - `tool=00`
  - `user=00`
  - gripper readback `motionFault=-2`, `activationFault=-2`, `positionFault=-2`, `position=0`
- `EnableServoForDebug()` returned `RobotEnable(1) 실패 · code=-2 · connected=True · enabled=False · mode=0 · drag=False`.
- Because this is a stop condition, no gripper live write was sent in the first retest.
- A follow-up Unity restart cleared stale FR5 SDK sockets and restored the preflight to `ReadyToJog`, `enabled=True`, `tool=01`, `user=01`, `activationFault=0`, `positionFault=0`.
- In that follow-up:
  - `70%` returned command sent, but readback remained unconfirmed: `motionFault=1; done=0; positionFault=0; raw=0`.
  - `100%` return failed with `그리퍼 동작 오류: gripper 통신, reset/activate 초기화, 장치 번호와 기능 설정을 확인하세요.`
  - The FR5 connection was disconnected after the failed return command.

## Doc Sync

- `fr5-gripper-live-success-pattern.md` now separates historical 2026-04-29 gripper success from the 2026-05-14 current blocker and follow-up `70 sent / 100 failed` result.
- `ACTIVE-WORK-INDEX.md`, `PROJECT-STATUS.md`, `FR5-LIVE-INTEGRATION-ROADMAP.md`, `fr5-live-field-checklist.md`, and `robotcontrol-next-session-handoff.md` now mark gripper as blocked until controller/servo enable and gripper readback baseline are recovered.

## UI Consolidation Follow-Up

- Legacy `IoPanelController` no longer owns gripper controls. It clears and hides `IoPanelHost` / `IoSheetHost` and reports `deprecated=True; mergedInto=EasyMotion`.
- `Easy Motion` now owns the single operator gripper surface: quick values `열기 100 / 중간 50 / 닫기 0`, slider, numeric input, mode/gate/readback labels, and `프리뷰 / 실제 적용` split.
- Safe UI verification used preview-only input: `SetEasyMotionGripperInputForDebug(50, false)` returned `requested=50; actual=100; raw=100; draft=50; pending=True; readback=True`; no live gripper write was sent in this UI consolidation check.

## Next

1. Ensure Unity stale SDK sockets are closed before gripper retest.
2. Re-run gripper reset/activate/device config checks before any movement command.
3. Require `activationFault=0`, `positionFault=0`, `toolId > 0`, `userId > 0`, and a readback that changes after command.
4. Only then retry the narrow `70 -> 100` gripper visible smoke.
