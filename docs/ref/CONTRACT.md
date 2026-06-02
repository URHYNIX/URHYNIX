# URHYNIX 인터페이스 계약

> ROS 메시지 / DB 스키마 / Unity 인터페이스 / 아두이노 시리얼 약속.
> 모든 토픽은 `robot_id`를 포함하거나 토픽 네임스페이스로 구분한다.
> 변경 시 영향받는 owner 전원 합의 후 PR로 반영.

## 1. ROS Topics

### 1.1 로봇별 토픽 (네임스페이스로 구분)

| Topic | Type | Publisher | Subscribers | Hz | 비고 |
|---|---|---|---|---|---|
| `/tb3_1/pose` | `geometry_msgs/PoseStamped` | `urhynix_bringup_1` (임현찬) | unity_bridge, db_writer | 10 | tb3_1 위치 |
| `/tb3_1/scan` | `sensor_msgs/LaserScan` | `urhynix_bringup_1` | unity_bridge, nav2 | 10 | LiDAR raw |
| `/tb3_1/camera/image_raw` | `sensor_msgs/Image` | `urhynix_bringup_1` (Pi cam) | unity_bridge | 15 | 다운샘플 후 |
| `/tb3_1/cmd_vel` | `geometry_msgs/Twist` | Nav2 | TurtleBot HW | — | |
| `/tb3_2/pose` | `geometry_msgs/PoseStamped` | `urhynix_bringup_2` (임현찬) | unity_bridge, db_writer | 10 | tb3_2 위치 |
| `/tb3_2/scan` | `sensor_msgs/LaserScan` | `urhynix_bringup_2` | unity_bridge, nav2 | 10 | |
| `/tb3_2/camera/image_raw` | `sensor_msgs/Image` | `urhynix_bringup_2` (Pi cam) | unity_bridge, camera_confirm | 15 | tb3_2 확인용 |
| `/tb3_2/cmd_vel` | `geometry_msgs/Twist` | Nav2 | TurtleBot HW | — | |
| `/map` | `nav_msgs/OccupancyGrid` | `cartographer_node` (Robot 자체) | nav2, unity_bridge, map_saver_cli | 1.0 | SLAM 점유 그리드. cartographer가 robot 라즈베리파이에서 실행 (Mac Docker host networking inbound NAT 부적합, 2026-05-29 진단). 정적 매핑 시 30s 누적 후 `nav2_map_server map_saver_cli`로 `.pgm + .yaml` 저장. |
| `/tf` | `tf2_msgs/TFMessage` | `urhynix_bringup_*` (turtlebot3_node) | cartographer, nav2, unity_bridge | 30+ | base_footprint↔odom↔map 트리. cartographer가 `map↔odom` 추정 발행. |
| `/tb3_1/odom` | `nav_msgs/Odometry` | `urhynix_bringup_1` (turtlebot3_node) | cartographer, nav2, unity_bridge | 30 | OpenCR 휠 엔코더 기반. |
| `/tb3_1/battery_state` | `sensor_msgs/BatteryState` | `urhynix_bringup_1` | unity_bridge | 1 | LiPo 전압 모니터링. |

> **ROS2 환경변수 모드 통일 (필수)**: 모든 노드는 같은 도메인(`ROS_DOMAIN_ID=230`) + 같은 RMW(`rmw_fastrtps_cpp`) + 같은 발견 모드를 사용해야 통신 가능. 본 프로젝트 기본은 **multicast (`ROS_AUTOMATIC_DISCOVERY_RANGE=SUBNET`)** 단일 모드. Discovery Server(`ROS_DISCOVERY_SERVER`) 혼용 금지 — 한쪽이라도 다르면 같은 도메인이라도 발견 실패. **2026-06-02 정정**: 티원이 230으로 작동 중이라 젠지도 230으로 통일(이전 56/30 표기는 16GB SD 시점/신규 SD 초기값 — DECISION-LOG 2026-06-02 참조). 검증: `cat /proc/$(pgrep -f turtlebot3_node)/environ \| tr '\0' '\n' \| grep ROS_`.

