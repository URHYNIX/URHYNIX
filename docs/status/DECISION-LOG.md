# Decision Log

## 2026-06-02

### Unity ControlRoom Phase 진행 전략 — 옵션 D (UI Polish First) + Phase 2.5 신설

- **결정**: Phase 3(데이터 모델/Registry) 진입 전에 **Phase 2.5 UI Visual Completion**을 신설해 UI를 contract로 먼저 100% 잠근다. 그 뒤 Phase 3~8은 UXML/USS/View 코드 0줄 수정 원칙으로 안만 채운다. fake interaction 깊이 = **알람 popup만** (시나리오 버튼 클릭 시 알람만 띄움, 센서 spike/로봇 dot animation 등은 안 함, Phase 3 이후 실 데이터로 자연 동작).
- **배경**: SSOT vs 현재 구현 cross-check 결과 View 14개 중 ✅4 / ⚠️3 / ❌7 — UI 자체가 절반 미완성. Map/Robot/Features/Sensors/Ros 5개 폴더는 통째 0%. 옵션 A(SSOT 순차)/B(Map 우선)/C(하이브리드)/D(UI Polish First) 4개 비교 후 D 채택. 이유: 기능 phase에서 UI를 또 바꾸면 두 번 작업 위험 → UI를 contract로 먼저 잠그면 꼬임 방지 + 시각 시연 가성비 최고.
- **Phase 2.5 산출물 (9 View 클래스 + UXML/USS 채움)**:
  - Scripts/UI/MovePanelView, ModePanelView, FeatureToggleListView(정적), WaypointListView, RobotTabView, PowerButtonView, HardwarePanelView, SensorCardListView, ProtectedTargetView
  - UXML 보강: 좌측 순회 지점 더미 5줄, 우측 하드웨어/센서 5종(PIR/화재 추가), MapPanel 격자+dot/waypoint placeholder, TopBar 전원 확인 모달
  - USS 보강: 격자 패턴, marker 스타일, 알람 popup polish
- **5단계 분해 (3~4일)**: ① 좌측 View 4개(0.5일) ② 상단바·우측 View 4개(0.5일) ③ 맵 placeholder 시각 완성(1일) ④ 카메라+로그 polish(0.5일) ⑤ 시나리오 알람 popup(1일).
- **검증 매트릭스 10건**: View 14 전부 ✅, 좌측 토글 시각 반응, 탭 전환 시 우측 갱신, 시나리오 4 알람, 맵 dot/waypoint/보호대상, 카메라 placeholder 자연스러움, 로그 5초 주기 push, 센서 5종, 전원 확인 모달, 30분 demo 녹화 시 "진짜 시연 같음".
- **핵심 학습**: 옵션 B/C(시각 가성비)는 기능 짜다 UI 또 만지는 두 번 작업 위험. 옵션 A(SSOT 순차)는 1주 작업해도 시각 변화 0. **UI를 contract로 먼저 잠그면 둘 다 잡힘**.
- **부수 산출물**: `docs/ref/UNITY-CONTROLROOM-CONVERSION-PLAN.md` §13에 Phase 2.5 신설 (Phase 2와 Phase 3 사이).
- **다음 진입**: Phase 2.5 단계 1 — 좌측 패널 View 4개 클래스 작성. 첫 파일: `Scripts/UI/MovePanelView.cs` (URHYNIX.ControlRoom.UI namespace, 순회 시작/정지 버튼 클릭 핸들러 + active 토글).

### 14:30 회의 Jira 최신 반영 — 티원/젠지, ROS_DOMAIN_ID 230, Teachable Machine 분류 스파이크

- **결정/확인**: 14:30 회의록 기준 로봇 별명과 역할을 다시 잠금. **젠지** = Pi Camera + 센서 탑재 로봇, **티원** = 비전 카메라 탑재 로봇. ROS 도메인은 `ROS_DOMAIN_ID=230`으로 통일한다.
- **검증 완료**: Unity에서 두 로봇 카메라 화면 동시 송출을 확인했다. 회의 근거는 Confluence `2026.06.02`(page `6127617`)와 `image-20260602-031954.png`.
- **AI 분류 스파이크**: Google Teachable Machine으로 TensorFlow/Keras 모델을 만들고, 1차 학습 클래스는 `빈공간`, `박스`, `마우스(검정/흰색)`, `손`으로 둔다. 목적은 로봇 카메라 영상에서 빠른 객체 분류 테스트다.
- **기존 비전 범위와의 관계**: 5/29 YOLO/OpenCV MVP 클래스(로봇/사람/중요품/불)는 발표 시나리오용 큰 범위로 유지한다. 6/2 Teachable Machine 클래스는 카메라 입력과 Keras 추론 경로를 빠르게 검증하는 별도 스파이크다.
- **Jira 반영 완료**: `SCRUM-44`, `SCRUM-51`, `SCRUM-54`, `SCRUM-56`, `SCRUM-61`, `SCRUM-62`, `SCRUM-64` 설명에 회의 최신 내용을 추가했다.
- **다음 검증**: 같은 도메인 230에서 `/tb3_1/...`와 `/tb3_2/...` 카메라 토픽이 동시에 보이는지, Keras 모델 추론 결과가 Unity 또는 테스트 로그에 클래스/신뢰도로 찍히는지 확인한다.

### Play UI 미표시 해결 — Scene 파일 m_PanelSettings 직접 GUID 패치 + unityctl screenshot 한계 확인

- **증상**: Play 진입 후 Game View가 Skybox+ground만 표시, UI Toolkit 6패널이 안 보임.
- **원인 1 (본질)**: Scene 파일 `ControlRoomMain.unity:439`의 `m_PanelSettings: {fileID: 0}` — Unity 6.3 LTS UIDocument.panelSettings setter가 직렬화 안 됨. `doc.panelSettings = panel` 호출 후 SaveScene 해도, SerializedObject.FindProperty("m_PanelSettings").objectReferenceValue 박아도 fileID 0으로 남음. sourceAsset(visualTreeAsset)은 정상 직렬화되어 대조됨. (Unity 6.3 버그 의심)
- **해결**: Scene 파일 직접 GUID 박음: `m_PanelSettings: {fileID: 11400000, guid: 22cd8904c7c224cd0a7d5e03ef3240ee, type: 2}` (PanelSettings asset의 mainObjectFileID + meta GUID 사용). Editor가 다음 reload에 그대로 인식.
- **이중 안전망**: `Assets/Scripts/UI/ControlRoomBinder.cs` Awake에 runtime fallback 추가 — `uiDoc.panelSettings == null`이면 `#if UNITY_EDITOR` LoadAssetAtPath로 박음. 다음 Setup 호출에서도 panel 누락되면 자동 복구.
- **원인 2 (부수)**: 본 fix 후에도 `unityctl screenshot capture --view game`이 검은 화면 또는 카메라만 캡처. `ScreenshotHandler.cs:83`이 `camera.Render()`만 호출 → UI Toolkit ScreenSpaceOverlay 미포함.
- **부수 해결**: `vendor/unityctl-plugin/.../ScreenshotHandler.cs:22` 패치 — view=game일 때 `includeOverlayUi` 기본 true. 그러나 `CaptureGameViewWithOverlay`의 `ScreenCapture.CaptureScreenshotAsTexture`가 sync 호출 timing 한계로 빈 frame 반환 (검은 화면). plugin level 본질 해결 어려움.
- **시각 검증 권장**: macOS native `screencapture -x` + `osascript "System Events" "Unity" set frontmost`로 캡처. Unity 활성화 + Game View 영역 캡처 → UI Toolkit overlay 포함 완벽 캡처 PASS. 박물관 시연 좌230(시나리오/모드) + 중앙(맵+카메라+로그) + 우240(배터리/센서) 6패널 모두 렌더링 확인.
- **핵심 학습 추가** (이전 entry 3건 + 2건):
  4. **UIDocument.panelSettings 직렬화 우회는 Scene YAML 직접 패치가 가장 확실**. setter도 SerializedObject도 fail 케이스 있음. GUID + mainObjectFileID(보통 `11400000`) 알면 직접 박기 가능.
  5. **`unityctl screenshot`은 UI Toolkit Overlay 캡처 신뢰 불가** — Native `screencapture` + Unity activate 우회 패턴 권장.
- **부수 산출물**:
  - `vendor/unityctl-plugin/src/Unityctl.Plugin/Editor/Commands/ScreenshotHandler.cs` 1줄 patch (URHYNIX 표시 주석 포함, upstream과 diverge — 다음 vendor 갱신 시 재적용 필요).
  - `Assets/Scripts/UI/ControlRoomBinder.cs:30-43` Awake fallback 블록 추가.
  - `Assets/Scenes/ControlRoomMain.unity:439` m_PanelSettings GUID 박음.
- **다음 진입**: Editor Game View에서 시각 검증 7체크리스트(시계 / 시나리오 버튼 클릭 → 로그+Alert / 2D 3D 토글 / 배터리 변동 / 센서 카드 갱신 / 로그 자동 스크롤 / AlertPopup 모달) 직접 수행.

### unityctl 통합 테스트 10/10 PASS — 5 FAIL 전부 해소 + plugin vendor 영구화 + 핵심 학습 3건

- **결정**: 이전 entry의 5 FAIL(`scene open` 500 / `screenshot` 502 / `exec` 502 / `test` 504 / 휘발성 경로) 모두 해소. unityctl 자동화 baseline → **풀 자동화 가능** 단계 진입.
- **해소 절차** (재현 가능):
  1. **Plugin source 영구화**: `rsync -a --exclude='.git' /tmp/unityctl-repo/ vendor/unityctl-plugin/` (4.3MB, 383 파일). `manifest.json` 경로 변경: `file:/tmp/unityctl-repo/...` → `file:../../../vendor/unityctl-plugin/src/Unityctl.Plugin` (Packages/ 기준 상대경로).
  2. **SceneSetup Camera 추가**: `Assets/Editor/ControlRoomSceneSetup.cs:38` `NewSceneSetup.EmptyScene` → `NewSceneSetup.DefaultGameObjects` 1줄 패치. Scene rootCount 5 → 7 (Main Camera + Directional Light 자동 포함).
  3. **Editor 재시작**: SIGTERM 60258 → nohup 신규 시동 → `osascript activate` (포커스가 IPC Bootstrap 트리거 조건).
  4. **Scene 재생성**: `unityctl exec ExecuteMenuItem("URHYNIX/Setup ControlRoom Scene")` (1차 Play 모드라 NewScene InvalidOperationException → `play stop` 후 2차 PASS).
- **10/10 결과 매트릭스**:
  | Step | 명령 | 결과 |
  |---|---|---|
  | 1 | `doctor` (IPC probe) | ✅ pipe `unityctl_4147f01858f6edf8` 활성 |
  | 2 | `check --type compile` | ✅ 31 assemblies, 0 error |
  | 3 | `scene hierarchy` | ✅ rootCount 7 (MainCamera + DirLight + 5 app GO) |
  | 4 | `screenshot capture --view game` | ✅ 1920×1080 PNG 270KB → `/tmp/controlroom-game-view.png` |
  | 5 | `screenshot capture --view scene` | ✅ 102KB → `/tmp/controlroom-scene-view.png` |
  | 6 | `play start` | ✅ isPlaying=true |
  | 7 | `exec RaiseScenarioTriggered("fire")` ×4 | ✅ 4 시나리오 (fire/intruder/noise/theft) 전부 success=true |
  | 8 | `screenshot capture` (post-scenario) | ✅ Alert popup 포함 화면 캡처 |
  | 9 | `console get-count` | ✅ 5 logs / 7 warnings / 0 errors |
  | 10 | `play stop` + `test --mode edit` | ✅ 3 passed, 0 failed (2.4s) |
- **핵심 학습 3건** (다음 세션 진입 캡슐):
  1. **Editor focus가 IPC Bootstrap 활성화 필수 조건**. `nohup Unity -projectPath ...` 만으로는 IPC pipe 안 뜸. 반드시 `osascript -e 'tell application "Unity" to activate'` 또는 사용자가 직접 Editor 클릭. doctor 메시지 "Editor not focused"가 진짜 단서.
  2. **`exec --code` void method 호출 OK** — 이전 entry의 "void method 거부" 노트는 부정확. `URHYNIX.ControlRoom.App.ControlRoomEvents.RaiseScenarioTriggered("fire")`처럼 직접 호출 가능 (result: null 반환). Roslyn evaluator가 expression-statement 허용.
  3. **`unityctl` plain 출력 Spectre 'Busy' style 버그** — Editor Busy 상태(Play 진입 직후 등)에서 plain CLI 출력이 `StyleParser` exception. **`--json` 옵션 권장** (모든 명령 지원). 그러면 안정적.
