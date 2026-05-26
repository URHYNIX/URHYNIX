# FR5 Gripper Live Success Pattern

Last Updated: 2026-05-19 (KST)

## Purpose

이 문서는 `/Users/family/jason/FR5UNITY/robotapp`에서 `Unity -> 실기 gripper` live 조작이 실제로 먹었던 조건만 고정한다.

목표는 두 가지다.

- 다음 세션에서 같은 성공 패턴을 다시 밟게 한다.
- `joint/tcp motion`과 `gripper live write`를 절대 섞지 않게 한다.

## Locked Truth

- 현재 대상은 `gripper-only` live control이다.
- arm `tiny MoveJ`와 gripper live는 같은 세션으로 열지 않는다.
- operator baseline flow는 `연결 + 위치 읽기` 1단계다.
- operator write flow는 `값 선택 -> 미리보기 적용 또는 실제 이동` 2버튼 분리다.
- current truth source는 FR5 controller readback이다.
- user percent와 SDK raw percent는 아직 `1:1`이 아니다.
- current branch calibration baseline은 `user 0% -> raw 0`이다.
- 2026-05-14 재검증 기준, 이전 `70 -> 100` green 패턴을 그대로 current green으로 일반화하지 않는다. 첫 세션은 gripper readback `motionFault=-2 / activationFault=-2 / positionFault=-2`와 `RobotEnable(1) code=-2`로 write 전 중단했고, Unity 재시작 후 재시도는 `70%` command sent / readback unconfirmed 뒤 `100%` 복귀가 gripper 동작 오류로 실패했다.
- 2026-05-15 current branch 기준, `연결 + 위치 읽기`가 실제로 성공한 세션에서는 UI/runtime gripper readback이 실기 닫힘 상태 `0%`를 다시 따라온다. 다만 같은 날 후속 재테스트에서는 `ping 192.168.57.2`는 성공했지만 `nc -vz 192.168.57.2 8080`이 timeout이라 fresh live connect가 실패했고, 그 상태의 V3는 다시 `requested=100; actual=100; raw=100; readback=False` 기본값에 머문다. 따라서 현재 문제는 `sync 후 0을 절대 못 읽음`이 아니라 `fresh 8080 연결이 불안정하면 UI가 default 100/readback false에 남음`이다.
- 2026-05-19 fresh field 기준, 네트워크와 V3 connect/sync는 회복됐다: Mac `en5=192.168.57.10/24`, `ping 192.168.57.2` 0% loss, `nc 192.168.57.2 8080` success, session `20260519-091601`, `connected=true`, `enabled=true`, `mode=1`, `toolId=1`, `userId=1`, `coordSystem=Base`, drift `ok`. 다만 `restart_v3_live_loop.sh` 계열 gate는 current `clientMode=direct-motion`을 `direct|bridge`로만 검사하면 false fail을 낼 수 있다.
- 2026-05-19 후속 현장 조치 기준, operator가 실제 teach pendant에서 `수동 -> 자동`으로 전환하고 gripper를 `DAHUAN`으로 바꾼 뒤 gripper path가 회복됐다. SDK profile은 `company=2; device=4; soft=0; bus=0; index=1`, `activationMask=1`, `activationFault=0`, `positionFault=0`으로 읽혔다. 이 조건에서는 explicit `gripper-only` write와 Easy Motion apply path 모두 `0% -> SDK position=0` / `100% -> SDK position=100`까지 수렴했다.

## Known-Good Field Conditions

### Robot / Gripper

- 제조업체: `DAHUAN`
- 유형: `PGI-140`
- 소프트웨어 버전: `D1.0`
- 마운트 위치: `말단 1번 포트`

### Network

- FR5 `eth0 = 192.168.57.2`
- MacBook Ethernet = `192.168.57.10/24`
- current live baseline uses `192.168.57.2:8080`

### Session / Flags

- default session: `readback-only`
- write session: `gripper-only`
- required live smoke flag: `FAIRINO_ENABLE_LIVE_GRIPPER_SMOKE=1`

## Must-Pass Order

