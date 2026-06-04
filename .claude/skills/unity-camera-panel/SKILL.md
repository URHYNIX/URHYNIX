---
name: unity-camera-panel
description: URHYNIX Unity 관제 UI에 ROS2 카메라 라이브 RGB 패널을 추가하는 표준 패턴. CameraStreamPanel.cs 컴포넌트(topic Inspector 입력) + CameraPanelSetup.cs Editor script(batch mode 자동 실행). 새 카메라 추가 시 AddCameraPanel 한 줄로 확장. — 시연용 Unity 패널 작업의 핵심 자산.
---

# unity-camera-panel

## 언제 쓰나

- Unity 관제 UI에 새 카메라 라이브 RGB 패널 추가할 때
- 박물관 시연 발표 자료용 라이브 영상 셋샷 필요할 때
- 카메라 두 개(젠지 + 티원) 동시 표시
- 코드 손대지 않고 Inspector에서 topic 만 바꿔서 확장

## 핵심 자산 3종

| 파일 | 역할 |
|---|---|
| `unity-smoke/Assets/Scripts/CameraStreamPanel.cs` | 한 컴포넌트가 한 카메라. Inspector에 topic + label 입력 |
| `unity-smoke/Assets/Editor/CameraPanelSetup.cs` | Unity Editor script — 메뉴 `URHYNIX → Setup Camera Panels` 또는 batch mode로 GameObject 자동 생성 |
| Unity batch mode CLI | Editor 안 켜고 Scene에 패널 자동 추가 |

## 컴포넌트 설계 (CameraStreamPanel.cs)

핵심 필드:
- `topicName` (string) — 예: `/tb3_2/camera/image_raw/compressed`
- `displayLabel` (string) — 예: `젠지` 또는 `티원`
- `targetImage` (RawImage) — 같은 GameObject의 RawImage 자동 사용
- `labelText` (Text) — 옵션, hz 표시

작동:
1. Start에서 `ROSConnection.Subscribe<CompressedImageMsg>(topicName, ...)`
2. callback에서 `Texture2D.LoadImage(msg.data)` — JPEG/PNG 자동 decode
3. Update에서 매 1초 hz 계산 + 라벨 갱신

→ 카메라 추가 = GameObject 복제 + Inspector에서 topic + label 한 줄 변경.

## 자동 실행 — Unity batch mode

Unity Editor 안 켜고 명령으로 GameObject 자동 추가:

```bash
/Applications/Unity/Hub/Editor/6000.0.64f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -quit -nographics \
  -projectPath /Users/family/jason/URHYNIX/unity-smoke \
  -executeMethod CameraPanelSetup.Setup \
  -logFile /tmp/unity_camera_setup.log
```

소요: 약 60~90초 (Unity 6 batch 시작 오버헤드).

검증:
```bash
grep "CameraPanelSetup" /tmp/unity_camera_setup.log
# 기대:
#   [CameraPanelSetup] Canvas created
#   [CameraPanelSetup] GenjiCameraPanel → topic=... label=...
#   [CameraPanelSetup] T1CameraPanel → topic=... label=...
#   [CameraPanelSetup] Done. Scene saved
```

Scene 파일 변경 검증:
```bash
git diff --stat unity-smoke/Assets/Scenes/SampleScene.unity
# 기대: 500+ lines insertions
grep "topicName" unity-smoke/Assets/Scenes/SampleScene.unity
```

## 함정 + 우회 표