- **부수 사실**:
  - Scene 재생성 시 Play 모드면 `EditorSceneManager.NewScene` InvalidOperationException. **반드시 `play stop` 선행**.
  - `vendor/unityctl-plugin/` git 추적 대상 (5MB, `vendor/CLAUDE.md`에 갱신 절차 박음). `.git` 제외해서 read-only 스냅샷 보관.
  - Plugin 컴파일 warning ~30개 (Unity 6.3에서 deprecated API). 동작 무관, 0.2.0 → 신규 버전 시 정리 가능.
- **다음 진입**: 풀 자동화 가능. Phase 3(데이터 모델/Registry 확장) 또는 박물관 시연 7체크리스트 GUI 1차 검증으로 진행.

### unityctl IPC 자동화 도구 도입 + 통합 테스트 10단계 부분 PASS (5/10 PASS, 5 후속 보강)

- **결정**: Unity Editor를 IPC로 원격 조작하는 `unityctl` 0.2.0(`Jason-hub-star/unityctl`, dotnet tool)을 자동 테스트 워크플로의 표준 도구로 채택. ControlRoom 프로젝트에 brigde plugin 설치.
- **설치 절차** (재현 가능):
  1. `dotnet tool install -g unityctl unityctl-mcp` (이미 설치됨)
  2. `git clone --depth 1 https://github.com/Jason-hub-star/unityctl.git /tmp/unityctl-repo`
  3. `unityctl init --project unity/ControlRoom --source /tmp/unityctl-repo/src/Unityctl.Plugin` → `manifest.json`에 `com.unityctl.bridge`(file: 경로) 박힘
  4. `ProjectSettings/UnityctlSettings.asset` 박음: `{"Enabled": true, "InstallSourceKind": "local", "InstalledVersion": "0.2.0"}` — **plugin Bootstrap이 이 파일 + Enabled=true 확인 후에만 IPC server 시작**
  5. Editor 재시작 → `Library/ScriptAssemblies/UnityctlBridge.dll` 생성 + IPC named pipe(`unityctl_4147f01858f6edf8`) 활성
- **10단계 시퀀스 결과** (`/tmp/urhynix-test-suite.sh`):
  | Step | 명령 | 결과 |
  |---|---|---|
  | 1 | `ping` | ✅ PASS (즉시 통과) |
  | 2 | `check --type compile` | ✅ PASS — 31 assemblies, scriptCompilationFailed: false |
  | 3 | `test --mode edit` | ❌ FAIL (504, "Test run failed before execution") |
  | 4 | `scene open ControlRoomMain.unity` | ❌ FAIL (500, log에 scene-open error 기록) |
  | 5 | `play start` | ✅ PASS ("Already in play mode") |
  | 6 | `screenshot capture --view game` | ❌ FAIL (502, "No camera found in the scene") |
  | 7 | `exec --code "RaiseScenarioTriggered(\"fire\");"` × 4 | ❌ FAIL (502, syntax — void method statement 미허용) |
  | 8 | `screenshot capture --view game` | ❌ FAIL (502, 동일) |
  | 9 | `log --last 100` | ✅ PASS — 100 entry 수집 |
  | 10 | `play stop` | ✅ PASS — isPlaying: false |
- **밝혀진 문제 4건과 해결 방향**:
  1. **Scene에 Camera 없음** — `Assets/Editor/ControlRoomSceneSetup.cs`가 `NewSceneSetup.EmptyScene`으로 생성해서 MainCamera + Directional Light 자동 추가 안 됨. UI Toolkit만 쓰면 카메라 불필요하지만 Game View 캡처에 필수. 후속: `NewSceneSetup.DefaultGameObjects` 모드로 바꾸거나 `gameobject create Camera` 추가.
  2. **`exec --code` syntax 제약** — expression 평가 모드라 `Type.Method(args);` statement 거부. 안내 메시지: "For structured calls, prefer `exec invoke`". 그러나 `unityctl --help`에 `exec invoke` 미노출 (0.2.0). 후속: `batch execute --file <C#>` 또는 PlayMode test로 시나리오 트리거 자동화.
  3. **`scene open` 500** — 자세 에러 메시지 없음. ControlRoomMain.unity 자체 OK여도 Editor 상태 또는 plugin 호환성 문제 가능. 후속: 재현 + Editor.log 상세 디버그.
  4. **`test --mode edit` 504** — Test Runner 시작 단계 에러. NUnit asmdef 호환성 또는 unityctl test 호출 방식. 후속: smoke test EditMode 직접 Test Runner 윈도우로 검증.
- **PASS 확보된 baseline**: 컴파일 31 assembly 검증 + Play 모드 진입/종료 + IPC log 100 entry 수집. 즉 Unity Editor를 CLI에서 무대조작 가능한 기초 라인은 살아있음.
- **부수 산출물**:
  - `Assets/Tests/EditMode/URHYNIX.ControlRoom.Tests.EditMode.asmdef` + `SmokeTests.cs` (NUnit Math/String/Float 3 테스트)
  - `Packages/manifest.json` 변경: `"com.unityctl.bridge": "file:/tmp/unityctl-repo/src/Unityctl.Plugin"` 추가 — **주의**: `/tmp/` 휘발성, macOS reboot 시 plugin source 손실 risk. 영구 위치 (`~/.unityctl/plugin/` 또는 vendor commit)로 이전 필요.
  - `/tmp/urhynix-test-suite.sh` 시퀀스 script (재실행 가능)
  - `/tmp/urhynix-test-suite/*.json` 10개 결과 파일

### Unity ControlRoom UI Toolkit skeleton PASS (Phase 2 완료, 19 산출물, batch 컴파일+Scene 생성 검증)

- **결정**: HTML 관제(`robot_control_system.html`) → Unity UI Toolkit 전환의 첫 phase(UI skeleton + fake data) 완료. 박물관 시연 6패널이 Play 모드에서 살아 움직임.
- **레이아웃 A 채택**: 상단바 + 좌 230px(시나리오/모드/순회/특수) + 중앙flex(맵 + 카메라+로그) + 우 240px(배터리/센서/하드웨어). HTML 직접 이식 톤.
- **색상 톤**: 박물관 고품격 밝은 슬레이트 (`#f1f5f9` bg / `#1e293b` text / `#2563eb` accent / `#10b981` ok / `#dc2626` danger / `#f59e0b` warn). USS 변수 10개 SSOT(`ControlRoomTokens.uss`) + C# 상수 1:1(`Design/UiTokens.cs`).
- **만든 파일 19개**:
  - App 3: `ControlRoomApp.cs`, `ControlRoomState.cs`, `ControlRoomEvents.cs`
  - Design 2: `UiTokens.cs`, `IconNames.cs`
  - Data 1: `RobotInfo.cs`
  - Simulation 2: `FakeSensorData.cs`(Perlin/Sin 1.5Hz), `DemoScenarioService.cs`(4 시나리오 트리거)
  - UI Markup 8: `ControlRoomMain.uxml` + Style/Tokens.uss + Parts 5개(TopBar/LeftControl/Map/CameraAndLog/RightStatus)
  - UI Views 7: `ControlRoomBinder.cs` + 7 View(TopBar/Scenario/Map/Camera/Log/Telemetry/AlertPopup)
  - Editor 1: `ControlRoomSceneSetup.cs` (batch Scene 자동 조립)
  - Config 1: `Resources/RobotConfig/default_robots.json` (티원/젠지 메타)
  - Scene 1: `Scenes/ControlRoomMain.unity` + `UI/ControlRoomPanelSettings.asset` (1920×1080 reference)
  - 추가 stub: `Database/SupabaseClient.cs` (PoseLogRepository 의존 해소, Phase 7에서 실 구현)
- **검증**: Unity 6000.3.16f1 batch 2회 (1차 컴파일 에러 2건 → Binder.camera 변수명 + SupabaseClient stub → 2차 PASS). `Assembly-CSharp.dll` 컴파일 PASS, error CS 0건, Scene 저장 PASS, `Exiting batchmode successfully` exit 0.
- **2D/3D 토글 처리**: `MapPanel.uxml`에 2D/3D 버튼 활성, 3D 클릭 시 placeholder + "Phase 6 예정 (URDF Importer)" 라벨. ControlRoomState.MapViewMode 상태 + OnMapViewModeChanged 이벤트.
- **fake data 흐름**: FakeSensorData 1.5Hz tick → ControlRoomEvents 발화 → TelemetryPanelView가 현재 선택 로봇 값만 표시. 배터리 87% ±3% Perlin, 가스/소음 sin, 조도 sin.
- **시나리오 흐름**: ScenarioPanelView 버튼 클릭 → ControlRoomEvents.RaiseScenarioTriggered → DemoScenarioService 수신 → 로그 + 위험 경보 발화 → AlertPopupView 모달 + TopBarView 경보 카운트 증가.
- **다음 진입**: Unity Hub에서 `unity/ControlRoom` Open → `Assets/Scenes/ControlRoomMain.unity` 더블클릭 → Play 모드 → 7 체크리스트 검증 (TopBar 시계, 시나리오 버튼 클릭, 2D/3D 토글, 배터리 변동, 센서 카드 갱신, 로그 자동 스크롤, AlertPopup 모달).
- **남은 단계 (다음 phase)**: Phase 3 데이터 모델/Registry 확장(default_features/sensors/office_base_map.json) → Phase 5 ROS 실제 연결(`CameraPanelView` unity-smoke 재이식) → Phase 6 URDF 3D → Phase 7 Supabase 실 통합.

### `pose_logs` 테이블 Supabase 적용 완료 (CLI `db query --linked`)

- **결정**: `scripts/sql/pose_logs.sql`을 Supabase 프로젝트 `ueupkrxwybuuqxflstvg`에 적용.
- **적용 명령**: `supabase link --project-ref ueupkrxwybuuqxflstvg` → `supabase db query --linked --file scripts/sql/pose_logs.sql --agent=yes` (CLI v2.84.2, env에 박힌 `SUPABASE_ACCESS_TOKEN` + `SUPABASE_PROJECT_REF` 사용).
- **검증** (모두 PASS):
  - 컬럼 9개 — id/session_id/robot_id/ts/x/y/theta/source_topic/nav_mode, NOT NULL/NULL 정합.
  - 인덱스 3개 — `pose_logs_pkey`, `idx_pose_logs_session_robot`, `idx_pose_logs_mode`.
  - RLS 정책 2개 — `anon_insert_pose` (INSERT, anon), `anon_select_pose` (SELECT, anon). UPDATE/DELETE 기본 거부 유지.
  - `select count(*) from public.pose_logs` → 0 rows (정상 빈 상태).
- **부수 효과**: `supabase/.temp/` CLI 캐시 폴더 생성 → 루트 `.gitignore`에 `supabase/.temp/`, `supabase/.branches/` 추가.
- **SCHEMA.md 갱신**: "Current Applied Entities" 섹션에 적용 사실 박음. "Planned Extensions" 표는 참고용으로 유지(컬럼 동일, ✅ 마킹).
- **scripts/sql/CLAUDE.md 갱신**: `pose_logs.sql` 상태 `미실행` → `✅ 2026-06-02 적용 완료`.
- **다음 진입**: 로봇 PC(`scripts/pose_logger.py`)에 `URHYNIX_ROBOT_ID` + `URHYNIX_SESSION_ID` + `SUPABASE_ANON_KEY` env 박고 systemd로 띄우면 실기기 좌표 자동 INSERT 시작.

### 로봇 현재 위치 저장 기능 부품 박음 (`pose_logs` 첫 부품 + Unity/Python/SQL 3측)

