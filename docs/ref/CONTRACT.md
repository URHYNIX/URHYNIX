# URHYNIX 인터페이스 계약

> ROS 메시지 / DB 스키마 / Unity 인터페이스의 **정본**.
> 한쪽 변경이 다른 영역을 깨뜨릴 수 있는 모든 경계 약속을 여기에 모은다.
> 변경 시 반드시 PR에 이 문서 수정 포함. 합의 없이 임의 변경 금지.

## 1. ROS Topics (임현찬 ↔ 김선일 ↔ 김주영)

| Topic | Type | Publisher | Subscribers | 비고 |
|---|---|---|---|---|
| `/turtlebot/pose` | `geometry_msgs/PoseStamped` | `urhynix_bringup` (임현찬) | `urhynix_unity_bridge` (임현찬→김주영), `urhynix_logger` (김선일) | 10Hz |
| `/turtlebot/scan` | `sensor_msgs/LaserScan` | `urhynix_bringup` (LiDAR raw) | `urhynix_unity_bridge`, `urhynix_obstacle_detect` (김선일) | 10Hz |
| `/turtlebot/camera/image_raw` | `sensor_msgs/Image` | `urhynix_bringup` (Pi cam) | `urhynix_obstacle_detect`, `urhynix_unity_bridge` (Unity 표시용) | 30Hz |
| `/obstacle/detection` | `urhynix_msgs/Detection` (커스텀) | `urhynix_obstacle_detect` (김선일) | `urhynix_unity_bridge`, `urhynix_logger` | 5Hz |
| `/turtlebot/cmd_vel` | `geometry_msgs/Twist` | Nav2 / `urhynix_bringup` | TurtleBot HW | — |

### 커스텀 메시지: `urhynix_msgs/Detection`

```
# urhynix_msgs/msg/Detection.msg
std_msgs/Header header
string class_name              # "person", "box", "cone", ...
float32 confidence             # 0.0 ~ 1.0
geometry_msgs/Point bbox_min   # 이미지 좌표 (px)
geometry_msgs/Point bbox_max
geometry_msgs/PointStamped world_pose  # 로봇 기준 추정 위치 (m)
```

**변경 시:** 임현찬·김선일·김주영 3명 모두 합의 → PR에 메시지 정의 + 양쪽 구독 코드 동시 수정.

## 2. DB Schema (김선일 → 김주영 QA)

> 저장소: Supabase (또는 로컬 Postgres). 결정은 SCRUM-14 진행 시점.

### `drive_log` (주행 데이터)
| Column | Type | 비고 |
|---|---|---|
| `id` | BIGSERIAL PK | |
| `ts` | TIMESTAMPTZ | UTC, ms 단위 |
| `x` | DOUBLE | meters |
| `y` | DOUBLE | meters |
| `theta` | DOUBLE | radians |
| `speed` | DOUBLE | m/s |
| `session_id` | UUID | 한 회 주행 세션 묶음 |

### `detection_log` (인식 결과)
| Column | Type | 비고 |
|---|---|---|
| `id` | BIGSERIAL PK | |
| `ts` | TIMESTAMPTZ | drive_log.ts와 join 가능 |
| `session_id` | UUID | FK → drive_log.session_id |
| `class_name` | TEXT | |
| `confidence` | REAL | 0.0~1.0 |
| `bbox` | JSONB | `{minX, minY, maxX, maxY}` |
| `image_path` | TEXT | 원본 이미지 저장 경로 (선택) |

### `session_meta` (세션 메타)
| Column | Type | 비고 |
|---|---|---|
| `session_id` | UUID PK | |
| `started_at` | TIMESTAMPTZ | |
| `ended_at` | TIMESTAMPTZ | |
| `mode` | TEXT | "lidar_only" 또는 "lidar_vision" |
| `notes` | TEXT | 자유 메모 |

**변경 시:** 김선일이 마이그레이션 SQL 작성 → 김주영이 QA 쿼리 확인 → PR에 양쪽 포함.

## 3. Unity ↔ ROS Bridge (임현찬 ↔ 김주영)

> 라이브러리: ROS-TCP-Connector (Unity ↔ ROS-TCP-Endpoint).

| 방향 | Topic | Unity 측 처리 |
|---|---|---|
| ROS → Unity | `/turtlebot/pose` | `Assets/Scripts/Network/PoseSubscriber.cs` → TurtleBot 모델 Transform 갱신 |
| ROS → Unity | `/obstacle/detection` | `Assets/Scripts/UI/Detection/DetectionPanel.cs` → 화면 오버레이 |
| ROS → Unity | `/turtlebot/camera/image_raw` | `Assets/Scripts/UI/Camera/CameraStream.cs` → RawImage 표시 |
| Unity → ROS | (선택) `/unity/cmd` | 수동 정지/리셋 명령 |

**변경 시:** 임현찬이 ROS 측 publisher 변경 → 김주영이 Unity 측 deserializer 갱신 → PR에 양쪽 포함.

## 4. 슬랙 / 외부 채널

| 채널 | ID | 용도 |
|---|---|---|
| URHYNIX 팀 | `C0B5Q43A27R` | daily-recap 자동 발송, PR 알림, 충돌 합의 |
| Jira | SCRUM 프로젝트 | 작업 트래킹 |

## 5. 변경 절차

```
1. 변경 제안 → 슬랙에서 영향받는 사람 호출
2. CONTRACT.md draft PR 열기 (제목: "contract: <변경 요지>")
3. 영향받는 owner 전원 승인
4. 머지 후 각자 코드 변경 PR 따로 진행
5. 머지 완료 후 DECISION-LOG.md에 1줄 기록
```

## 한줄정리
ROS 토픽 5종 + DB 테이블 3종 + Unity bridge 3종 — 한 줄 바뀌면 다른 영역 깨질 수 있어서 모든 변경은 이 문서 동시 갱신을 PR 승인 조건으로 한다.