### 1.2 보안 이벤트 토픽 (공통 네임스페이스, robot_id 포함)

| Topic | Type | Publisher | Subscribers | Hz | 비고 |
|---|---|---|---|---|---|
| `/security/event` | `urhynix_msgs/SecurityEvent` | `urhynix_sensor_bridge` (센서 인터페이스) | dispatcher, unity_bridge, db_writer | 이벤트 단발 | 감지 발행 |
| `/security/dispatch` | `urhynix_msgs/Dispatch` | `urhynix_dispatcher` | tb3_2 nav node, unity_bridge, db_writer | 이벤트 단발 | tb3_2 출동 명령 |
| `/security/camera_confirm` | `urhynix_msgs/CameraConfirm` | `urhynix_camera_confirm` (tb3_2) | unity_bridge, db_writer | 도착 시 단발 | 카메라 캡처 결과 |

### 1.3 커스텀 메시지

```text
# urhynix_msgs/msg/SecurityEvent.msg
std_msgs/Header header
string robot_id              # "tb3_1" | "tb3_2"
string event_type            # current DB: "dark" | "pir" | "noise" | "fire"; planned: "asset_seen" | "asset_missing" | "camera_confirm"
uint8 severity               # 0=info, 1=low, 2=medium, 3=high
geometry_msgs/PoseStamped pose  # 이벤트 감지 시점 로봇 pose
string session_id            # UUID
```

```text
# urhynix_msgs/msg/Dispatch.msg
std_msgs/Header header
string source_event_id       # SecurityEvent에 부여된 UUID
string target_robot_id       # 보통 "tb3_2"
geometry_msgs/PoseStamped target_pose
string session_id
```

```text
# urhynix_msgs/msg/CameraConfirm.msg
std_msgs/Header header
string dispatch_id           # Dispatch UUID
string robot_id              # "tb3_2"
string image_path            # 저장 경로 (로컬 또는 Supabase Storage URL)
string protected_asset_id    # "frame_01" 등, 없으면 ""
string result                # "confirmed" | "false_alarm" | "missed" | "unverified"
float32 response_time        # 이벤트 발생 → 도착까지 초
string session_id
```

**변경 시:** M1·M3·M5 owner 합의 → PR에 메시지 정의 + 양쪽 코드 동시 수정.

## 2. DB Schema

> 저장소: Supabase (기본) 또는 로컬 Postgres. `docs/ref/SCHEMA.md`가 정본 — 본 섹션은 요약.

### `events`
| Column | Type | 비고 |
|---|---|---|
| `id` | UUID PK | SecurityEvent에 부여 |
| `session_id` | UUID FK → session_meta | |
| `robot_id` | TEXT | `tb3_1` 등 |
| `ts` | TIMESTAMPTZ | UTC ms |
| `event_type` | TEXT | 현재 DB: `dark` `pir` `noise` `fire`; SCRUM-23 예정: `asset_seen` `asset_missing` |
| `severity` | SMALLINT | 0~3 |
| `x`, `y`, `theta` | DOUBLE | pose |
| `raw_payload` | JSONB | 센서 원본값 |

### `dispatches`
| Column | Type | 비고 |
|---|---|---|
| `id` | UUID PK | |
| `event_id` | UUID FK → events | |
| `target_robot_id` | TEXT | |
| `target_x`, `target_y` | DOUBLE | |
| `dispatched_at` | TIMESTAMPTZ | |
| `arrived_at` | TIMESTAMPTZ | nullable |
| `response_time` | NUMERIC | 초 |

### `camera_captures`
| Column | Type | 비고 |
|---|---|---|
| `id` | UUID PK | |
| `dispatch_id` | UUID FK → dispatches | |
| `robot_id` | TEXT | |
| `ts` | TIMESTAMPTZ | |
| `image_path` | TEXT | |
| `result` | TEXT | `confirmed`/`false_alarm`/... |
| `ai_label` | TEXT | nullable, M1 분류 모델 결과 |
| `ai_confidence` | REAL | nullable 0~1 |
| `protected_asset_id` | TEXT | nullable, 액자형 사진 타깃 ID |