- **결정**: SSOT 핵심 목표 "이동 좌표·사진·영상·사운드와 모든 결과가 DB에 기록된다" 중 **이동 좌표** 부품을 폴더 구조에 반영. SCHEMA.md의 SCRUM-23 Planned Extension `pose_logs`를 실 코드 진입점까지 연결.
- **저장 경로**: 로봇 PC가 주 쓰기, Unity는 read 우선. service_role 키 절대 미반입(anon + RLS).
- **추가된 파일**:
  - `unity/ControlRoom/Assets/Scripts/Data/RobotPoseEntry.cs` — pose 1행 POCO, SCHEMA.md 컬럼 1:1.
  - `unity/ControlRoom/Assets/Scripts/Database/PoseLogRepository.cs` — read 우선 + 보조 INSERT 골격(Phase 7 구현).
  - `scripts/pose_logger.py` — 로봇 PC가 `/tb3_*/pose` 구독 → Supabase `pose_logs` INSERT (ROS2 + supabase-py + UTC ISO ts + quaternion→yaw 변환).
  - `scripts/sql/pose_logs.sql` — 테이블 + 인덱스 2종 + RLS 정책 3종(anon INSERT/SELECT, UPDATE/DELETE 기본 거부) migration.
  - `scripts/sql/CLAUDE.md` — migration SQL 운영 규칙 (실행 경로 3종, 검증 흐름, 명명 규칙, 보안).
- **CLAUDE.md 갱신**: `Assets/Scripts/Data/`, `Assets/Scripts/Database/` 두 곳 예정 파일 표에 ✅ 마킹.
- **다음 단계 (적용 전 결정)**:
  - `pose_logs.sql`을 실제 Supabase에 실행할 시점 — 시연 직전 또는 Phase 7 진입 시.
  - 적용 후 `SCHEMA.md` "Planned Extensions" → "Current Applied" 이전.
  - 환경변수 `URHYNIX_ROBOT_ID`, `URHYNIX_SESSION_ID`, `SUPABASE_ANON_KEY`를 `/etc/urhynix.env`에 박는 절차.

### Unity ControlRoom 첫 batch import PASS (Library + .meta 자동 생성)

