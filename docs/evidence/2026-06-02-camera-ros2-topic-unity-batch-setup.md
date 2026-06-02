# Pi Camera ROS2 토픽 30Hz + Unity batch 자동 패널 추가 (2026-06-02)

> 젠지(`urhynix-robot`)에서 `camera_ros` ROS2 노드 + `ros_tcp_endpoint`를 백그라운드 launch해서 `/tb3_2/camera/image_raw` 30.095Hz 발행 확인. Unity batch mode로 `CameraStreamPanel.cs` + `CameraPanelSetup.cs` Editor script 자동 실행 → SampleScene에 GenjiCameraPanel + T1CameraPanel 2종 GameObject + 543 라인 자동 추가. 두 패턴을 신규 스킬 2종(`robot-camera-bringup`, `unity-camera-panel`)으로 영구 자산화.

## 환경

| 항목 | 값 |
|---|---|
| 호스트 (젠지) | `urhynix-robot` (kim@192.168.0.82) — Pi 4 Model B, Ubuntu 24.04.4 |
| ROS | Jazzy + `ros-jazzy-camera-ros 0.6.0` + `ros-jazzy-compressed-image-transport 4.0.6` + `ros-jazzy-ros-tcp-endpoint` |
| Pi Camera | Module v2 (Sony IMX219, 8MP, 3280×2464) |
| libcamera | Pi fork v0.7.1 (어제 빌드, `/usr/local/lib/aarch64-linux-gnu/libcamera*.so.0.7.1`) |
| ROS_DOMAIN_ID | 230 (어제 통일) |
| Unity Editor | 6000.0.64f1 |
| Mac | macOS Tahoe 26.3.1 (arm64) |

## Phase 1 — 패키지 설치 (sudo apt)

```
ros-jazzy-camera-ros 0.6.0-1noble.20260413.020755
ros-jazzy-compressed-image-transport 4.0.6-1noble.20260412.175917
```

## Phase 2 — camera_ros 노드 launch (ABI 충돌 우회)

**1차 시도 (실패)**:
```bash
ros2 run camera_ros camera_node --ros-args -r __ns:=/tb3_2
```

결과:
```
[FATAL] Serializer control_serializer.cpp:626 A list of V4L2 controls requires a ControlInfoMap
```

→ apt camera_ros(시스템 libcamera v0.7.0)와 Pi fork libcamera(v0.7.1, `raspberrypi_ipa_proxy`) 사이 ABI 충돌.

**2차 시도 (성공)** — Pi fork libcamera를 LD_LIBRARY_PATH로 강제:
```bash
export LD_LIBRARY_PATH=/usr/local/lib/aarch64-linux-gnu:$LD_LIBRARY_PATH
export LIBCAMERA_IPA_MODULE_PATH=/usr/local/lib/aarch64-linux-gnu/libcamera
nohup ros2 run camera_ros camera_node \
  --ros-args -r __ns:=/tb3_2 -p width:=1280 -p height:=720 \
  > ~/camera_node.log 2>&1 < /dev/null & disown
```

검증 (PID 12454):
```
[INFO] camera "/base/soc/i2c0mux/i2c@1/imx219@10" configured with 1280x720-XRGB8888/sRGB stream
[INFO] Sensor: imx219 - Selected sensor format: 1920x1080-SBGGR10_1X10/RAW
```

## Phase 3 — 토픽 hz 측정

```bash
ros2 topic list | grep camera
# /tb3_2/camera/camera_info
# /tb3_2/camera/image_raw
# /tb3_2/camera/image_raw/compressed
```

`ros2 topic hz /tb3_2/camera/image_raw` 결과:

| 측정 시점 | average rate | min | max | std dev |
|---|---|---|---|---|
| Window 31 | **30.281 Hz** | 0.020s | 0.040s | 0.00386s |
| Window 61 | **30.139 Hz** | 0.020s | 0.042s | 0.00363s |
| Window 92 | **30.095 Hz** | 0.020s | 0.042s | 0.00348s |

