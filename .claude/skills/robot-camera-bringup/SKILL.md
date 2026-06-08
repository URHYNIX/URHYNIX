---
name: robot-camera-bringup
description: URHYNIX 박물관 시연용 카메라 노드 + ros_tcp_endpoint 백그라운드 launch + 토픽 hz 검증 표준 패턴. 젠지(Pi Camera v2 IMX219, camera_ros) + 티원(RealSense D435, realsense2_camera) + ROS-TCP-Endpoint 한 묶음. macOS Bash + ssh + sudo + LD_LIBRARY_PATH 우회 + nohup + disown + setsid 표준화. — 매 세션 첫 5분에 카메라 트랙 살리는 데 반복 사용.
---

# robot-camera-bringup

## 언제 쓰나

- 매 세션 첫 5분 — 두 로봇 카메라 트랙(camera_ros + realsense2_camera + ros_tcp_endpoint)을 한 번에 살릴 때
- 박물관 시연 dry-run 직전
- robot 재부팅 후 토픽 발행 노드들이 다 꺼졌을 때
- ROS_DOMAIN_ID 통일 검증 후 cross-visibility 확인

## 사용 호스트

| 별명 | hostname | 카메라 | 토픽 namespace | 포트 |
|---|---|---|---|---|
| 젠지 | `urhynix-robot` (kim@192.168.0.82) | Pi Camera v2 IMX219 | `/tb3_2/camera/*` | 10000 (TCP endpoint) |
| 티원 | `t1@192.168.0.250` (hostname `rb`) | RealSense D435 | `/tb3_1/camera/*` | (티원 측 endpoint) |

ROS_DOMAIN_ID=230 양쪽 동일.

## 표준 launch — 젠지 (Pi Camera)

### A. camera_ros (Pi Camera) launch — **LD_LIBRARY_PATH 우회 필수**

apt의 camera_ros는 시스템 libcamera(v0.7.0)으로 컴파일됐는데 우리 빌드한 Pi fork libcamera(v0.7.1)와 ABI 충돌(`FATAL Serializer: ControlInfoMap required`). 우리 Pi fork를 강제 로드해서 우회:

```bash
ssh -o ControlMaster=no urhynix-robot '
  source /opt/ros/jazzy/setup.bash &&
  source ~/turtlebot3_ws/install/setup.bash 2>/dev/null
  export ROS_DOMAIN_ID=230
  export LD_LIBRARY_PATH=/usr/local/lib/aarch64-linux-gnu:$LD_LIBRARY_PATH
  export LIBCAMERA_IPA_MODULE_PATH=/usr/local/lib/aarch64-linux-gnu/libcamera
  nohup ros2 run camera_ros camera_node \
    --ros-args -r __ns:=/tb3_2 -p width:=1280 -p height:=720 \
    > ~/camera_node.log 2>&1 < /dev/null & disown
'
```

검증 (별도 ssh):
```bash
ssh urhynix-robot 'source /opt/ros/jazzy/setup.bash && export ROS_DOMAIN_ID=230 && timeout 7 ros2 topic hz /tb3_2/camera/image_raw'
# 기대: ~30 Hz
```

발행되는 토픽:
- `/tb3_2/camera/camera_info`
- `/tb3_2/camera/image_raw`
- `/tb3_2/camera/image_raw/compressed` ← Unity가 subscribe 대상

### B. ros_tcp_endpoint (Unity 다리)

```bash
ssh -o ControlMaster=no urhynix-robot '
  source /opt/ros/jazzy/setup.bash &&
  source ~/turtlebot3_ws/install/setup.bash 2>/dev/null
  export ROS_DOMAIN_ID=230
  nohup ros2 run ros_tcp_endpoint default_server_endpoint \
    --ros-args -p ROS_IP:=0.0.0.0 -p ROS_TCP_PORT:=10000 \
    > ~/ros_tcp.log 2>&1 < /dev/null & disown
'
```

검증:
```bash
ssh urhynix-robot 'ss -tln | grep 10000'
# 기대: LISTEN 0      10           0.0.0.0:10000
```

## 표준 launch — 티원 (RealSense D435)

```bash
ssh t1@192.168.0.250 '
  source /opt/ros/jazzy/setup.bash
  export ROS_DOMAIN_ID=230
  nohup ros2 launch realsense2_camera rs_launch.py \
    align_depth.enable:=true pointcloud.enable:=true \
    camera_namespace:=tb3_1 \
    > ~/rs_camera.log 2>&1 < /dev/null & disown
'
```

검증:
```bash
ssh t1@192.168.0.250 'source /opt/ros/jazzy/setup.bash && export ROS_DOMAIN_ID=230 && ros2 topic list | grep tb3_1'
# 기대 토픽:
#   /tb3_1/camera/camera/color/image_raw
#   /tb3_1/camera/camera/depth/image_rect_raw
#   /tb3_1/camera/camera/aligned_depth_to_color/image_raw
```