### `pose_logs` (SCRUM-23 확장 예정, 현재 DB 미적용)
| Column | Type | 비고 |
|---|---|---|
| `session_id` | UUID FK → session_meta | |
| `robot_id` | TEXT | `tb3_1` / `tb3_2` |
| `ts` | TIMESTAMPTZ | |
| `x`, `y`, `theta` | DOUBLE | 이동 좌표 로그 |
| `nav_mode` | TEXT | `patrol`/`dispatch`/`lidar_boost`/`manual` |

### `media_artifacts` (SCRUM-23 확장 예정, 현재 DB 미적용)
| Column | Type | 비고 |
|---|---|---|
| `session_id` | UUID FK → session_meta | |
| `event_id`, `dispatch_id` | UUID FK | nullable |
| `media_type` | TEXT | `image`/`video`/`audio` |
| `storage_path` | TEXT | Supabase Storage URL 또는 로컬 경로 |
| `duration_sec` | NUMERIC | 영상/사운드 길이 |

### `protected_assets` (SCRUM-21/23 확장 예정, 현재 DB 미적용)
| Column | Type | 비고 |
|---|---|---|
| `asset_id` | TEXT PK | 예: `frame_01` |
| `session_id` | UUID FK → session_meta | |
| `name`, `asset_type` | TEXT | 액자형 사진 타깃 또는 중요물품 |
| `x`, `y` | DOUBLE | 전시 위치 |
| `marker_type`, `marker_value` | TEXT | AprilTag/QR/프레임 색상/수동 라벨 |

### `session_meta`
| Column | Type | 비고 |
|---|---|---|
| `session_id` | UUID PK | |
| `started_at` | TIMESTAMPTZ | |
| `ended_at` | TIMESTAMPTZ | |
| `scenario` | TEXT | `night_patrol`/`intrusion`/`noise`/`fire_mock`/`museum_guard`/`gallery_fire`/`mixed` |
| `notes` | TEXT | 자유 메모 |

**변경 시:** M1 owner가 마이그레이션 SQL 작성 → M3 owner가 Unity 조회 쿼리 확인 → PR에 양쪽 포함.

## 3. Unity ↔ ROS Bridge (M3 ↔ M5)

> 라이브러리: ROS-TCP-Connector (Unity ↔ ROS-TCP-Endpoint).

| 방향 | Topic | Unity 측 처리 |
|---|---|---|
| ROS → Unity | `/tb3_1/pose`, `/tb3_2/pose` | `Assets/Scripts/Network/PoseSubscriber.cs` → 듀얼 로봇 Transform |
| ROS → Unity | `/security/event` | `Assets/Scripts/UI/Events/EventMarker.cs` → 지도 마커 |
| ROS → Unity | `/security/dispatch` | `Assets/Scripts/UI/Events/DispatchPanel.cs` → 출동 상태 패널 |
| ROS → Unity | `/security/camera_confirm` | `Assets/Scripts/UI/Camera/CameraStream.cs` → 결과 패널 + 보호 대상 상태 |
| ROS → Unity | `/tb3_2/camera/image_raw` | `Assets/Scripts/UI/Camera/LiveStream.cs` → RawImage 라이브 스트림 |
| Unity → ROS | (선택) `/unity/manual_dispatch` | 운영자 수동 출동 명령 |

**변경 시:** M5 publisher 변경 → M3 deserializer 갱신 → PR에 양쪽 포함.

## 4. 센서 인터페이스 ↔ ROS 브릿지 (M2 ↔ M4) — 확정

**로봇**: TurtleBot3 Burger
**MCU**: Arduino Uno R3 (별도 층 추가 X, RPi 옆 빈 공간 양면테이프 부착)
**데이터 경로**: `센서 4종 → 미니 브레드보드 → Arduino Uno → USB Type-B → Raspberry Pi → urhynix_sensor_bridge (pyserial) → /security/event`
**전원 경로**: `OpenCR 5V 핀 → 점퍼 2줄 (5V + GND) → Arduino 5V 핀` (Vin 아님). AA 배터리 등 별도 전원 추가하지 않음.