1. Unity 재기동 뒤 `RobotControlV3`로 진입
2. `연결 + 위치 읽기`
3. live evidence 확인
4. gripper readback 재확인
5. `Easy Motion`에서 목표 퍼센트 입력 또는 preset 선택
6. `미리보기 적용` 또는 `실제 이동` 중 하나를 명시적으로 고른다
7. `실제 이동`을 고른 경우에만 `이동 실행 확인` popup confirm
8. `gripper-only` 세션 write 1회
9. readback 재확인
10. 자동 `readback-only` 복귀 확인

## Readback Baseline

다음 값이 먼저 보이면 시작점으로 본다.

- `clientMode=direct`
- `toolId=1`
- `userId=1`
- `coordSystem=Base`
- `sdkConfig matchesExpected=True`
- `activationFault=0`
- `positionFault=0`

`motionFault=1`이 남아 있어도, 실제 위치 readback이 바뀌면 discrete gripper live write 자체는 먹을 수 있다.

다음 값이면 시작점으로 보지 않는다.

- `enabled=false`
- `toolId=0` 또는 `userId=0`
- `activationFault=-2`
- `positionFault=-2`
- `RobotEnable(1)` 실패

이 조합에서는 gripper write를 보내지 말고 controller/servo enable 상태를 먼저 복구한다.

## Known Successful Patterns

### Connect / Sync

- `BtnConnect` 1회
- expected result:
  - `status=ConnectedServoOff`
  - feedback = `[Sync] 현재 자세 동기화 완료`
  - gripper readback이 살아 있으면 runtime/easy-motion summary가 `Actual 0%` 또는 현재 실기 값으로 따라와야 한다

### Gripper Open Baseline

- `open 100%`
- final readback = `position=100`

### Closed Contact Baseline

- `close 0%`
- current branch calibration은 `closedRawPercent=0` 기준이다.
- 2026-04-29 field verify에서 operator가 실제 FR5 gripper 접촉/닫힘을 `0%`에서 확인했다.
- 같은 시점 movement summary embedded peripheral readback은 `position=5`, `positionFault=0`까지는 보였지만, `motionFault=1`, `done=0`이라 completion-grade confirmation으로 보지는 않는다.

### Visible Test Pattern

- `100 -> 70 -> 100`
- operator visual check succeeded on this pattern
- current calibration 기준 readback은 대략 `100 -> 88 -> 100`으로 읽혔다
- 2026-04-29 Unity `Easy Motion` apply + confirm 경로에서도 같은 패턴이 다시 먹었다

### Hold Pattern

- `50%` command
- final readback hold = `position=80`

## Current Operator Path

- `BtnConnect` 한 번으로 `연결 + 위치 읽기`
- gripper 조작 UI는 `Easy Motion`의 `통합 그리퍼 조작` surface 하나만 본다. legacy `IoPanelController` host는 비우고 숨긴 compatibility shim이다.
- `Easy Motion`에서 `열기 100 / 중간 50 / 닫기 0` quick button, slider, 또는 숫자 입력
- quick button과 숫자 입력은 모두 `draft` 값만 바꾼다
- `프리뷰`는 화면 프리뷰만 갱신하고 실기 write는 보내지 않는다
- `실제 적용`만 popup confirm과 `gripper-only` live write 경로를 탄다. gate가 막히면 버튼은 `잠김`이고, UI는 fault/readback/session blocker를 표시한다.
- 성공 후 자동 `readback-only` 복귀

## Current UI Separation Rule

- `열기 100 / 중간 50 / 닫기 0` quick button과 slider는 명시적 value selector다
- `프리뷰`와 `실제 적용`은 같은 버튼이 아니다
- mock / dryRun / disconnected / readback-only에서는 `실제 이동`이 green truth가 아니라 blocked reason 또는 preview-only 결과를 남기는 것이 정상이다
- 따라서 mock 검증에서는 `draft 유지`, `preview wording`, `blocked/live wording 분리`, legacy I/O host 비어 있음(`deprecated=True; mergedInto=EasyMotion`)을 본다

## Current Evidence Notes

