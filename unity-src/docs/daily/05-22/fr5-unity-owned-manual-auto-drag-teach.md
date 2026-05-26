# 2026-05-22 - FR5 Unity-Owned Manual/Auto Drag Teach

## Request

- 티칭포인트 테스트 전에 기존 티칭펜던트 없이 Unity에서 수동/자동을 직접 켜고 끌 수 있게 한다.
- 수동 이동 후 포인트 저장/실행 속도를 높인다.
- `unityctl`로 필요한 호출 표면과 수정사항을 기록한다.

## What Changed

- `IFairinoRobotClient`에 `EnterDragTeach()` 계약을 추가했다.
- `LiveFairinoClient.EnterDragTeach()`는 FAIRINO SDK `DragTeachSwitch(1)`을 호출한다.
- `RequestManualMode()` 경로는 이제 `Mode(1)` 뒤 `DragTeachSwitch(1)`까지 실행하고, readback에서 `mode=1`, `drag=true`를 확인한다.
- `RequestAutoMode()` 경로는 기존처럼 drag teach를 끈 뒤 `Mode(0)`을 확인한다.
- V3 `수동` 버튼은 `mode=1`이어도 `drag=false`이면 다시 누를 수 있게 했다.
- `RobotControlV3DebugBridge`에 아래 unityctl 직접 호출용 helper를 추가했다.
  - `RequestManualModeForDebug()`
  - `RequestAutoModeForDebug()`

## Unityctl Recipe

Manual capture prep:

```bash
unityctl exec --project /Users/family/jason/FR5UNITY/robotapp --code "KineTutor3D.App.RobotControlV3DebugBridge.RequestManualModeForDebug()" --json
```

Return to auto before point save/apply/run:

```bash
unityctl exec --project /Users/family/jason/FR5UNITY/robotapp --code "KineTutor3D.App.RobotControlV3DebugBridge.RequestAutoModeForDebug()" --json
```

Teaching-point guard after manual motion:

```bash
unityctl exec --project /Users/family/jason/FR5UNITY/robotapp --code "KineTutor3D.App.RobotControlV3DebugBridge.SyncCurrentStateForDebug()" --json
unityctl exec --project /Users/family/jason/FR5UNITY/robotapp --code "KineTutor3D.App.RobotControlV3DebugBridge.RefreshLiveEvidenceForDebug()" --json
```

## Verification

- `dotnet build /Users/family/jason/FR5UNITY/robotapp/robotapp.slnx -nologo`
- Result: errors `0`, warnings only.

## Doc Sync

- `PROJECT-STATUS`, `ACTIVE-WORK-INDEX`, and `FR5-LIVE-INTEGRATION-ROADMAP` now distinguish the 2026-04-29 mode-only field green result from the 2026-05-22 drag-teach-on implementation that still needs live smoke.
- `fr5-auto-manual-mode-transition-plan.md` is the SSOT for the `수동=Mode(1)+DragTeachSwitch(1)` and `자동=DragTeachSwitch(0)+Mode(0)` flow.
- `robotcontrol-next-session-handoff.md` now tells the next operator to use Unity `수동 -> 손이동 -> Sync/Refresh -> 자동` before teaching point save/run.
- `progress-checklist.md` and `fr5-live-official-sdk-audit.md` now include `EnterDragTeach()` and the pending app-owned drag capture live check.

## Next Live Check

1. `RequestManualModeForDebug()` and confirm fresh `latest-state.json` reports `mode=1`, `isInDragTeach=true`.
2. Move the robot by hand.
3. `SyncCurrentStateForDebug()` and `RefreshLiveEvidenceForDebug()`.
4. `RequestAutoModeForDebug()` and confirm `mode=0`, `isInDragTeach=false`.
5. Save/apply/run the teaching point only after the auto-mode truth is fresh.