- **결정**: `unity/ControlRoom/` 프로젝트가 Unity 6000.3.16f1로 첫 batch import 성공. Unity Hub의 Add Project 절차 없이 바로 Open 가능 상태.
- **검증**: `Unity.app -batchmode -quit -nographics -projectPath unity/ControlRoom -logFile /tmp/unity-controlroom-first-open.log` exit code 0 + `Exiting batchmode successfully now!`. License 채널 정상 활성, 어셈블리 에러 0건.
- **산출물**:
  - `Library/` 12개 하위 생성 (BuildInstructions/PackageCache/ScriptAssemblies/ShaderCache/Bee 등)
  - `Assets/**/.meta` 83개 자동 생성 (CLAUDE.md.meta 24개, PNG.meta 26개 포함)
  - `ProjectSettings/ProjectVersion.txt`에 Unity revision hash `a56f230f6470` 자동 박힘
  - Tests 폴더 추가: `Assets/Tests/{EditMode, PlayMode}/` + `CLAUDE.md` (Opus 자기리뷰 caveat #1 해결)
- **무시한 warning**: `Access token is unavailable` (Unity Cloud Analytics 미인증, Personal 사용에 무관), `Curl error 42` (Telemetry 호출 중단, 무관), License 첫 채널 handshake 실패 후 재시도로 success (정상 패턴).
- **다음 진입**: Unity Hub에서 `unity/ControlRoom` Open (Add Project 불필요 — 이미 등록됨). 첫 Open 시 Library 재생성 없이 즉시 열림.

### Unity ControlRoom 신규 프로젝트 분리 + Unity 6.3 LTS (6000.3.16f1) 채택

- **결정**: HTML 관제(`robot_control_system.html` 2727줄)를 Unity C# 관제로 전환하기 위해 **`unity/ControlRoom/`** 신규 Unity 프로젝트를 만들고 **Unity 6.3 LTS (6000.3.16f1)**를 사용한다.
- **버전 선택 근거**:
  - Unity 6.3 LTS는 2025-12 출시, **2027-12까지 지원** (Unity 6.0 LTS는 2026-10 EOL).
  - 박물관 시연 이후에도 장기 안정. unity-smoke(6000.0.64f1)는 카메라 검증용 자료실로 보존.
  - URDF Importer는 Unity 6 계열에서 호환성 미검증이라 Phase 6 진입 전 별도 smoke 필요 — fallback: community fork(gkjohnson urdf-loaders) 또는 사전 변환된 prefab.
- **폴더 구조**: `URHYNIX/unity/ControlRoom/` (Unity 프로젝트 루트). 기존 `unity-smoke/`(카메라 검증 PASS, 자료실로 보존), `unity-src/`(PNG 시트만 채워진 빈 껍데기, Art는 신규로 이관)는 그대로 둔다.
- **scaffold 완료**: ProjectSettings = unity-smoke 복사, manifest.json = ROS-TCP-Connector v0.7.0 + Universal RP 17.0.4 + UI Toolkit (`com.unity.modules.uielements`) 베이스, ProjectVersion.txt = `6000.3.16f1`, .gitignore = Unity 표준 + Supabase 키 차단, PNG 26개 이관(`Assets/Art/IconsPng/`).
- **다음 진입**: 주인님이 Unity Hub에서 **6000.3.16f1 설치 → Add Project → `unity/ControlRoom` 선택** → 첫 Open 시 Library/ 자동 재생성(5~10분) → URDF Importer smoke 1건 결정.

### Supabase 연동 URL + write path 정책 확정

- **결정**: Unity ControlRoom의 Supabase 진입점을 **`https://ueupkrxwybuuqxflstvg.supabase.co`** 로 박는다.
- **write path 정책 (시뮬은 최대한 실기기 기반)**:
  - 로봇 PC(젠지/티원 Python ROS2 노드) = **주 쓰기 주체**. anon key + RLS 정책으로 events/dispatches/pose_logs INSERT.
  - Unity ControlRoom = **read + 제한 INSERT만**. `dispatches`(출동 명령), `session_meta` 등 사람 액션만 쓰기. service_role 키 **절대 미반입**.
  - 민감 작업(전원 종료, RLS 우회) = Supabase **Edge Function** 호출만.
- **키 보관**: anon key는 `Assets/Resources/SupabaseConfig.local.asset` (`.gitignore` 차단). template 파일 `SupabaseConfig.template.asset`만 커밋.
- **SDK**: `supabase-csharp` + `kamyker/supabase-unity` git URL (또는 NuGetForUnity), UniTask 필수.
- **dual naming**: DB `robot_id`는 `tb3_1`/`tb3_2` 그대로. 사람 UI 표기는 티원/젠지 별명.

### ROS_DOMAIN_ID 230 통일 (티원 기준에 젠지 맞춤)

- **결정**: 두 로봇의 `ROS_DOMAIN_ID`를 **230**으로 통일한다.
- **배경**: 티원(`t1@192.168.0.250`)이 이미 230으로 작동 중. 젠지(`urhynix-robot`)는 신규 SD 부트스트랩 시 30으로 초기화돼있어 같은 도메인이 아니면 두 로봇 토픽이 서로 안 보임 → 박물관 시연 시 dispatcher/협업 불가.
- **드리프트 발견**: SSOT에서 56(정본 설계 CONTRACT.md), 30(신규 SD HANDOFF/evidence), 230(티원 실제) 3개 값이 섞여있었음. 230으로 일괄 통일.
- **변경 작업**:
  - 젠지 `~/.bashrc`: `export ROS_DOMAIN_ID=30` → `export ROS_DOMAIN_ID=230` (`sed` 1줄)
  - 검증: `ssh urhynix-robot 'source ~/.bashrc && echo $ROS_DOMAIN_ID'` → `230` ✅
  - 백업 파일: `~/.bashrc.bak-YYYYMMDD-HHMMSS` 자동 생성
- **SSOT 정정 (5파일)**:
  - `docs/ref/CONTRACT.md` 정본 설계 (56 → 230) + 2026-06-02 정정 사유 박음
  - `docs/status/HANDOFF.md` 환경 자동 source 라인 (30 → 230)
  - `docs/status/DECISION-LOG.md` 신규 SD 부트스트랩 결정 본문 (30 → 230)
  - `docs/evidence/2026-06-01-new-sd-128gb-ros2-jazzy-bootstrap.md` 부트스트랩 표 (30 → 230)
  - `docs/instructor-report/index.html` 발표 자료 (56 → 230)
- **건드리지 않음**: 이전 evidence (`2026-05-27-live-turtlebot...`, `2026-05-29-mac-docker-slam...`, `maps/desk_static_v1/eval.md`) — 16GB SD 시점 역사 기록. `MAC-DOCKER-ROS2-PLAYBOOK.md` 5곳 — Mac Docker 트랙은 5/29에 부적합 결론났으므로 deprecated, 별도 정리 시 일괄 변경.
- **230 안전 범위 caveat**: ROS2 공식 0~232 안에 있지만 Linux 권장 0~101 (multicast port 7400 + 250*domain_id가 system reserved 영역과 가까움). 동료가 이미 230으로 작동 검증했고 codelab_5G WiFi에서 충돌 없으니 박물관 시연 한정으로 사용. 후속 프로젝트는 0~101 권장.
- **다음 검증** (다음 세션 또는 두 로봇 동시 켤 때):
  - 양쪽 `ros2 topic list`에서 `/tb3_1/...` + `/tb3_2/...` 모두 보이는지
  - Unity ROS-TCP-Endpoint도 같은 도메인 환경 확인 (TCP 자체는 도메인 무관이지만 robot 내부 ros_tcp_endpoint 노드는 도메인 영향 받음)

### 로봇 작명 + 호스트 매핑 확정 (티원 / 젠지)

- **결정**: 두 로봇에 e스포츠 팀 별명을 부여한다. ROS namespace는 `tb3_1`/`tb3_2` 그대로 유지하고, 사람 문서/UI/회의록에서 별명을 사용한다 (dual naming).
- **매핑**:
  - **tb3_1 = 티원** (비전 중심) — 카메라: **Intel RealSense D435** (3층 정면 부착) — 호스트: **`t1@192.168.0.250`** (hostname `rb`) — 사용자명 `t1` = 티원 일치
  - **tb3_2 = 젠지** (센서 중심) — 카메라: **Raspberry Pi Camera Module v2 (Sony IMX219, 8MP)** + Arduino 4종 (PIR/LDR/소리/불꽃) — 호스트: **`urhynix-robot`** (kim@192.168.0.82)
- **근거**: 2026-06-01 회의(Confluence 5111810)에서 "로봇1=비전, 로봇2=센서" 분담 결정. 회의 결정 + 사용자명 + 작업 영역 매핑이 모두 일치(D435는 t1@.250에서, IMX219는 urhynix-robot에서 검증됨).
- **dual naming 원칙**:
  - ROS topic: `/tb3_1/...`, `/tb3_2/...` 영문 + 표준 (한글 unicode 토픽 미사용)
  - DB `robot_id`: `tb3_1`, `tb3_2`
  - 사람 문서/Unity UI/회의록/PR 제목: **티원** / **젠지** 별명 사용 권장
  - 양쪽 표기 모두 SSOT에 명시
- **SSOT 반영 위치**: `docs/ref/ARCHITECTURE.md` (듀얼 로봇 역할 + 외부 시스템 표), `docs/ref/PROJECT-PLAN.md` (2대 로봇 역할 분리), `docs/status/PROJECT-STATUS.md` (한 줄 상태), `docs/status/HANDOFF.md` (Last updated).
- **잔여 액션**:
  - JIRA-MAP의 SCRUM-19/25 본문에 카메라 매핑 정확화 (`/tb3_1/camera/*` D435 vs `/tb3_2/camera/*` IMX219)
  - Unity 패널의 로봇 전환 토글 라벨을 "티원/젠지"로
  - 다음 회의에서 카메라 부착 이전 일정 확정 (D435가 어제 임시 검증 머신 = t1@.250 그대로 영구라면 이전 불필요. 다른 머신이면 이전 일정 잡기)
- **영향 없음**: ROS namespace `tb3_1`/`tb3_2`는 그대로라 코드/Unity/DB 변경 없음. 사람 표기만 별명 추가.

## 2026-06-01

### RealSense D435 Windows streaming PASS (pyrealsense2) 추가 확인

- **검증 결과**: Windows workstation에서 RealSense D435가 OS 장치 인식뿐 아니라 `pyrealsense2` RGB-D streaming pipeline까지 PASS. RGB/Depth 장치가 Windows PnP에서 `OK`로 보였고, Python pipeline에서 depth/color `640x480` frame 수신을 확인했다.
- **장치 정보**: `Intel RealSense D435`, Serial `254522075185`, Product ID `0B07`, Firmware `5.17.0.10`.
- **프레임 결과**: `depth 640x480`, `color 640x480`, center depth sample `0.159 m`.
- **해석**: 기존 Mac evidence의 streaming BLOCKED 결론은 macOS Tahoe + Homebrew librealsense 조합에 한정한다. 카메라 하드웨어 자체와 Windows SDK 경로는 정상이다.
- **프로젝트 결정 유지**: 실제 로봇/ROS2 통합은 Pi4 + `realsense2_camera` 경로를 계속 우선한다. Windows는 빠른 RGB-D bench test host로 사용할 수 있다.
- **근거 evidence**: `docs/evidence/2026-06-01-realsense-d435-windows-pyrealsense2-smoke.md`

### Pi Camera 모델 확정 (Module v2 / Sony IMX219) + Ubuntu 24.04 ports repo 미제공 → 소스 빌드 결정

- 변경 목적: 2026-06-01 SCRUM 회의록(Confluence page `5111810`)의 역할 분담/부착 계획을 SSOT에 명시한다.
- **모델 확정**: 신규 128GB SD 부트스트랩 후 첫 진단에서 Raspberry Pi Camera Module v2 (Sony IMX219, 8MP, 3280×2464 최대 해상도) 확정. 근거: `lsmod`에 `imx219` 로드, `i2c-10` address `0x10` 응답, `/dev/video0` = `unicam-image` (CSI MMIO `fe801000.csi`).
- **하드웨어 상태**: 100% 정상 (CSI controller + `bcm2835_unicam` + `bcm2835_isp` + `bcm2835_codec` 전부 로드). 케이블/sensor 응답 정상.
- **차단 원인**: Ubuntu 24.04 LTS for Raspberry Pi ports repo는 `rpicam-apps`/`libcamera-apps` **미제공**. `apt install` 시 "패키지를 찾을 수 없습니다". Ubuntu는 upstream libcamera만 포함하고 Pi ISP/IPA는 Raspberry Pi fork에만 존재.
- **결정**: libcamera Pi fork(`github.com/raspberrypi/libcamera`) + rpicam-apps(`github.com/raspberrypi/rpicam-apps`) **소스 빌드 진행** (30~60분, Pi4 풀로드). 한 번 빌드 후 영구 사용. W2 진입 전 박물관 시연 풀 기능 확보.
- **HANDOFF 잔여 액션 #4 갱신**: 기존 "Pi 카메라 동작 검증 3분"에서 "Module v2 (IMX219) user-space 풀 빌드 30~60분"으로 정정. 검증 자체는 이미 통과 (하드웨어 정상).
- **외부 근거**:
  - [rpicam-apps#388 — Libcamera-apps not available for Ubuntu](https://github.com/raspberrypi/rpicam-apps/issues/388)
  - [Sepideh Shamsizadeh — IMX219 on Ubuntu 24.04 LTS 가이드](https://medium.com/@sepideh.92sh/setup-and-troubleshooting-of-raspberry-pi-camera-module-v2-1-imx219-on-ubuntu-24-04-lts-fb518f4576c0)
  - [Hackaday — Bringing Up IMX219 on Pi 5 with Ubuntu 24.04](https://hackaday.io/project/203704-gesturebot/log/242459)
- **D435와의 역할 분담**: IMX219 = 일반 RGB 영상(라이브 스트림 + YOLO 4종 인식 + 녹화 MP4/rosbag). D435 = Depth + RGB(3D 매핑, 가벽 detection). 박물관 시연에서 **두 카메라 동시 사용**.
- **빌드 완료 (16:36)**: libcamera Pi fork v0.7.1+rpt20260429 + rpicam-apps v1.12.0 6분만에 빌드 PASS. capabilities `egl:1 qt:1 drm:1 libav:0`. 캡처 검증도 통과: `rpicam-still` 1920×1080 JPG 283KB + `rpicam-vid` 1280×720@30Hz × 5초 H.264 2.9MB.
- **잡은 함정 3건** (Ubuntu 24.04 특이): ① ports repo에 rpicam-apps 없음 → 소스 빌드. ② `libepoxy-dev` deps 누락 (preview/meson.build:32) → apt 추가. ③ libavcodec 60.31.x가 rpicam-apps master 요구 API보다 오래됨 → `-Denable_libav=disabled` 우회 (mp4 인코딩은 별도 ffmpeg).
- **재사용 가능 스크립트**: `scripts/build-picamera.sh` — sudo keeper 50s + setsid + 함정 3건 모두 반영. 다음 SD 또는 협업자 머신에서 한 줄(`nohup bash build-picamera.sh > picam-build.log 2>&1 &`)로 30~60분 안에 동일 빌드 가능.
- **근거 evidence**: `docs/evidence/2026-06-01-rpi-camera-imx219-source-build.md` (산출물: JPG 283KB + H.264 2.8MB 동봉)

### 로봇 1/2 역할 분리 + 카메라/센서 부착 계획 (회의록 기반, 계획/진행 중)

- 변경 목적: 2026-06-01 회의록에서 합의된 2대 로봇 역할 분리를 SSOT에 고정한다.
- **운영 모델(초안)**:
  - `tb3_1`(로봇 1) = **비전 중심**: RealSense `D435`를 전면(3층 정면) 부착해 RGB-D 기반 인식/매핑을 담당.
  - `tb3_2`(로봇 2) = **센서/확인 중심**: Arduino 센서 스택 + Pi Camera(IMX219) 부착로 이벤트/확인(영상) 담당.
- **상태**: 부착은 “예정/진행 중”. 실제 장착 완료/ROS 토픽 레벨 검증은 evidence로 별도 업데이트 필요.

### Unity 관제 UI v1 상호작용/버튼 요구사항 (회의록 기반, 계획/정의 중)

- 변경 목적: 2026-06-01 회의록의 UI 기능 정의를 “현재 구현”이 아니라 “정의/계획”으로 분리 기록한다.
- **결정/정의(요약)**:
  - 맵 클릭 상호작용: 우클릭=좌표 생성/삭제, 좌클릭=화면 스크롤(좌우).
  - 좌표 상태/속성: 충전 위치/특정 좌표/이름 지정, 순회 번호 및 경로·방향 편집(모달에서 드래그앤드랍 순서 조정).
  - 차단 지역: 자유 변형 가능한 영역 스케치.
  - 모드: 수동(teleop) / 자동(순회 시작, 확인 팝업), 스캔 모드(좌표마다 360° 1회전), 가속 모드(속도 프리셋).
  - 로봇 상태 패널: 배터리 + (가스/소리/화재/조도 등) 센서 수치 표시.
  - UI 정리: “화재 발생 이미지 매칭 UI”는 삭제하고 “조도 센서 상태”를 우선 추가, 위험 상태 시 알람 팝업.

### RealSense 카메라 모델 D435 확정 (D435i 아님) + Mac SDK streaming 차단 → Pi4 이전 결정

- **모델 확정**: 주인님 손에 있는 카메라는 `Intel RealSense D435` (Product ID `0B07`, Serial `254522075185`, Asic Serial `350423023342`, FW `5.15.1.55`). `Imu Type: IMU_Unknown` 으로 D435i가 아닌 D435 확정. 그동안 SSOT의 "D435i 도입 후보" 표기는 모두 D435로 정정 대상.
- **Mac 검증 결과**: `sudo /opt/homebrew/bin/rs-enumerate-devices` verbose는 PASS (Depth/RGB/IR 모든 stream profile 노출). 그러나 `rs-hello-realsense`에서 `Frame didn't arrive within 15000` + `Dispatcher: mutex lock failed: Invalid argument` 로 실제 streaming은 차단.
- **차단 원인 (3중 호환 이슈)**:
  1. macOS Monterey+ 이후 librealsense는 sudo 필수 (해결됨)
  2. brew formula 2.58.1은 `-DHWM_OVER_XU=false`, `-DFORCE_RSUSB_BACKEND=true` 빌드 옵션 누락 → 알려진 timeout 버그
  3. macOS Tahoe(26.3.1)은 librealsense 공식 미지원 + Apple Silicon adhoc 서명에 IOUSBHost entitlement 부재
- **결정**: macOS source 재빌드(1~2시간, 성공률 ~40%)는 시도하지 않는다. Pi4 이전(30분, 95%)으로 진행. **근거**: 어차피 ROS2 Jazzy는 macOS 미지원 → 박물관 매핑은 Pi4가 정답. URHYNIX 시연 흐름(카메라=Pi4 직결 → ROS topic 발행 → Unity(Mac) 구독)에서 카메라가 Mac에 꽂혀있을 일 자체가 없음.
- **박물관 매핑 계획 영향**:
  - VIO (Visual-Inertial Odometry) 폐기 → odom 보정은 LDS-03 + wheel odom으로
  - RTAB-Map RGB-D SLAM, 가벽 detection (낮은 가벽 depth로 잡기), 액자 YOLO+depth 위치 식별, Pi 카메라 자리 흡수, Unity 3D mesh import — 모두 그대로 살아있음
  - 전체 계획의 95% 유지, IMU 의존 부분만 빠짐
- **잔여 액션**:
  1. 카메라 케이블 → Pi4 USB 3.0 직결 (사람 작업, 1분)
  2. `ssh urhynix-robot` + `sudo apt install ros-jazzy-realsense2-camera` (5분)
  3. `ros2 launch realsense2_camera rs_launch.py` + 토픽 30Hz hz 검증 (5분)
- **근거 evidence**: `docs/evidence/2026-06-01-realsense-d435-mac-sdk-smoke.md` (Phase별 결과, 명령 로그, 외부 이슈 트래커 6건 인용)

## 2026-05-29

### 🎉 SLAM end-to-end 첫 검증 PASS — Robot 직접 cartographer + Unity 임포트

- 결정: SLAM은 Mac VM/Docker 우회 시도 모두 실패 후 **로봇 자체에서 cartographer 실행**하기로. multicast 모드(ROS_DISCOVERY_SERVER 미사용)로 통일.
- 산출물: `docs/evidence/maps/desk_static_v1/{.pgm, .yaml, .png, eval.md}` — 5.90m × 5.40m 책상 환경 정적 매핑. Unity Plane scale `(0.5900, 1, 0.5400)` 자동 계산.
- 검증: `/scan` 10Hz + `/map` 1.0Hz + map_saver_cli 정상 + scp + PIL pgm→png + tb3-map-to-unity 한 줄.
- 영향: 경기장 출동 시 같은 흐름 (`tb3-up → tb3-slam → tb3-teleop → tb3-slam-save → tb3-fetch-map → tb3-map-to-unity`)을 25분 주행에 적용. Mac Docker/VM은 본 사례에 불필요로 확인.

### Mac Docker로 외부 SLAM 실행 (라즈베리파이 디스크 회피)

- 결정: cartographer/nav2/map-server를 라즈베리파이가 아니라 Mac Docker 컨테이너에서 실행한다. 로봇은 bringup만 담당.
- 이유: 라즈베리파이 SD 15GB가 96%+ 사용 중이라 apt install이 dpkg hang을 일으킴. 4/4 패키지 commit은 끝났지만 ldconfig trigger가 디스크 0으로 hang. 또한 RPi 4 (4GB RAM)에서 cartographer 동시 실행 시 메모리 부담 큼.
- 영향: 
  - 호스트 종속 (Mac/Ubuntu) — 동료가 Ubuntu라면 native 가능, Mac은 Docker Desktop 4.34+ 호스트 네트워크 필요.
  - 새 자산: `docs/ref/MAC-DOCKER-ROS2-PLAYBOOK.md` + `scripts/tb3.sh`의 `tb3-docker-*` 8 helpers.
  - 이미지: `robotis/turtlebot3:jazzy-pc-latest` (5GB) — cartographer + nav2 + map-server 사전 설치.

### 라즈베리파이 dpkg hang 복구 + 워크스페이스 클린 재빌드

- 결정: dpkg hang (4/4 commit 완료 + trigger 단계 hang) 발견 후 reboot으로 회복. `~/turtlebot3_ws/install/build` 삭제 → `colcon build --symlink-install --parallel-workers 1 --executor sequential` 클린 재빌드.
- 이유: 처음 디스크 정리 시 `~/turtlebot3_ws/build`를 같이 지웠는데 그 안에 install/setup.bash hook 일부가 있어서 launch가 깨짐. sequential 빌드로 메모리 부담 최소화 (8 패키지 6분 17초).
- 영향: bringup 정상 publish (`/scan /odom /tf /battery_state` 등 13 토픽). 다음 세션부터 build/ 절대 지우지 말 것.

### macOS Docker host networking — inbound UDP 미라우팅 미해결

- 사실 확인: Docker Desktop 4.34+ host networking은 outbound는 작동(컨테이너에서 LAN ping OK)이지만 **inbound UDP가 컨테이너 프로세스로 라우팅되지 않음**. `lsof -nP -iUDP:11811` 결과 `com.docker`가 IPv6 dual-stack으로 listen하지만 Fast DDS Discovery Server에 robot 접속 메시지 0건.
- 영향: Mac 컨테이너에서 robot `/scan` topic discovery 실패. 다음 세션 디버깅 출발점: (1) Cyclone DDS XML로 strict unicast peer, (2) `osrf/ros:jazzy-desktop` 다른 base 이미지 시도, (3) 동료 Ubuntu native (multicast 정상) fallback.

### 경기장 진입 + 라이브 SLAM 사이클 검증 (arena_v1)

- 결정: 어제 책상 매핑에서 검증된 흐름(`tb3-up → tb3-slam → save → fetch → map-to-unity`)을 경기장에 그대로 적용해 1차 매핑 산출물 `arena_v1`을 생성. Mac Docker 우회는 시도조차 안 함 (어제 결정대로 robot 직접 cartographer + multicast 모드).
- 근거: `/scan` 10.04Hz + `/map` 1.000Hz 안정. 158×151 px @ 0.05 m/px = 7.90×7.55m. robot/local evidence/Unity Assets 3곳 저장 OK. SSH key 인증 무대화형 통과. ARENA-DEPLOYMENT-CHECKLIST 첫 10단계 절차 작동 검증.
- 영향: `docs/evidence/maps/arena_v1/{pgm,yaml,png,eval.md}` 신규 + `unity-smoke/Assets/Maps/arena_v1.{png,yaml}` 임포트 자동. 어제 결정 "robot 직접 cartographer가 정답"이 경기장 환경에서 재검증됨.

### DHCP IP 변경 대응 — `.138` → `.33` + Unity scene 일시 패치

- 결정: 경기장 Wi-Fi에서 robot이 DHCP로 `192.168.0.33`을 받음. `scripts/tb3.sh`의 `TB3_ROBOT_IP_HINT='192.168.0.138'`는 유지(tb3-ip가 MAC sweep으로 자동 발견). Unity 측 `unity-smoke/Assets/Scenes/SampleScene.unity:151` + `unity-smoke/Assets/Scripts/RosSmokeDashboard.cs:10`의 `rosIP`를 `.138 → .33`으로 임시 패치 + Mac `known_hosts`에서 `.138` 엔트리 정리.
- 근거: ARP MAC 매칭으로 진짜 robot IP가 `.33`으로 검증됨 (`.138`은 다른 기기가 응답해 SSH refused + host key 충돌). Unity Inspector 수동 입력은 시간 낭비라 코드/Scene 직접 수정으로 자동화.
- 영향:
  - 다음 세션 DHCP가 또 바뀌면 같은 패치 반복 필요 (Scene + Script 두 곳).
  - 잔여 결정: helper에 `tb3-unity-set-ip <ip>` 신설 후보 (Scene + Script 일괄 패치 → `tb3-ip` 결과 자동 주입). 미실행.
  - HANDOFF "Unity rosIP 매 세션 수동" 이슈에 패치 절차 추가 + git status에 두 파일 변경 잡힘 (commit 시점에 임시방편임 명시).

### 회전만 매핑의 한계 인식 + 하이브리드 패턴 표준화

- 결정: 경기장 중앙에서 회전만 5~6바퀴 매핑한 결과 가벽이 LDS-03 반경 3.5m 안에 부분적으로만 들어옴을 확인. **다음 매핑(arena_v2)부터 하이브리드 (회전 + 작은 stop & rotate 이동) 패턴을 표준**으로 한다. arena_v1은 회전만 매핑의 비교 evidence로 영구 보존.
- 근거: arena_v1 픽셀 통계 = occupied 1.9% / free 98.1% / **unknown 0.0%**. unknown 0은 회전 5바퀴라 모든 방향 관측 완료이지만 외곽이 둥글게 끊기고 가벽 연결선 없음 = 가벽 일부가 LiDAR 반경 밖. PNG 시각 검증 결과 박물관 보호 영역 시각화로는 부적합.
- 하이브리드 표준 절차 (다음 매핑 채택):
  1. 출발점에서 360° 1바퀴 (각속도 0.2 rad/s)
  2. 천천히 1m 직진 (선속 0.10 m/s)
  3. 360° 1바퀴
  4. (2)~(3) 반복 3~4 stop으로 가벽 전체 도달
  5. 출발점 복귀 후 360° 한 번 더 → 루프 클로저 강제
  6. 총 ~5분 예상
- 영향:
  - `docs/ref/ARENA-DEPLOYMENT-CHECKLIST.md` §"현장 도착 후 첫 10분" 8단계 매핑 주행을 하이브리드로 갱신 (잔여 작업).
  - HANDOFF Top 1을 "arena_v2 하이브리드 매핑 OR W2 SCRUM-10 진입" 분기 결정으로 갱신.
  - 발표 시연용 maps는 arena_v2 후보. arena_v1은 발표 자료에 "회전만의 한계" 비교 슬라이드용 활용 가능.

### 매핑 실패 진단 정정 — "회전 한계"가 아니라 "가벽 높이 < LiDAR 스캔 평면" (회의록 기반)

- 결정: 위의 "회전만 매핑의 한계" 진단을 **정정한다**. 실제 원인은 **경기장 가벽 높이가 TurtleBot3 Burger의 LDS-03 LiDAR 스캔 평면(약 192mm 지상고)보다 낮아서 LiDAR가 가벽 상단을 over-shoot한 것**. 회전 횟수·반경과는 무관.
- 근거: 2026-05-29 Confluence 회의록 (page `3932161`) 김주영 발언 직접 인용: *"png파일 얻었지만 라이다높이보다 가벽이낮아서 벽 매핑실패. 하지만 좌표값읽기 성공"*. arena_v1 픽셀 통계의 occupied 1.9% / unknown 0%는 같은 증상이지만 원인은 **평면(거리)이 아니라 수직(높이)**.
- 영향:
  - **하이브리드 매핑 권장 폐기** — 가벽 높이가 부족하면 회전을 더 해도 stop & rotate를 추가해도 해결 안 됨.
  - `arena_v1/eval.md` Verdict + Recommendation 재정정 (하이브리드 권장 제거).
  - `HANDOFF.md` Top 1 분기 재정의: "가벽 높이 측정 + 보강" 우선, 보강 후 hybrid 매핑.
  - `.claude/skills/map-quality-eval/eval.py` classify() 로직에 **수직 차원 가능성** 추가.
  - 다음 매핑 전 사람이 줄자로 가벽 실측 높이를 eval.md에 기록.
- 잠재 해법 (다음 매핑 전 결정 분기):
  - (A) 가벽을 200mm 이상으로 물리적 보강 (테이프·종이·박스)
  - (B) LiDAR를 더 낮게 마운트 (Burger 구조상 어려움, 비추천)
  - (C) 카메라 vision 기반 가벽 인식으로 보완 (임현찬 YOLO 라인 활용)
  - (D) 가벽을 obstacle이 아닌 **"보호 영역 경계 마커"**로 정의 변경 (Unity 디지털 트윈 + DB 좌표만 사용, Nav2 cost map 미반영)
- 잠금: arena_v1의 **"좌표값 읽기 성공"**(odom·TF·map 좌표 1:1)은 그대로 유효. 시각 텍스처용 PNG만 한계.

### Pi 카메라 토픽 검증 + YOLO/OpenCV 환경 통과 + MVP 4 클래스 잠금 (임현찬)

- 결정: Raspberry Pi 카메라 ROS 토픽 3종(`/camera/image_raw`, `/camera/camera_info`, `/camera/image_raw/compressed`)을 30Hz 정상 publish로 검증 완료. MP4 + ROS bag 동시 녹화 스크립트(`/home/pi/camera_recordings/scripts/record_bag_mp4.sh`)를 표준 녹화 도구로 채택. 노트북 Ubuntu에 YOLO/OpenCV 환경 + `yolo11n.pt` 기본 모델 + 실시간 카메라 스트림 인식 통과. **MVP 학습 클래스 4종 잠금: 로봇 · 사람 · 중요품 · 불**.
- 근거: 2026-05-29 Confluence 회의록 (page `3932161`) 임현찬 진척 보고 직접 인용.
- 영향:
  - `docs/ref/PRD.md` 카메라 인식 범위에 4 클래스 명시.
  - `docs/ref/ARCHITECTURE.md` Vision 파이프라인에 MP4/bag 분리 (MP4 = 즉시 확인, bag = 재처리) 추가.
  - `docs/ref/CONTRACT.md`에 카메라 토픽 3종 + 30Hz 명시.
  - `docs/ref/JIRA-MAP.md` SCRUM-19/20에 진척 반영.
  - 다음 작업: 자체 데이터셋 촬영 + 라벨링 + 커스텀 YOLO 학습 (W2 후반).
- 잠금: 기본 `yolo11n.pt`로는 박물관 도메인(액자·중요품) 인식 한계 — 발표 시연 전에 커스텀 학습 필수.

## 2026-05-26

### 로봇팔 제거 버전으로 MVP 진행

- 결정: FR5 로봇팔, 픽앤플레이스, 장애물 물리 제거는 이번 버전에서 제외한다.
- 이유: 7~8주 일정 안에서는 TurtleBot3 자율주행, Unity 관제, 카메라 인식, DB 기록에 집중하는 편이 성공 가능성이 높다.
- 영향: 발표 주제는 협동로봇 전체 시스템보다 "Unity Digital Twin 기반 자율주행 장애물 인식 관제"에 가까워진다.

### 카메라가 있는 상태 기준으로 작업 재분리

- 결정: 카메라가 있다고 가정하고 Jira에 카메라 설치, ROS pose 동기화, 데이터셋, 실시간 인식, Unity 표시, DB 저장, QA 카드를 추가한다.
- 이유: 카메라가 있는 경우 Vision 작업이 단순 샘플 이미지 테스트보다 훨씬 커진다.
- 영향: 김선일, 임현찬, 김주영, 박태진 모두에게 카메라 관련 작업이 나뉘었다.

### Unity는 관제와 시각화 중심

- 결정: 실제 로봇 안전 판단과 주행 제어의 진실값은 ROS/TurtleBot3 쪽에 둔다.
- 이유: Unity는 시각화와 관제 UI에는 강하지만, 실제 로봇 제어의 안전 기준으로 삼기에는 위험하다.
- 영향: Unity는 map, pose, path, camera result, DB summary를 보여주는 역할을 맡는다.

## 2026-05-27

### 단일 TurtleBot 비교 데모 → 다중 경비 로봇 디지털 트윈 전환

- 결정: 발표 시나리오를 "LiDAR only vs Camera+Vision 비교 데모"에서 "다중 TurtleBot 디지털 트윈 경비 로봇 (tb3_1 순찰/감지 + tb3_2 출동/확인)"로 전면 전환한다.
- 근거: Confluence 1540099 "브레인스토밍: 다중 경비 로봇 디지털 트윈 마인드맵" (2026-05-27)에서 팀 합의. 기존 비교 데모는 발표 임팩트와 데이터셋 가치가 떨어진다는 판단.
- 영향:
  - SSOT 8종(`PRD`, `PROJECT-PLAN`, `PROJECT-STATUS`, `ARCHITECTURE`, `CONTRACT`, `SCHEMA`, `JIRA-MAP`, `STACK-PROFILES`) 전면 재작성.
  - 토픽 네임스페이스: `/turtlebot/*` → `/tb3_1/*`, `/tb3_2/*`, `/security/*`.
  - DB 테이블 재정의: `drive_log`/`detection_log` → `events`/`dispatches`/`camera_captures`/`session_meta`.
  - Jira SCRUM-8~25 티켓 제목·담당자·Sprint 재배치 (ID 유지).
- 새 역할 매트릭스 (5 모듈 × 4명):
  - 백엔드 DB / ROS-TCP 라벨링 / AI: 김주영, 김선일
  - 아두이노 (메인 보드/통신): 박태진, 임현찬, 김주영
  - 유니티 관제UI · ROS-TCP 통신 · 영상 라이브 스트리밍: 김선일, 박태진
  - 아두이노 센서: 김주영, 임현찬, 박태진
  - 터틀봇 LiDAR · 카메라 · SLAM · 네비게이션: 임현찬, 김선일
- 보존: ARCHITECTURE의 "ROS=진실값, Unity=시각화" 원칙은 유지. `GIT-WORKFLOW.md`, `DECISION-LOG.md` 양식 유지.
- 발표 한 줄(MVP): tb3_1이 야간 순찰 중 센서 이벤트를 감지하면 Unity 관제 화면에 위치·이벤트가 표시되고, tb3_2가 감지 지점으로 출동해 카메라로 확인하며, 모든 이벤트와 대응 결과를 DB에 기록한다.

### 발표 제목·범위·센서 인터페이스 정리

- 결정: 표시 제목은 `디지털트윈경비로봇`으로 통일하고, 모바일/태블릿 앱 DT는 이번 범위에서 제거한다.
- 역할: M4(아두이노 센서)에 박태진을 추가해 김주영·임현찬·박태진 3인 담당으로 둔다.
- 보류: 아두이노 센서를 TurtleBot에 붙이는 방식은 아직 확정하지 않는다. S1에서 Arduino 보드→Raspberry Pi USB serial, OpenCR GPIO/ADC, Raspberry Pi GPIO/I2C/UART 후보를 비교한다.

### 센서 연결·적층 구조 확정

- 결정: 센서 4종(PIR/조도/소리/불꽃)은 **별도 Arduino Uno R3 + 브레드보드 → 라즈베리파이 USB serial** 경로로 통일한다. OpenCR 직접 연결과 RPi GPIO 직접 연결 후보는 폐기.
- 이유:
  - 팀이 보유한 **아두이노 기본 키트**(Uno R3 + 브레드보드 + 점퍼선 + LDR + 저항)로 즉시 시작 가능.
  - 주행 펌웨어(OpenCR core)를 건드리지 않아 SLAM/Nav2 안정성 보존.
  - 아날로그 센서 3종(조도·소리·불꽃)을 ADC 외부 IC 없이 처리 가능.
  - 분리된 시스템이라 디버깅이 쉬움 (`/dev/ttyACM0` 시리얼만 확인하면 됨).
- 적층 구조 (위→아래):
  1. **LDS LiDAR (최상단, 절대 양보 X)** — 360° 시야 보존
  2. **Arduino + 브레드보드 + 센서 4종 (NEW 층)** — M3 스페이서 30~40mm로 추가
  3. Raspberry Pi (기존)
  4. OpenCR (기존)
  5. 배터리/모터 (베이스)
- 시리얼 포맷: `EVT,<type>,<severity>,<unix_ts>\n` 예) `EVT,pir,3,1716800000\n`
- 핀 할당 (Arduino Uno R3):
  - PIR → D2 (디지털)
  - 조도 (LDR + 10kΩ 분압) → A0 (아날로그)
  - 소리 (KY-038 D-out) → D3
  - 불꽃 (D-out) → D4
  - 모의 입력 버튼 (화재) → D5
- 영향:
  - `PRD.md` 리스크 표에서 "센서 연결 방식 미확정" 행 제거 → "센서 노이즈" 행만 유지.
  - `ARCHITECTURE.md`에 적층 다이어그램과 핀 매핑 추가.
  - `CONTRACT.md §4` 후보 비교 표 → 확정 표.
  - `PROJECT-STATUS.md` 미확정 항목에서 제거.

### 병렬 작업 우선 — Sprint 앞에 매트릭스 추가

- 결정: 한 사람이 한 모듈에서 직렬로 작업하는 동안 다른 사람들은 다른 모듈에서 동시 진행이 가능하다는 점을 명시한다. `PROJECT-PLAN.md` 앞부분에 **주차×모듈 병렬 매트릭스**와 **의존성 그래프** 섹션을 추가한다.
- 이유: 7주는 빠듯하므로 직렬 대기 시간을 최소화해야 한다. 모듈 간 인터페이스(CONTRACT.md)만 합의해두면 각 모듈은 독립적으로 진행 가능.
- 핵심 병렬 라인 (S1 1주차 동시 시작 가능):
  - 김주영·김선일 → SCRUM-14 (DB 스키마 초안, M1)
  - 김선일·박태진 → SCRUM-9 (Unity UI 초안, M3)
  - 박태진·임현찬 → SCRUM-16 (실내 트랙 환경, 공통)
  - 김주영·임현찬·박태진 → 아두이노 키트 점검 + 핀 배선 도면 (M2/M4 준비)
- 직렬 병목: SCRUM-8 합의 (1일) → SCRUM-10 (SLAM은 SCRUM-16 뒤) → SCRUM-12 (출동은 SCRUM-13 뒤).

### 하드웨어 최종 확정 — TurtleBot3 Burger + Arduino Uno + OpenCR 5V 분기

- 결정:
  - 로봇 모델: **TurtleBot3 Burger** 확정
  - MCU: **Arduino Uno R3** 확정 (ESP32 검토했으나 키트 보유 우선)
  - 적층: **한 단 추가 없음**. 라즈베리파이 위치를 한쪽으로 치우치게 재배치하고 반대편 빈 공간(약 50×130mm)에 미니 브레드보드 + Arduino를 양면테이프로 부착
  - 전원: **OpenCR 5V 핀 → Arduino 5V 핀 점퍼 2줄(5V + GND)**. AA 배터리 4개 소켓은 추가하지 않는다 (6V로 Uno DC 잭 7V 최저 미달, 무게/관리 부담)
  - 통신: USB Type-B 케이블로 Arduino ↔ 라즈베리파이 (데이터 전용, USB 5V는 Uno 내부 P-MOSFET이 자동 선택)
- 이유:
  - AA 4개(6V)는 Uno DC 잭에 모자라고 5V 핀 직결이면 OpenCR 5V 분기와 동일 효과 → 배터리 추가 이득 없음
  - OpenCR 5V 출력 마진(약 1A 한계, 현재 LDS 400mA + 자체 100mA + Arduino 150mA = 650mA)이 충분
  - 메인 LiPo 배터리 영향은 시연 10분 기준 ~5분 단축 정도 (사실상 무영향)
- 영향:
  - `PRD.md` 하드웨어 구성 표 갱신 (Arduino 전원 = OpenCR 5V 점퍼)
  - `ARCHITECTURE.md` 적층 다이어그램 단순화 (별도 층 X, 라즈베리파이 옆 부착)
  - `CONTRACT.md §4` 시리얼 배선 표 갱신
- 주의: Arduino 전원은 반드시 **5V 핀**에 (Vin 아님). OpenCR과 Arduino GND 공통 연결.

### Day-1 작업 분담 (2026-05-27 즉시 시작)

- 결정: SCRUM-8 합의는 끝났다고 보고, 각자 오늘부터 모듈 안에서 즉시 가능한 검증 작업을 시작한다.
- 팀 분담:
  - **김주영 + 임현찬**: **라즈베리파이 Pi Camera 스트림 + DB 테스트** (SCRUM-19 일부 + SCRUM-14 일부)
  - **박태진**: **Arduino + PIR(인체 감지) 센서값 → DB 연결 테스트** (SCRUM-13 + SCRUM-14 일부)
  - **김선일**: **Unity 관제 UI 기능 정의 문서화** (SCRUM-9 + SCRUM-22 기능 명세 초안)
- 이유: 인터페이스(`CONTRACT.md`)가 잠긴 상태라 각자 모듈에서 독립 검증 가능. Day-1에 PIR → 시리얼 → DB로 데이터 한 줄이 통하면 S1 끝까지의 자신감이 생긴다.
- 산출물 (오늘 끝):
  - 김주영·임현찬: Pi Camera 토픽 확인 영상 + `events` 테이블에 sample insert 1건
  - 박태진: Arduino 스케치(PIR + 시리얼) + DB insert까지 통한 로그
  - 김선일: Unity UI 기능 목록 1장 (운영 대시보드·이벤트 패널·카메라 패널·모드 토글)

## 2026-05-28

### Arduino 플래시 파이프라인 자동화 + `arduino-flash` 스킬 등록

- 결정: 아두이노 GUI IDE 의존을 줄이고 **`arduino-cli` 기반 컴파일·업로드·시리얼 검증 파이프라인**을 URHYNIX 표준으로 채택한다. 동일 흐름을 재사용하기 위해 `.claude/skills/arduino-flash/SKILL.md`로 스킬화한다.
- 근거:
  - 2026-05-28 PIR(HW-740) + LED 한 줄 검증을 GUI 없이 `brew install arduino-cli` → `core install arduino:avr` → `compile` → `upload` → 시리얼 raw 캡처 30초로 성공적으로 마쳤다.
  - 센서 4종(PIR/조도/소리/불꽃) 동일 보드(Arduino UNO R3)에 반복 플래시될 예정이므로, 동일 절차를 4번 반복하는 대신 스킬로 묶어 첫 회부터 표준화한다.
  - `arduino-cli monitor`가 비-TTY 환경에서 즉시 종료되는 함정도 `stty + cat` 우회로 잡았으므로 AI 비대화형 검증까지 포함해 자산화한다.
- 영향:
  - `.claude/skills/arduino-flash/SKILL.md` 신설 (이번 커밋).
  - `.claude/skills/README.md`에 Embedded / Hardware Skills 표 + Rule of Thumb 1줄 추가.
  - `docs/status/PROJECT-STATUS.md` Evidence Status에 "PIR 플래시 (cli) 통과" 행 추가.
  - `docs/status/HANDOFF.md` 자산 표에 스킬 + 스케치 폴더 추가.
  - 향후 조도·소리·불꽃 센서 작업 시 본 스킬 1개로 일관 진행.
- 주의 (핀 매핑 정렬):
  - 2026-05-28 검증 코드는 **PIR=D7 / LED=D2**로 작성됐다. 그러나 2026-05-27 결정의 **SSOT 핀 매핑은 PIR=D2** (소리=D3, 불꽃=D4, 모의=D5, 조도=A0).
  - LED를 사용하는 디버그 코드는 다음 단계에서 **PIR=D2 / LED=D8(또는 D11)**로 재정렬해야 SSOT와 일치한다. 본 결정은 정렬 의무를 잠그는 의도이며, 다음 박태진 작업분에서 반영한다.

### Day-1 PIR 단계 진행 (Arduino 측 완료, DB 연결 단계 잔여)

- 결정: 박태진 Day-1 작업 중 **Arduino + PIR + 시리얼 로그까지의 절반**은 2026-05-28 시점에 검증 완료로 본다. 남은 절반 **시리얼 → 라즈베리파이 → `events` insert**는 다음 세션 첫 5분 액션으로 유지한다.
- 근거: `/Users/family/jason/URHYNIX/sketches/pir_led/pir_led.ino` 업로드 후 시리얼에서 `[MOTION] detected -> LED ON` / `[CLEAR ] no motion -> LED OFF` 패턴이 안정적으로 출력됨을 확인.
- 영향:
  - `HANDOFF.md` Top 1 액션을 "PIR → DB insert 단계 연결"로 좁힘.
  - `PROJECT-STATUS.md` Day-1 진행 표에서 박태진 행에 "Arduino+PIR 통과(2026-05-28) / DB 단계 잔여" 메모.

### LDR(조도) 센서 추가 + A0 SSOT 정렬 검증

- 결정: PIR 회로 위에 **LDR + 10kΩ 분압회로**를 추가하고, 신호 핀을 **A0 (SSOT 일치)**로 확정한다. 시리얼 라벨 포맷은 `[LDR] A0=<0-1023> (dark|dim|bright|very bright)`로 표준화한다.
- 근거:
  - 2026-05-28 1차 시도는 임시로 A1에 꽂아 검증 → 값 25↔211 진동으로 빛 변화 추종 확인.
  - 2026-05-28 2차로 **A0로 재배선 + 코드 정렬 + 재플래시 + 30초 시리얼 재캡처** 모두 통과. 라벨이 `[LDR] A0=...`로 갱신되고 29↔214 진동 + PIR 모션 동시 발생 시 충돌 없음을 확인.
  - SSOT(2026-05-27 결정)의 `A0 = 조도(LDR + 10kΩ 분압)`와 일치하므로 별도 SSOT 변경 불필요. 본 결정은 **실측 정렬 완료를 잠그는 기록**.
- 영향:
  - `sketches/pir_led/pir_led.ino`가 PIR(D7) + LED(D2) + LDR(A0) 3-기능 베이스 스케치가 됨. 남은 센서(소리/불꽃) 추가 시 본 파일을 분기해 재사용.
  - `arduino-flash` 스킬에 LDR 분압회로 배선 패턴 + 라벨 포맷을 자산화 (version 2).
  - `PROJECT-STATUS.md` Evidence Status에 "LDR A0 정렬 검증" 행 추가.
  - `HANDOFF.md` 자산 표·상태 표에 LDR 정렬 완료 반영, Top 1 잔여는 여전히 **PIR=D7→D2 정렬 + 시리얼→DB insert**.
- 잔여 정렬: PIR=D7(코드) ↔ D2(SSOT) 불일치만 남음. LED는 SSOT에 액추에이터로만 명시되어 있어 D2 사용은 SSOT와 충돌하나, **PIR을 D2로 옮기는 시점에 LED를 D8 또는 D11로 이동**해 함께 해소한다.

### 라즈베리파이 ↔ Arduino USB 시리얼 안정 식별 (2026-05-28 후속)

- 결정: Arduino UNO USB serial을 라즈베리파이에서 **`/dev/tb3_arduino` 안정 심볼릭 링크**로 영구 식별한다. udev rule + 사용자 그룹을 한 번에 잠근다.
- 근거:
  - 2026-05-28 라즈베리파이(`kim@192.168.0.138`) 점검에서 `/dev/ttyACM0` = OpenCR (vendor 0483), `/dev/ttyACM1` = Arduino UNO (vendor 2341, model 0043) 분리 확인.
  - `pyserial 3.5` 사전 설치 확인. bringup tmux 살아있는 상태에서 동시 점검 가능.
  - 기본 udev에서 `/dev/ttyACM1` 권한이 `crw-rw----` + `dialout` 그룹 멤버가 비어있어 `kim` 사용자 접근 불가 (`Permission denied`).
- 적용 사항:
  - `sudo usermod -aG dialout kim` — `kim` 사용자 영구 그룹 가입 (`id`에서 `20(dialout)` 확인 완료).
  - `/etc/udev/rules.d/99-urhynix-arduino.rules` 작성: `SUBSYSTEM=="tty", ATTRS{idVendor}=="2341", MODE="0666", SYMLINK+="tb3_arduino"`. `udevadm control --reload && udevadm trigger`까지 적용.
  - 결과: `/dev/tb3_arduino -> ttyACM1` 자동 생성, 모드 `crw-rw-rw-`. USB 재연결 시에도 룰이 모드+심링크 자동 복구.
- 영향:
  - 라즈베리파이에서 Arduino를 읽는 모든 코드는 **`/dev/tb3_arduino`** 사용 (USB 순서 바뀌어도 안전).
  - 8초 시리얼 캡처에서 `[MOTION] detected -> LED ON`, `[LDR] A0=190 (dark)` 등 표준 라벨 정상 수신 확인 (워밍업 직후 첫 2줄은 버퍼 잔재로 라인 잘림 — readline 정상 동작 후 라벨 깔끔).
  - `CONTRACT.md §4` 시리얼 배선 표에 `/dev/tb3_arduino` 권장 경로 반영 예정 (다음 작업).

### DB 선정 보류 — Day-1 "한 줄 insert" 사전 차단 (2026-05-28)

- 결정: `events` 테이블이 들어갈 데이터베이스를 **이번 세션에서는 선정하지 않는다**. 다음 세션의 첫 행동으로 격상한다.
- 근거:
  - Supabase MCP `list_projects` 결과 URHYNIX 전용 프로젝트가 **없음** (현재 ACTIVE는 `vibe` 1개로 무관, `TailLog`/`mungmungfit`는 INACTIVE이며 별개 프로젝트).
  - `SCHEMA.md`의 `db/migrations/2026-05-27_init_security.sql` 파일도 미작성 상태 → 테이블 DDL 자체가 존재하지 않음.
  - 그래서 박태진 Day-1 잔여 액션 "시리얼→`events` insert"가 **DB 선정**과 **마이그레이션** 두 단계로 사전 차단됨.
- 결정 보류 옵션 3가지 (다음 세션에서 김주영 결정):
  1. **신규 Supabase 프로젝트 `urhynix`** (`ap-northeast-2`, 무료 tier active 2개 한도 내 가능) — 격리·SSOT 정확 일치, 생성 ~2-3분
  2. **기존 `vibe` 프로젝트에 `urhynix` 스키마 추가** — 즉시 가능/비용 0, 다른 프로젝트와 혼재
  3. **라즈베리파이 로컬 Postgres 14+** — 완전 격리/오프라인 가능, Unity·원격 접근 어려움
- 차단 영향:
  - `HANDOFF.md` Top 1이 "PIR → DB insert" → **(0) DB 선정 → (1) `session_meta`+`events` 마이그레이션 → (2) 시리얼→insert 파이썬 스크립트** 3단계로 확장.
  - 박태진 Day-1 작업은 DB 선정 결정까지 **대기**. 단, Arduino 측 잔여 (PIR 핀 D7→D2 정렬)는 독립 진행 가능.
  - `SCHEMA.md` 상단·Open Questions에 "DB 미선정 (Day-1 차단)" 명시.
  - `arduino-flash` 스킬 마지막에 "RPi→DB 단계는 DB 선정 후 별 스킬" 노트 추가.
- 잠금: 본 결정은 "DB 미선정"을 **명시적으로 잠그는 결정**. 다음 세션에서 옵션 1/2/3 중 하나로 전환되는 즉시 새 DECISION 항목으로 갱신.

### DB 선정 완료 — 신규 Supabase `ueupkrxwybuuqxflstvg` (옵션 B + 트위스트) (2026-05-28)

- 결정: URHYNIX `events`/`session_meta`/`dispatches`/`camera_captures` 4테이블은 **신규 Supabase 프로젝트 `ueupkrxwybuuqxflstvg`** (region ap-northeast-1 Tokyo, org `uisuqsaynxoedcsuikqc`)에 잠근다. 기존 시도(`oucgzkbqrzbwxxffmmqt` mungmungfit)는 **egress quota 초과**로 외부 REST가 HTTP 402로 차단되어 폐기.
- 근거:
  - 2026-05-28 외부 진단: `https://ueupkrxwybuuqxflstvg.supabase.co/rest/v1/` HTTP 401 `No API key found` = endpoint 살아있음 + quota 깨끗.
  - Supabase access token `sbp_…` 으로 `supabase projects list` 통과 → org 권한 확인.
  - Management API SQL endpoint `POST https://api.supabase.com/v1/projects/{ref}/database/query`로 마이그레이션 적용 성공 (HTTP 201, 4 테이블 + seed 1건 확인).
- 외부 REST insert 통로:
  - **publishable key** (`sb_publishable_bB5OpwyxD3-9o41kgcSY8g_tDgiCARM`) → RLS auto-on 상태에서 INSERT `HTTP 401 code 42501 RLS violation` (정상 보안).
  - **service_role legacy JWT** → INSERT `HTTP 201` 정상 (RLS 우회). 새 row `c8c389b9-a5ee-4054-87fa-0203454a5d11` 확인.
- 영향:
  - `db/migrations/2026-05-27_init_security.sql` 헤더의 대상 ref를 신규 프로젝트로 갱신.
  - `scripts/arduino_bridge.py` default `SUPABASE_URL`을 `https://ueupkrxwybuuqxflstvg.supabase.co`로 갱신. `SUPABASE_KEY`는 RPi `/etc/urhynix.env`에만 service_role 값으로 주입 (commit 금지).
  - `SCHEMA.md` 상단 "저장소 미선정" 경고 해소.
  - `PROJECT-STATUS.md` Evidence Status에 DB 선정 행 추가.
  - `HANDOFF.md` Top 1을 (1) RPi env 작성 → (2) tb3-up → (3) tb3-bridge → (4) PIR row insert 검증 4단계로 재정렬.
- 키 운영 룰 (2026-05-28 잠금):
  - **service_role legacy JWT** = secret. RPi `/etc/urhynix.env`에만, 절대 repo·HTML 보드·Slack에 박지 않음.
  - **publishable key** = 안전 공개 가능. 단 RLS ON 상태라 외부 anon 접근은 의미 없음 (정책 추가 시 효력).
  - **access token `sbp_…`** = 일회용 작업 토큰. 2026-05-28 작업 후 https://supabase.com/dashboard/account/tokens 에서 revoke 권장.
- RLS 정책: 현재 4테이블 모두 RLS ON · 정책 0개. service_role만 R/W 가능. 추후 시연 dashboard용 SELECT policy는 별도 결정.

### LDR(조도) 이벤트 트리거 규칙 — edge-trigger + 히스테리시스 (2026-05-28)

- 결정: `arduino_bridge.py`가 LDR 시리얼 라인 (`[LDR] A0=<v>`)을 받을 때 **A0 < 200 으로 처음 진입**하는 순간 한 번만 `events` 테이블에 `event_type='dark', severity=1` 로 insert. **A0 >= 250 으로 복귀**하면 내부 state reset(insert 없음). 같은 어두움 상태 안에선 중복 insert 없음.
- 근거:
  - Arduino 스케치 임계값: `<200 dark / <600 dim / <900 bright / else very bright` (PIR+LDR 스케치).
  - LDR은 2초 주기로 시리얼 발행 → 어두운 상태가 계속되면 매 2초마다 row 1건 → 시연 30분이면 ~900 row → DB가 dark로 가득 차고 신호 노이즈 비율 악화.
  - edge-trigger(진입 1회만) + 히스테리시스(`enter=200`, `exit=250`)로 chatter 방지.
  - severity: PIR(=3, 침입)보다 낮은 `1`로 잠금 (어두움 = 야간 모드 진입 신호 정도의 중요도).
- 트리거 흐름:
  ```
  A0=190 (dark) → dark_state=False → insert event_type='dark' → dark_state=True
  A0=180 (dark) → dark_state=True → insert 안 함 (중복 방지)
  A0=260 (dim)  → A0>=250 + dark_state=True → state=False (insert 없음)
  A0=190 (dark) → dark_state=False → insert event_type='dark' → dark_state=True
  ```
- 영향:
  - `scripts/arduino_bridge.py`에 `LDR_DARK_ENTER=200`, `LDR_DARK_EXIT=250`, `self._dark_state` 추가.
  - `scripts/aliases.sh`에 `sb-by-type` / `sb-dark` / `sb-pir` 신규 alias 3개. 시연 시 이벤트 타입별 통계 한 줄.
  - `SCHEMA.md`의 `event_type` enum은 이미 `dark/pir/noise/fire`를 포함하므로 변경 없음.
  - `raw_payload`에 `label="dark"`, `ldr=<A0 raw>`, `ts_unix`, `have_odom` 저장 → 후속 분석에서 어두운 정도(A0 값) 복원 가능.
- 잔여: 시연 시 darkening 속도(LDR 직접 가림)에 따라 `LDR_DARK_ENTER=200`이 너무 민감하거나 둔할 수 있음 → 실측 후 임계값 미세조정. 임계값은 코드 상단 상수 두 줄만 변경.

### SSH 공개키 인증 채택 — expect+비번 의존 영구 제거 (2026-05-28)

- 결정: Mac/Linux 머신의 ed25519 공개키를 로봇 `kim@192.168.0.138`의 `~/.ssh/authorized_keys`에 등록한다. 이후 모든 SSH/SCP 호출이 비번 prompt 없이 즉시 통과하며, expect의 password 처리 의존을 거의 0으로 줄인다.
- 근거:
  - 2026-05-28 사용자 실측에서 `tb3-go` 흐름이 expect heredoc + send 자동 입력으로 처리되긴 했으나, 화면에 password prompt가 일부 노출되고 사용자가 무의식적으로 키 입력을 추가하면 zsh 명령 라인으로 흘러나가 `command not found: 1234` 같은 부작용 발생.
  - 더 근본적으로 일부 셸 상태에서 `$TB3_PASSWORD`가 빈 값으로 expect에 expand되어 자동 입력 실패 → `Connection refused` 연쇄.
  - 공개키 인증은 (a) 비번 prompt 자체가 발생하지 않음 (b) `BatchMode=yes`로 비대화형 호출 가능 (c) 키 분실 시 robot 쪽 authorized_keys만 정리하면 회수 가능.
- 적용:
  - Mac: `ssh-keygen -t ed25519 -N '' -f ~/.ssh/id_ed25519` (이미 있으면 재사용) + `ssh-copy-id -i ~/.ssh/id_ed25519.pub kim@192.168.0.138`.
  - 검증: `ssh -o BatchMode=yes kim@192.168.0.138 hostname` → `kim-desktop` 무대화형 응답.
  - 헬퍼: `scripts/tb3.sh`에 `tb3-key-setup` 함수 신설 — 키 생성/검사 + ssh-copy-id + 검증을 한 줄로.
- 영향:
  - `tb3-go` / `tb3-restart` / `tb3-bridge` / `tb3-down` / `tb3-poweroff` / `tb3-arduino` / `tb3-logs` 모두 password 무관하게 동작.
  - `~/.tb3rc`의 `TB3_PASSWORD`는 (a) 비상시 expect fallback (b) `tb3-key-setup` 첫 ssh-copy-id 호출 용도로만 의미.
  - 협업자 (Ubuntu) 첫 setup 단계에 `tb3-key-setup` 한 줄 추가.
  - `HANDOFF.md` Top 1을 (1) 메인 스위치 ON → (2) `tb3-go` → (3) `tb3-unity` → (4) PIR/LDR → `sb-tail` 검증 4단계로 단순화. SSH key 미등록 머신만 (0) `tb3-key-setup` 한 번 선행.
- 보안:
  - 키는 passphrase 없이 생성됨 — 개인 머신 전제. 공용 PC에선 passphrase 사용 권장 + `ssh-agent`에 일시 add.
  - 키 회수: 로봇에서 `sed -i '/<MAC public key fingerprint>/d' ~/.ssh/authorized_keys`.

### 박물관/미술관 액자 보호 컨셉 + 미디어/좌표 저장 요구 반영 (2026-05-28)

- 결정: 디지털트윈경비로봇의 발표 컨셉을 **박물관/미술관 액자형 중요물품 보호**로 구체화한다. 사진이 붙은 액자형 타깃을 카메라가 보호 대상으로 인식하고, 외부자 판단은 PIR 단독이 아니라 **PIR + LiDAR 변화 + pose log** 조합으로 남긴다.
- 이유:
  - "무엇을 지키는 로봇인가"가 분명해져 발표 시나리오가 직관적이다.
  - 화재 의심 이벤트에서 액자/중요물품 주변 카메라 확인, 영상 클립, 대응 좌표 로그를 함께 보여주면 DB/AI/Unity/로봇 파트가 하나의 체인으로 설명된다.
  - 조도 센서가 어두움을 감지했을 때 LiDAR를 물리적으로 새로 켜는 구조가 아니라, 저속 순찰·스캔/pose 로그 저장 빈도 증가·확인 이벤트 강화로 "LiDAR 강화 모드"를 표현하는 편이 TurtleBot 구조와 맞다.
- 영향:
  - `PRD.md`: MVP/성공 기준/데이터 수집 전략/리스크에 액자형 중요물품, 좌표·사진·영상·사운드 저장 요구 추가.
  - `ARCHITECTURE.md`: `protected_assets`, `pose_logs`, `media_artifacts`, 박물관/미술관 보호 컨셉, LiDAR 강화 모드 원칙 추가.
  - `SCHEMA.md`/`CONTRACT.md`: 현재 실제 DB 4테이블과 분리해 `pose_logs`, `media_artifacts`, `protected_assets`, `protected_asset_id`, `asset_seen`/`asset_missing` 확장 예정 계약 추가.
  - `PROJECT-PLAN.md`/`JIRA-MAP.md`: SCRUM-9/14/15/16/21/23 제목과 Sprint 작업을 보호 컨셉에 맞게 확장.
- 잔여:
  - 현재 Supabase에는 초기 4테이블이 적용되어 있다. `pose_logs`/`media_artifacts`/`protected_assets` 실제 마이그레이션은 SCRUM-23에서 별도 SQL로 적용한다.
  - 카메라 인식 1차 구현은 AprilTag/QR/고대비 프레임 같은 보조 표식을 우선 후보로 둔다.
  - 2026-05-28 REST 실조회 확인: `session_meta`, `events`, `dispatches`, `camera_captures`는 HTTP 200. `pose_logs`, `media_artifacts`, `protected_assets`는 HTTP 404(PGRST205)로 현재 미존재.

### 매일 18:00 Confluence 회의록 기반 SSOT 자동화 예약 (2026-05-28)

- 결정: Codex automation `urhynix-daily-ssot-sync-from-confluence`를 매일 18:00(KST) 실행하도록 생성한다.
- 이유: 당일 Confluence 회의록의 결정/완료/블로커/다음 액션을 로컬 SSOT와 외부 Confluence/Jira에 반복 반영해야 하며, 사람이 매번 놓치기 쉽다.
- 실행 기준:
  - 작업 디렉터리: `/Users/family/jason/URHYNIX`
  - 로컬 SSOT를 먼저 읽고 갱신한다.
  - 실제 검증된 현재 상태와 예정안을 분리한다. DB/Jira 상태가 언급되면 가능한 범위에서 실조회 후 current로 승격한다.
  - SSOT 변경이 보드에 영향을 주면 `python3 docs/whiteboards/build_bundle.py`를 실행하고 HTML 파싱 검증을 수행한다.
  - Confluence/Jira는 회의록에서 명확히 영향받은 항목만 갱신한다.
- 주의: 외부 문서 전체 덮어쓰기는 필요할 때만 수행하고, 기본은 검증된 변경만 반영한다.

### 신규 128GB SD + Ubuntu 24.04.4 + ROS2 Jazzy 풀 부트스트랩 (2026-06-01)

- 결정: 라즈베리파이4의 기존 16GB SD를 128GB로 교체하고, Mac에서 직접 `dd` + cloud-init 사전설정 박는 방식으로 새 부팅 환경을 잠근다. ROS2 Jazzy + turtlebot3 메타 + ld08_driver + ros_tcp_endpoint까지 한 세션에 풀 셋업.
- 이유:
  - 기존 SD가 디스크 1.1GB(94%) 빡빡해서 colcon build/패키지 추가가 위험. 128GB로 여유 확보.
  - Raspberry Pi Imager GUI는 매번 수동 클릭이 필요하지만, cloud-init `user-data`/`network-config`/`meta-data` 3개 파일을 `system-boot` 파티션 루트에 두면 Pi용 `preinstalled-server-arm64+raspi.img.xz`가 자동 인식한다. 재현성과 자동화에 더 좋다.
  - Opus 자기리뷰가 "NoCloud datasource_list 명시 필요 / cloud/ 서브디렉토리 필요"라고 했지만, 실제 검증에서는 둘 다 불필요했다. Ubuntu Pi 이미지의 cloud-init은 system-boot 파티션 루트의 user-data를 자동으로 NoCloud datasource로 인식한다 (`status: done` `DataSourceNoCloud [seed=/dev/mmcblk0p1]`).
- 잠금 사항:
  - 이미지: `ubuntu-24.04.4-preinstalled-server-arm64+raspi.img.xz` (SHA256 `790652fa...0d37`)
  - 사용자: `kim` / 비번 `1234` (학원 LAN 한정, 발표 후 변경 권장)
  - hostname: `urhynix-robot`, timezone `Asia/Seoul`, locale `ko_KR.UTF-8`
  - SSH 키 인증 자동 (Mac `~/.ssh/id_ed25519` 등록), `ssh_pwauth: true` (helper 호환)
  - 네트워크: 유선 eth0 DHCP. 학원이 `192.168.0.x` ↔ `192.168.10.x` 라우팅을 해줘서 robot이 `192.168.10.59`에 있어도 Mac에서 직접 SSH/ping 가능. 단 mDNS multicast는 라우터 못 건너 `.local` 미작동 → Mac `~/.ssh/config` 별칭 `urhynix-robot` 으로 우회.
  - ROS2 Jazzy + `ros-jazzy-turtlebot3` 메타(2.3.6) + cartographer + nav2-bringup + hls-lfcd-lds-driver + dynamixel-sdk + rmw-cyclonedds-cpp 모두 apt
  - src 빌드: `ld08_driver` (jazzy 브랜치, LDS-03 LiDAR) + `ros_tcp_endpoint` (main-ros2 0.7.0, Unity 통신)
  - `~/.bashrc`에 ros-jazzy setup + ws setup + TURTLEBOT3_MODEL=burger + LDS_MODEL=LDS-03 + OPENCR_PORT=/dev/ttyACM0 + ROS_DOMAIN_ID=230 자동 source (초기 30 → 2026-06-02에 230으로 통일, 티원과 일치)
  - udev rules `/dev/tb3_arduino` (Arduino UNO 2341:0043, 2a03:0043) + `/dev/tb3_opencr` (STM 0483:5740) 안정 심볼링크
  - `/etc/urhynix.env` 템플릿 (640 root:kim, SUPABASE_KEY 자리는 `PASTE_SERVICE_ROLE_JWT_HERE` 로 비워둠 — 발표 직전 주인님이 채움)
- 잔여:
  - 다음 세션 첫 5분에 `/etc/urhynix.env` SUPABASE_KEY 주입 (service_role JWT, 절대 commit 금지)
  - OpenCR firmware 재플래시, Arduino UNO PIR/LDR 스케치 재플래시 (D2 핀 SSOT 정렬)
  - Pi 카메라 동작 검증 (`libcamera-hello -t 0`)
  - bringup `/scan` `/odom` topic 검증 (LiPo ON 필요)
  - 그 다음에 가벽 보강(옵션 A) + arena_v2 매핑
- 근거: `docs/evidence/2026-06-01-new-sd-128gb-ros2-jazzy-bootstrap.md`

### IP-drift zero-touch 화 — Unity rosIP + helper mDNS hostname 기반화 (2026-06-01 오후)

- 결정: DHCP IP가 바뀔 때마다 Unity Scene/Script/helper의 hardcoded IP를 patch하던 매 세션 첫 5분 표준 작업(`ip-drift-resync` 스킬 호출)을 mDNS hostname 기반화로 zero-touch 한다.
- 이유: 신규 SD 부트스트랩 후 robot이 학원 Wi-Fi(codelab_5G)에 영구 연결됐고 avahi-daemon이 `urhynix-robot.local`을 publish. Mac과 robot이 같은 192.168.0.x 서브넷에 있으므로 mDNS multicast 작동. 모든 진입점에 hostname을 박으면 IP 변경에 무관.
- 잠금 사항:
  - `unity-smoke/Assets/Scenes/SampleScene.unity:151` rosIP=`urhynix-robot.local`
  - `unity-smoke/Assets/Scripts/RosSmokeDashboard.cs:10` 기본값=`urhynix-robot.local`
  - `scripts/tb3.sh`에 `export TB3_HOSTNAME='urhynix-robot'` 추가 + `tb3-ip()` 맨 앞에 mDNS 우선 시도 (`ping <hostname>.local`에서 IP 추출, 실패 시 기존 ARP sweep으로 fallback)
  - Mac `~/.ssh/config` `Host urhynix-robot` 별명 → `HostName urhynix-robot.local`
- 효과: IP 바뀌어도 `ssh urhynix-robot` / `tb3-ip` / Unity 모두 자동 follow. `ip-drift-resync` 스킬은 호출 거의 불필요 (다른 망 가거나 mDNS 죽었을 때만 안전망으로 남음).
- 검증: 랜선 분리 + 재기동 후 무선 단독 PASS (eth0 IP 비어있는 상태로 wlan0=192.168.0.82만으로 ssh + ros2 진입 OK).
- 근거: `docs/evidence/2026-06-01-new-sd-128gb-ros2-jazzy-bootstrap.md` §"IP-drift zero-touch 화"
