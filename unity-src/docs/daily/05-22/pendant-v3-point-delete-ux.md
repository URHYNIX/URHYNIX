# 2026-05-22 - Pendant V3 Point Delete UX

## Request

- 저장된 티칭포인트 삭제와 그룹/묶음 삭제가 실제 UI 버튼에서 동작하지 않는 문제를 확인하고, 사용자 경험이 좋은 방향으로 패치한다.

## What Changed

- 포인트 리스트 행에 `BtnPointRowDelete`를 추가했다.
- 포인트 행 삭제는 해당 행의 이름을 직접 넘기므로, 사용자가 보는 행과 삭제 대상이 일치한다.
- 상단 `BtnPointDelete`는 현재 불러온 포인트를 우선 삭제 대상으로 해석한다.
- 위험 삭제 버튼은 첫 클릭 후 라벨이 `삭제 확인`, `선택 삭제 확인`, `전체 삭제 확인`으로 바뀌게 했다.
- 묶음 단일 삭제도 즉시 삭제에서 2단계 확인으로 바꿨다.
- `HomePoint1Loop` builder가 `Home`을 current readback에서 암묵적으로 다시 만들던 경로를 제거했다. 이제 `Home`도 사용자가 저장한 명시 포인트로만 남고, 삭제 후 sequence panel refresh가 일어나도 자동 복구되지 않는다.
- 저장 위치 리스트 행 높이를 줄이고, selected border의 기본 폭을 투명 border로 고정해 선택 하이라이트가 밀려 보이는 문제를 줄였다.
- 새 저장 항목이 카드 안에서 잘려 보이지 않도록 포인트 행 버튼과 선택 detail card를 compact layout으로 줄였고, detail card의 J/TCP 표기는 핵심 축만 보이도록 축약했다.
- `Point` 삭제 디버깅 결과, 버튼 2회 클릭 후 저장소에서는 삭제되지만 입력칸 `name=Point`가 그대로 남아 삭제되지 않은 것처럼 보이는 stale state가 있었다. 삭제 성공 후 남은 다음 포인트를 자동 recall하고, 남은 포인트가 없으면 입력칸을 비우도록 정리했다.
- Play Mode 경계에서는 `PendantV3Points.json`이 이미 없는 상태와 UI 메모리 상태가 어긋날 수 있으므로, 마지막 포인트 삭제 때 파일이 이미 없더라도 삭제 완료 상태로 처리하고 UI를 비우게 했다.

## Verification

- `dotnet build /Users/family/jason/FR5UNITY/robotapp/robotapp.slnx -nologo`
  - result: pass, errors `0`, warnings only
- `unityctl check --project /Users/family/jason/FR5UNITY/robotapp --type compile --json`
  - result: pass
- `RunTeachingSubviewActualClickMatrixForDebug()`
  - result: `23/36 PASS`
  - delete path pass: `point-row-delete-confirm-click`, `point-row-delete-second-click`, `point-bulk-delete-confirm-click`, `point-bulk-delete-second-click`
  - remaining failures: existing row locator / sequence / function candidate cases
- `DeletePointMoveForDebug("Home")` two-step delete + sequence panel refresh
  - result: pass
  - post-delete summary: `pointCount=0`, point list `points=[]`
- `RunTeachingSubviewActualClickMatrixForDebug()` after compact list/card patch
  - result: `25/36 PASS`
  - delete path remained pass
  - remaining failures: existing locator / sequence / function candidate cases
- `Point` actual delete repro
  - before: `points=[Point:MoveJ,home:MoveJ]`, `BtnPointDelete found=True enabled=True`
  - first click: `pendingConfirm=delete:Point`
  - second click: `points=[home:MoveJ]`, feedback `[Delete] Point 삭제`
- stale-input fix verification
  - created temp `home_COPY` from `home`
  - first delete click: `pendingConfirm=delete:home_COPY`
  - second delete click: `points=[home:MoveJ]`, active/name moved to `home`, feedback `[Delete] home_COPY 삭제`
- Play Mode storage check
  - `WaypointStore.GetStoragePath()`: `/Users/family/Library/Application Support/DefaultCompany/robotapp2/waypoints`
  - after point deletes: no `PendantV3Points.json`, runtime point summary `points=[]`
- `RunFunctionActualClickMatrixForDebug()`
  - result: `7/8 PASS`
  - delete path pass: `function-delete-confirm-click`, `function-delete-second-click`
  - remaining failure: existing `function-add-point-click` locator

## Notes

- 이번 변경은 FR5 live motion 범위를 넓히지 않는다.
- 저장 포인트/묶음 삭제의 actual UI click 연결성과 확인 UX를 개선한 작업이다.
- Unity screenshot capture는 scene/camera만 저장되고 overlay UI는 포함되지 않아, 이번 compact layout의 시각 검증 근거로 쓰지 않았다.