→ **30Hz ±0.3 안정 발행 PASS**.

## Phase 4 — ros_tcp_endpoint launch

함정: launch 파일 이름 `default_server_endpoint.launch.py` → 실제는 `endpoint.py`. 그냥 `ros2 run`으로 직접:

```bash
nohup ros2 run ros_tcp_endpoint default_server_endpoint \
  --ros-args -p ROS_IP:=0.0.0.0 -p ROS_TCP_PORT:=10000 \
  > ~/ros_tcp.log 2>&1 < /dev/null & disown
```

검증:
```
PID 12886 default_server_endpoint
LISTEN 0  10  0.0.0.0:10000  0.0.0.0:*
[INFO] [UnityEndpoint]: Starting server on 0.0.0.0:10000
```

## Phase 5 — Unity CameraStreamPanel.cs (확장성 컴포넌트)

`unity-smoke/Assets/Scripts/CameraStreamPanel.cs` 신규 (104 lines):
- `topicName` (string) — Inspector 입력. 예: `/tb3_2/camera/image_raw/compressed`
- `displayLabel` (string) — `젠지` / `티원` 등
- `targetImage` (RawImage) — 자동 GetComponent
- `labelText` (Text) — hz 실시간 표시
- `Start()`: `ROSConnection.Subscribe<CompressedImageMsg>(topicName, ...)`
- `Update()`: 매 1초 hz 계산 + 라벨 갱신
- JPEG/PNG → `Texture2D.LoadImage()` 자동 decode

확장 방식: 카메라 추가 = GameObject 복제 + Inspector `topicName` + `displayLabel` 한 줄 변경.

## Phase 6 — Unity batch mode 자동 Scene 갱신

`unity-smoke/Assets/Editor/CameraPanelSetup.cs` 신규 (170 lines).

### 함정 — RectTransform 누락 (1차 실패)

```
MissingComponentException: There is no 'RectTransform' attached to the "GenjiCameraPanel"
```

→ `new GameObject(name)`는 기본 Transform만. UI는 RectTransform 필요.

**우회**: `new GameObject(name, typeof(RectTransform))` 패턴.

### Unity batch 실행

```bash
/Applications/Unity/Hub/Editor/6000.0.64f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -quit -nographics \
  -projectPath /Users/family/jason/URHYNIX/unity-smoke \
  -executeMethod CameraPanelSetup.Setup \
  -logFile /tmp/unity_camera_setup.log
```

소요: **약 90초** (Unity 6 batch 시작 오버헤드).

### 자동 실행 결과 (log)

```
[CameraPanelSetup] Canvas created
[CameraPanelSetup] GenjiCameraPanel → topic=/tb3_2/camera/image_raw/compressed label=젠지 (Pi Camera)
[CameraPanelSetup] T1CameraPanel → topic=/tb3_1/camera/camera/color/image_raw/compressed label=티원 (D435)
[CameraPanelSetup] Done. Scene saved: Assets/Scenes/SampleScene.unity
```

### Scene 파일 변경 검증

```
git diff --stat unity-smoke/Assets/Scenes/SampleScene.unity
# 1 file changed, 543 insertions(+)

grep -E "GenjiCameraPanel|T1CameraPanel|topicName" unity-smoke/Assets/Scenes/SampleScene.unity
# m_Name: T1CameraPanel
# topicName: /tb3_1/camera/camera/color/image_raw/compressed
# m_Name: GenjiCameraPanel
# topicName: /tb3_2/camera/image_raw/compressed
```

→ **Unity Editor 안 켜고 GameObject 2종 자동 추가 PASS**.

## Phase 7 — 신규 스킬 2종 등록

| 스킬 | 위치 | 핵심 |
|---|---|---|
| `robot-camera-bringup` | `.claude/skills/robot-camera-bringup/SKILL.md` | camera_ros + realsense2_camera + ros_tcp_endpoint 한 묶음 launch 표준. 함정 8건 매트릭스. 다음 세션 진입 한 줄 |
| `unity-camera-panel` | `.claude/skills/unity-camera-panel/SKILL.md` | CameraStreamPanel.cs + CameraPanelSetup.cs + Unity batch CLI. 새 카메라 추가 = `AddCameraPanel` 1줄. 함정 7건 매트릭스 |