| 함정 | 증상 | 우회 |
|---|---|---|
| `new GameObject(name)`로 만든 UI GameObject의 RectTransform 없음 | `MissingComponentException: RectTransform` | `new GameObject(name, typeof(RectTransform))` 패턴 |
| Canvas 없는 Scene에서 UI 추가 | Canvas 자식이 안 보임 | `Object.FindFirstObjectByType<Canvas>()` 후 없으면 자동 생성 + ScreenSpaceOverlay |
| EventSystem 없으면 UI 인터랙션 안 됨 | 버튼/입력 무반응 | Setup에서 EventSystem 자동 추가 |
| Scene 저장 안 됨 | batch 실행 후 변경 사라짐 | `EditorSceneManager.MarkSceneDirty(scene)` + `SaveScene(scene)` 명시 |
| 한글 디스플레이 라벨 폰트 깨짐 | □□ 또는 안 보임 | `Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")` 사용 또는 한글 폰트 별도 import |
| 컴파일 에러로 batch 실패 | `Aborting batchmode due to failure: executeMethod method ... threw exception` | log 파일 grep으로 line 번호 확인 후 수정 |
| Unity license 활성화 | `Access token is unavailable` 경고 | 무시 가능 (Personal License로 batch 작동) |
| **★ Unity 기본은 ROS1 모드. ROS2 endpoint 사용 시 `Define Symbol ROS2` 필수** (2026-06-04 발견) | Console: `Incompatible protocol: ROS-TCP-Endpoint is using ROS2, but Unity is in ROS1 mode`. 그 뒤 `OverflowException` + `ArgumentException` deserialize 실패 반복. frame 0장 | **신 Unity 프로젝트 첫 진입 시 무조건**: `Edit → Project Settings → Player → Other Settings → Scripting Define Symbols → "ROS2" 추가 → Apply`. 또는 `ProjectSettings.asset`에 `scriptingDefineSymbols:\n  Standalone: ROS2`. 자세히: `docs/evidence/2026-06-04-controlroom-camera-live-pass.md` 함정 #16 |
| **UI Toolkit `VisualElement`에는 Texture2D 동적 주입 불가** (ControlRoom Phase 2.7) | 카메라 placeholder에 영상 안 흐름 (런타임 background-image asset 변경 안 됨) | UXML에서 `<ui:VisualElement>` → **`<ui:Image>`** (1줄). View에서 `root.Q<Image>("camera-image").image = streamTexture` |
| **macOS Unity 시동 시 `setsid+nohup`은 빨리 죽음** | log 28~41줄에서 종료, ps에 프로세스 0건 | `open -a "/Applications/Unity/Hub/Editor/<ver>/Unity.app" --args -projectPath ... -logFile ...` |

## 새 카메라 추가 (확장성 패턴)

### Option A: Editor script에 한 줄 추가 (영구)

`CameraPanelSetup.cs`의 `Setup()` 안에 한 줄 추가:

```csharp
AddCameraPanel(
    canvas: canvas,
    name: "NewRobotCameraPanel",
    topic: "/tb3_3/camera/image_raw/compressed",   // 새 카메라 토픽
    label: "신규 로봇",
    anchorMin: new Vector2(0, 1),
    anchorMax: new Vector2(0, 1),
    pivot: new Vector2(0, 1),
    anchoredPos: new Vector2(20, -20),              // 위치만 조정
    size: new Vector2(320, 240)
);
```

그 다음 batch mode 재실행 → Scene에 자동 추가.

### Option B: Unity Editor 직접 (즉석)

1. Hierarchy에서 `GenjiCameraPanel` 우클릭 → **Duplicate**
2. 이름 변경
3. Inspector에서 `topicName` + `displayLabel` 한 줄 변경
4. Rect Transform 위치 조정

## 박물관 시연 매핑 (회의록 5111810)

| 패널 이름 | 별명 | 카메라 | 토픽 |
|---|---|---|---|
| GenjiCameraPanel | 젠지 | Pi Camera v2 (IMX219) | `/tb3_2/camera/image_raw/compressed` |
| T1CameraPanel | 티원 | RealSense D435 | `/tb3_1/camera/camera/color/image_raw/compressed` |

## 의존성

- ROS-TCP-Connector (`Unity.Robotics.ROSTCPConnector`) — unity-smoke에 이미 설치
- `RosMessageTypes.Sensor.CompressedImageMsg` — 어셈블리 import
- robot 측 `ros_tcp_endpoint` 노드 실행 중 + 같은 LAN + ROS_DOMAIN_ID 일치
- robot 측 camera 토픽 발행 중 (`robot-camera-bringup` 스킬 참조)

## 검증 흐름 (full smoke)

```bash
# 1) robot 측 카메라 + ros_tcp_endpoint launch (robot-camera-bringup 스킬)
# 2) Unity batch mode로 Scene에 패널 자동 추가
/Applications/Unity/Hub/Editor/6000.0.64f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -quit -nographics \
  -projectPath /Users/family/jason/URHYNIX/unity-smoke \
  -executeMethod CameraPanelSetup.Setup \
  -logFile /tmp/unity_setup.log

# 3) Unity Editor 켜고 Play → 라이브 영상 확인
#    (사람 작업, 발표 자료 스크린샷 캡처)
```

## 한줄정리

`CameraStreamPanel.cs` 한 컴포넌트 + `CameraPanelSetup.cs` Editor script + Unity batch mode 명령 3종이면 박물관 시연 카메라 패널을 **코드 손 안 대고 추가**할 수 있어요. 새 카메라 추가는 `AddCameraPanel(...)` 한 줄 박고 batch 재실행 또는 Hierarchy에서 Duplicate + Inspector topic 변경.