- 2026-04-29 실기 smoke에서 `70% -> raw 88`, `100% -> raw 100`, `50% -> raw 80` 명령 송신은 확인됐다.
- 같은 날짜 calibration patch 뒤 `0% -> raw 0` command에서 operator physical contact가 확인됐고, movement summary embedded peripheral readback은 `position=5`, `positionFault=0`을 남겼다.
- current SDK readback은 여전히 `motionFault=1`, `done=0`, `positionFault=0` 조합으로 completion confirmation이 약하다.
- 따라서 discrete smoke 성공 판정은 `operator visual movement + commanded/raw state change` 기준으로 먼저 본다.
- completion-grade sensor confirmation은 아직 follow-up 과제다.
- 2026-05-14 IPC 안정화 이후 current branch 재검증에서는 arm tiny MoveJ와 분리한 `gripper-only` 세션을 다시 열었다. 연결은 `192.168.57.2:8080`로 됐지만 상태는 `ConnectedServoOff`, cached evidence는 `enabled=False`, `tool=00`, `user=00`였다.
- 같은 재검증에서 `GetGripperSdkSummaryForDebug(true)`는 SDK capability/profile을 읽었지만 readback은 `motionFault=-2`, `activationFault=-2`, `positionFault=-2`, `position=0`이었다.
- `EnableServoForDebug()`도 `RobotEnable(1) 실패 · code=-2 · connected=True · enabled=False · mode=0 · drag=False`로 반환했다. 따라서 이 세션에서는 gripper write를 보내지 않았고, current blocker는 `gripper command path`가 아니라 `servo/controller enable + gripper readback preflight`다.
- 같은 날 Unity를 재시작해 stale FR5 SDK sockets를 정리한 뒤에는 `8080` 포트가 다시 열렸고, reconnect는 `ReadyToJog`, `enabled=True`, `tool=01`, `user=01`, `activationFault=0`, `positionFault=0`까지 복구됐다. 이 상태에서 `70%`는 `[Live Gripper] 요청 70% 전송 완료 · readback 확인 안 됨 (motionFault=1; done=0; positionFault=0; raw=0)`으로 반환했지만, `100%` 복귀는 `그리퍼 동작 오류: gripper 통신, reset/activate 초기화, 장치 번호와 기능 설정을 확인하세요.`로 실패했다. 따라서 최신 current result는 `command sent / movement unconfirmed / return-to-open failed`다.
- 2026-05-15 same-day follow-up에서는 두 종류 truth가 같이 확인됐다.
  - successful reconnect/sync session:
    - `ConnectDefaultForDebug()` 직후 `Gripper: Cmd 100% / Actual 0%`
    - peripheral = `[Live Gripper] readback 0% 확인 (raw 0%)`
    - `GetEasyMotionGripperInputStateForDebug()` = `requested=100; actual=0; raw=0; readback=True`
  - later failed reconnect session:
    - `ping 192.168.57.2` success
    - `nc -vz 192.168.57.2 8080` timeout
    - `ConnectDefaultForDebug()` = `RPC 연결 실패 (code=-2)`
    - `GetEasyMotionGripperInputStateForDebug()` = `requested=100; actual=100; raw=100; readback=False`
  - current interpretation:
    - gripper UI/runtime resync logic itself is partially recovered
    - fresh live socket availability on `8080` is still a blocker for operator-stable startup
- 2026-05-19 fresh reconnect에서는 `192.168.57.2:8080`이 다시 정상이고, `SyncCurrentStateForDebug()`는 `[Sync] 현재 자세 동기화 완료`를 반환했다. 자동모드/DAHUAN 전환 전에는 `0%` close가 최종 SDK `position=96`에 남는 문제가 재현됐지만, 전환 후에는 같은 세션에서 아래처럼 회복됐다.
  - baseline readback: `Gripper: Cmd 0% / Actual 96%`, SDK `position=96`, `activationMask=1`
  - explicit `gripper-only` close: `SetGripperPositionForDebug(0)` 뒤 5초 후 SDK `position=0`, `done=1`; Easy Motion `requested=0; actual=0; raw=0; readback=True`
  - explicit `gripper-only` open: `SetGripperPositionForDebug(100)` 뒤 5초 후 SDK `position=100`, `done=1`; Easy Motion `requested=100; actual=100; raw=100; readback=True`
  - Easy Motion apply close: `SetEasyMotionGripperInputForDebug(0, true)` 뒤 5초 후 SDK `position=0`, `done=1`; UI state `requested=0; actual=0; readback=True`
  - Easy Motion apply open: `SetEasyMotionGripperInputForDebug(100, true)` 뒤 5초 후 SDK `position=100`, `done=1`; UI state `requested=100; actual=100; readback=True`