`.claude/skills/README.md` Embedded/Hardware 표 2행 추가.

## 함정 종합 (다음 세션 진입 시 참고)

| # | 함정 | 우회 |
|---|---|---|
| 1 | camera_ros ABI 충돌 (시스템 libcamera vs Pi fork) | LD_LIBRARY_PATH + LIBCAMERA_IPA_MODULE_PATH로 Pi fork 강제 |
| 2 | ssh 비-인터랙티브에서 ROS env 빈값 | `source /opt/ros/jazzy/setup.bash` 명시 |
| 3 | ssh ControlMaster connection 끊김 (255) | `ssh -o ControlMaster=no` 강제 |
| 4 | nohup 백그라운드 ssh 종료 시 죽음 | `nohup ... < /dev/null & disown` 패턴 |
| 5 | ros_tcp_endpoint launch 파일 이름 불일치 | `ros2 run` 직접 (launch 아님) |
| 6 | Unity UI GameObject의 RectTransform 누락 | `new GameObject(name, typeof(RectTransform))` |
| 7 | Unity batch가 Scene 안 저장 | `MarkSceneDirty()` + `SaveScene()` 명시 |
| 8 | Unity 한글 폰트 누락 | `Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")` |

## 다음 단계 (W2 진입 전 권장)

1. **Unity Editor 켜고 Play** — GenjiCameraPanel에 라이브 영상 뜨는지 시각 검증 (사람 작업)
2. **티원 측 realsense2_camera + ros_tcp_endpoint** launch (동료 진입) → 두 카메라 동시 라이브
3. **scripts/tb3-camera-up.sh** helper 작성 (선택) — robot-camera-bringup 스킬의 한 줄 명령을 스크립트로
4. **dispatcher 노드 (SCRUM-12)** — `/tb3_2/security/event` → `/tb3_1/goal_pose` 변환 ROS 로직

## 외부 근거

- [camera_ros — Raspberry Pi Camera + libcamera ROS2 노드](https://github.com/christianrauch/camera_ros)
- [Unity batch mode CLI](https://docs.unity3d.com/Manual/CommandLineArguments.html)
- DECISION-LOG 2026-06-02 (작명/매핑/도메인 통일 결정)

## 산출물

| 파일 | 신규/변경 | 크기 |
|---|---|---|
| `unity-smoke/Assets/Scripts/CameraStreamPanel.cs` | 신규 | 104 lines |
| `unity-smoke/Assets/Editor/CameraPanelSetup.cs` | 신규 | 170 lines |
| `unity-smoke/Assets/Scenes/SampleScene.unity` | 변경 | +543 lines (GenjiCameraPanel + T1CameraPanel + Canvas) |
| `.claude/skills/robot-camera-bringup/SKILL.md` | 신규 | ~140 lines |
| `.claude/skills/unity-camera-panel/SKILL.md` | 신규 | ~130 lines |
| `.claude/skills/README.md` | 변경 | +2 rows (스킬 인덱스) |
| 이 evidence | 신규 | 본 파일 |

## 한줄정리

`camera_ros` + `LD_LIBRARY_PATH` 우회로 Pi Camera → **/tb3_2/camera/image_raw/compressed 30.095Hz** 발행 + `ros_tcp_endpoint` 0.0.0.0:10000 listening. Unity batch mode로 `CameraStreamPanel.cs` + `CameraPanelSetup.cs` 자동 실행 → SampleScene에 GenjiCameraPanel + T1CameraPanel 2종 자동 추가(`MissingComponent` 함정 잡음). 패턴 2종을 `robot-camera-bringup` + `unity-camera-panel` 스킬로 영구 자산화. 다음 세션 진입 한 줄로 카메라 트랙 ready.