권한 함정 (티원 첫 셋업 시 1회): `sudo usermod -aG video,plugdev t1` (이후 sudo 없이 enumerate 가능).

## 함정 + 우회 표

| 함정 | 증상 | 우회 |
|---|---|---|
| ABI 충돌 (camera_ros vs Pi fork libcamera) | `FATAL Serializer ControlInfoMap required` | `LD_LIBRARY_PATH=/usr/local/lib/aarch64-linux-gnu` + `LIBCAMERA_IPA_MODULE_PATH=/usr/local/lib/aarch64-linux-gnu/libcamera` |
| ssh 비-인터랙티브 ROS env 누락 | `ros2: command not found`, `ROS_DOMAIN_ID=` 빈값 | `source /opt/ros/jazzy/setup.bash && source ~/turtlebot3_ws/install/setup.bash` 명시 |
| ssh ControlMaster connection 끊김 (255) | `Exit code 255` | `ssh -o ControlMaster=no` 강제 새 연결 |
| nohup 백그라운드 ssh 종료 시 죽음 | 노드 즉시 사라짐 | `nohup ... > log 2>&1 < /dev/null & disown` 패턴 |
| sudo 비번 stdin 필요 (apt install 등) | `[sudo] password for kim:` 멈춤 | `. ~/.tb3rc && printf '%s\n' "$TB3_PASSWORD" \| ssh urhynix-robot 'sudo -S apt ...'` |
| ROS_DOMAIN_ID drift (양쪽 다름) | `ros2 topic list`에 상대 토픽 안 보임 | `~/.bashrc`에서 `export ROS_DOMAIN_ID=230` 통일 |
| ros_tcp_endpoint launch 파일 이름 | `default_server_endpoint.launch.py was not found` | `ros2 run ros_tcp_endpoint default_server_endpoint` 직접 run (launch 아님) |
| `realsense2_camera` 권한 | `/dev/video* Permission denied` (non-root) | `sudo usermod -aG video,plugdev <user>` 후 재로그인 |
| `pkill -f realsense`가 ssh 자기 자신 죽임 | ssh exit 255 + 명령 누락 | `kill <PID>` 직접 또는 `pgrep -af "ros2 launch r"` 정확한 패턴으로 |
| `camera_namespace:=tb3_1`가 만드는 토픽 구조 — `/tb3_1/camera/color/...` (camera 한 번, 중복 아님) | Unity가 `/tb3_1/camera/camera/...` subscribe하면 publisher 0 | `ros2 topic list \| grep tb3_1`로 실제 발행되는 정확한 이름 확인 후 Unity topic 매칭 |
| `realsense2_camera`에 `compressed` plugin 자동 안 들어옴 | `/...image_raw/compressed` 토픽 발행 안 됨 (raw만) | `sudo apt install -y ros-jazzy-compressed-image-transport` 별도. 설치 후 realsense2_camera 재시작 |
| 티원 측 ssh 비번이 젠지와 다름 → 우리 자동화 불가 | `Permission denied (publickey,password)` | 1회 `ssh-copy-id -o StrictHostKeyChecking=accept-new t1@192.168.0.250` (비번 1회) → 영구 자동 |
| Pi Camera 1280×720@30Hz로 Unity 지연 1~2초 | Wi-Fi/buffer 누적 백로그 | camera_node 해상도 **640×480@30Hz** (frame 크기 1/4) → 지연 0.1~0.3초 실시간 |
| `urhynix-robot` IP 변경 (reboot 후 .82 → .150) | ssh urhynix-robot은 OK지만 .82 IP는 ping 안 됨 | mDNS `urhynix-robot.local` 사용 (자동 follow) + Unity rosIP도 mDNS 박아둠 |
| **13. Ubuntu 24.04 `KillUserProcesses=yes`로 nohup+disown까지 ssh 끊김 시 죽음** (2026-06-04 발견) | 백그라운드 ROS 노드 즉시 사라짐, `pgrep camera_node` 0건 | **1회 영구**: `sudo loginctl enable-linger kim` → `Linger=yes` 확인. 이후 tmux 세션 + 자식 프로세스 살아남 |
| **14. Unity 6.3 + ROS-TCP-Connector v0.7.x syscommand JSON `[:-1]` 호환 안 됨** (2026-06-04 발견) | robot `json.JSONDecodeError: Expecting ',' delimiter: line 1 column 91`. Unity socket shut down 반복 | **server.py:125 패치**: `data.decode("utf-8")[:-1]` → `data.decode("utf-8").rstrip("\x00").strip()`. src/ + build/ 둘 다 박고 colcon build 권장 |
| **15. macOS `setsid+nohup` Unity 시동이 LaunchServices attach 실패로 빨리 죽음** (2026-06-04 발견) | Unity 프로세스 28~41줄 log에서 즉시 종료. `ps -ef` 0건 | **`open -a` 사용**: `open -a "/Applications/Unity/Hub/Editor/6000.3.16f1/Unity.app" --args -projectPath ... -logFile ...` + `sleep 5` + `osascript -e 'tell application "Unity" to activate'` |
| **16. ★ Unity는 기본 ROS1 모드. ROS2 endpoint와 binary format 비대칭으로 OverflowException** (2026-06-04 발견) | RegisterSubscriber는 OK지만 frame deserialize 시 `OverflowException` + `ArgumentException: Offset and length were out of bounds`. Unity Console: `Incompatible protocol: ROS-TCP-Endpoint is using ROS2, but Unity is in ROS1 mode` | **GUI**: `Edit → Project Settings → Player → Other Settings → Scripting Define Symbols → "ROS2" 추가`. **직접 편집** (Editor 죽음 시 비상): `ProjectSettings.asset`의 `scriptingDefineSymbols:` 아래 `Standalone: ROS2` 박기. **신 Unity 프로젝트 첫 진입 시 무조건 첫 액션** |
| **17. Write/외부 에디터로 만든 신규 `.cs`는 `.meta` 미생성 → 어셈블리 누락** (2026-06-05 발견) | 같은 namespace의 다른 파일에서 `error CS0103: The name '<Class>' does not exist in the current context`. `Library/ScriptAssemblies/*.dll` mtime이 갱신 안 됨 | **unityctl**: `unityctl asset import --project <proj> --path Assets/Scripts/.../<file>.cs --json` → `.meta` 생성 + Asset Pipeline 등록 한 번에. **GUI**: Project 창 우클릭 → Refresh (`Cmd+R`). 자세히: `docs/evidence/2026-06-05-controlroom-dual-camera-toggle.md` 함정 #17 |
| **18. Play 모드 중에는 도메인 리로드 차단 → 새 코드 미적용** (2026-06-05 발견) | `unityctl asset refresh` + `RequestScriptCompilation` 호출해도 `Library/ScriptAssemblies/Assembly-CSharp*.dll` mtime 옛값 유지. `unityctl exec`로 메서드 호출 시 옛 코드 실행 | **5단계 표준**: `unityctl play stop` → settled 대기(`unityctl status` `Ready`) → `unityctl exec --code 'UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation()'` → assembly mtime 갱신 확인 → `unityctl exec --code '<Method>()'` → `unityctl play start`. 자세히: `docs/evidence/2026-06-05-controlroom-dual-camera-toggle.md` 함정 #18 |