- 남은 UI 잔여 이슈는 green 판정과 별개로 추적한다. 명령 직후에는 여전히 `readback 확인 안 됨`이 먼저 표시되고 약 5초 뒤 SDK/UI가 수렴한다. Easy Motion debug state의 `pending=True`와 `lastApply`가 완료 후에도 오래 남는 케이스가 보였고, 이번 UI apply smoke는 이전 debug session approval이 살아 있어 popup-open path 자체는 새로 검증하지 못했다.

## Simplification Note

- 현재 제품 safety gate가 요구하는 본질은 `operator confirm 1회`다.
- `token`은 popup과 runtime 사이에서 그 1회를 1-shot으로 전달하는 현재 구현 디테일이다.
- `MoveJ/MoveL`에는 target mismatch 보호가 있으므로 approval token/target model 유지 가치가 크다.
- `MoveGripper`는 current target key가 `none`이라서, 장기적으로는 `UI에 토큰을 노출하지 않는 gripper 전용 1-shot confirm latch`로 단순화할 수 있다.
- 다음 단순화 우선순위는 `operator visible token 제거 -> popup confirm direct path 정리 -> debug helper 유지`다.
- 2026-04-29 현재 debug/field QA는 ad-hoc 버튼 조합 대신 공통 Live QA runner로도 기록할 수 있다. gripper smoke artifact는 `Artifacts/live/qa/*.json`에 `before/after movement + approval + latest-state/latest-drift + ndjson tail`까지 같이 남기는 쪽을 기준선으로 본다.

## Interpretation Rule

- `user 70%`는 현재 raw `70`을 뜻하지 않는다.
- current branch 기준 `user 0%`는 raw `0`을 뜻한다.
- current calibration 기준으로 `70% user`는 raw `88%` 근처다.
- 따라서 운영자 UI는 `user%` 기준으로 보고, debug/readback 비교 때만 raw를 같이 본다.

## Do Not Mix

- `gripper-only` 세션에서 `MoveJ`, `MoveL`, `IO`, `ToolDO` 금지
- `tiny-movej-only` 세션에서 `MoveGripper`, `SetGripperConfig`, `ActGripper` 금지
- same session에서 arm motion과 gripper write를 같이 열지 않는다
- product surface는 current branch에서 점진적으로 `live-control` 하나로 보이게 정리 중이지만, debug/smoke override는 아직 내부에 남아 있다.
- 따라서 `joint + gripper mixed live`를 user-facing green이라고 부르기 전에는 별도 same-session field smoke가 다시 필요하다.

## Stop Conditions

아래 중 하나면 즉시 추가 write를 멈춘다.

- `activationFault != 0`
- `positionFault != 0`
- `motionFault=-2` / `activationFault=-2` / `positionFault=-2`
- `RobotEnable(1)` 실패
- readback이 아예 갱신되지 않음
- operator visual movement와 readback이 심하게 어긋남
- live evidence freshness가 깨짐

## Next Target

다음 구현 목표는 `discrete smoke` 자체가 아니다.

순서는 고정한다.

1. `clientMode=direct-motion`을 field gate에서 정상 live client로 인정하도록 script/checklist를 맞춘다.
2. DAHUAN + auto mode preflight를 gripper live 시작 조건으로 고정한다.
3. 명령 직후 `readback 확인 안 됨`에서 약 5초 뒤 SDK/UI 수렴까지의 pending UX를 정리한다.
4. Easy Motion debug/UI state의 `pending=True`와 stale `lastApply`가 완료 후 남는 케이스를 정리한다.
5. 이전 debug session approval 없이 실제 popup-open/confirm path를 fresh로 다시 검증한다.
6. gripper confirm / debug flow 단순화
7. completion-grade readback 확인 범위 재정의
8. `Easy Motion` preview button / live button 분리 semantics를 실기에서 다시 검증
9. `Easy Motion` slider input throttling/commit policy 정리
10. slider 이동 중 live write cadence 고정
11. slider value와 readback value 차이 측정
12. 그다음에만 joint/tcp live slider 설계 검토