### 4.1 핀 / 회로

| 센서 | Arduino 핀 | 회로 |
|---|---|---|
| PIR (HC-SR501) | D2 (디지털) | 5V/GND + 모듈 OUT → D2 |
| 조도 (LDR) | A0 (아날로그) | `5V─[LDR]─A0─[10kΩ]─GND` 분압 |
| 소리 (KY-038) | D3 | 5V/GND + D-out → D3 |
| 불꽃 (Flame) | D4 | 5V/GND + D-out → D4 |
| 모의 입력 버튼 (화재) | D5 | `INPUT_PULLUP`, GND로 풀다운 |

브레드보드 공통 5V/GND 레일 → Arduino 5V/GND 핀. 모든 센서의 GND는 단일 레일.

### 4.2 시리얼 프로토콜

- 보드레이트: **115200** (Arduino, RPi 양쪽 동일)
- 줄 단위 ASCII: `EVT,<type>,<severity>,<unix_ts>\n`
- 예: `EVT,pir,3,1716800000\n`
- 타입: `pir` / `dark` / `noise` / `fire` / `fire_mock`
- severity: `0`(info) ~ `3`(high)

### 4.3 보드 ↔ robot_id 매핑

| 보드 식별자 | 호스트 RPi | robot_id (브릿지 파라미터로 주입) |
|---|---|---|
| Arduino Uno (tb3_1 위 적층) | tb3_1 Raspberry Pi | `tb3_1` |
| Arduino Uno (tb3_2 위 적층) | tb3_2 Raspberry Pi | `tb3_2` |

- 시리얼 포트는 자동 `/dev/ttyACM0` 또는 `/dev/ttyACM1`. udev rule 또는 by-id 경로로 고정 권장.
- 임계값(조도/소리/불꽃)은 브릿지 노드 파라미터로 런타임에 주입, 시연 직전 캘리브.
- 조도 이벤트가 `dark`로 진입하면 브릿지/dispatcher는 `lidar_boost` 상태를 기록한다. 구현상 LiDAR 전원을 새로 켜는 것이 아니라 저속 순찰, 스캔 로그 저장, pose 로그 빈도 증가로 "라이다 강화"를 표현한다.
- 외부자 판단은 PIR 단독 이벤트로 확정하지 않고, PIR + LiDAR 거리 변화 + pose 로그를 함께 DB에 남긴다.

**변경 시:** M2·M4 owner 합의 → Arduino 펌웨어(`mcu_tb3_*.ino`) + 브릿지 노드 PR 동시.

## 5. 슬랙 / 외부 채널

| 채널 | ID | 용도 |
|---|---|---|
| URHYNIX 팀 | `C0B5Q43A27R` | daily-recap 자동 발송, PR 알림, 충돌 합의 |
| Jira | SCRUM 프로젝트 | 작업 트래킹 — `docs/ref/JIRA-MAP.md` 참조 |
| Confluence 1540099 | 브레인스토밍 (방향 결정 근거) | 참조 전용 |
| Confluence 1605636 | 역할 분배 보드 | 사람·모듈 매트릭스 정합 체크 |

## 6. 변경 절차

```
1. 변경 제안 → 슬랙에서 영향받는 owner 호출
2. CONTRACT.md draft PR (제목: "contract: <변경 요지>")
3. 영향받는 owner 전원 승인
4. 머지 후 각자 코드 변경 PR 따로 진행
5. 머지 완료 후 DECISION-LOG.md에 1줄 기록
```

## 한줄정리
듀얼 로봇 토픽 8종 + 보안 이벤트 토픽 3종 + DB 테이블 4종 + Unity bridge 6종 + 아두이노 시리얼 — 한 줄 바뀌면 다른 영역 깨질 수 있어서 모든 변경은 이 문서 동시 갱신을 PR 승인 조건으로 한다.
