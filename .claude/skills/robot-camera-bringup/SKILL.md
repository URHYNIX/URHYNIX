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