## 다음 세션 진입 한 줄 (젠지 풀 launch)

```bash
. ~/.tb3rc && printf '%s\n' "$TB3_PASSWORD" | ssh -o ControlMaster=no urhynix-robot '
  source /opt/ros/jazzy/setup.bash && source ~/turtlebot3_ws/install/setup.bash 2>/dev/null
  export ROS_DOMAIN_ID=230
  export LD_LIBRARY_PATH=/usr/local/lib/aarch64-linux-gnu:$LD_LIBRARY_PATH
  export LIBCAMERA_IPA_MODULE_PATH=/usr/local/lib/aarch64-linux-gnu/libcamera
  pkill -f camera_node 2>/dev/null
  pkill -f ros_tcp_endpoint 2>/dev/null
  sleep 1
  nohup ros2 run camera_ros camera_node --ros-args -r __ns:=/tb3_2 -p width:=1280 -p height:=720 > ~/camera_node.log 2>&1 < /dev/null & disown
  nohup ros2 run ros_tcp_endpoint default_server_endpoint --ros-args -p ROS_IP:=0.0.0.0 -p ROS_TCP_PORT:=10000 > ~/ros_tcp.log 2>&1 < /dev/null & disown
  sleep 5
  pgrep -af "camera_node|ros_tcp_endpoint" | head -5
  ss -tln | grep 10000
'
```

기대 출력 (마지막 2줄):
```
12454 ... ros2 run camera_ros camera_node ...
12886 ... default_server_endpoint ...
LISTEN 0  10  0.0.0.0:10000  0.0.0.0:*
```

## helper script로 자동화 (선택)

`scripts/tb3-camera-up.sh` 생성 후 위 명령 박으면 한 줄 호출 가능:

```bash
bash scripts/tb3-camera-up.sh
```

## 검증 evidence

- `docs/evidence/2026-06-01-rpi-camera-imx219-source-build.md` (Pi Camera 빌드 + 캡처)
- `docs/evidence/2026-06-01-robot2-realsense-d435-ros2-smoke.md` (D435 ROS2 통합)
- `docs/evidence/2026-06-02-pi-camera-ros2-topic-30hz.md` (이 스킬 검증, 작성 예정)

## 한줄정리

박물관 시연 카메라 트랙(젠지 Pi Camera + 티원 D435 + ROS-TCP-Endpoint)을 매 세션 첫 5분에 한 번에 살리는 표준 패턴. **LD_LIBRARY_PATH 우회 + ROS env 명시 source + nohup/disown + ControlMaster=no** 4종 함정 모두 잡혀있음. 다음 세션 진입 한 줄로 카메라 ready 검증까지 끝.
