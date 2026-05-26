# URHYNIX Unity Source

URHYNIX 디지털 트윈 Unity 프로젝트. 원본은 `/Users/family/jason/FR5UNITY/robotapp` (FR5 로봇팔 제어 프로젝트)을 2026-05-26 fork 한 뒤 로봇팔 자산을 제거한 형태.

## 출처
- 원본: `/Users/family/jason/FR5UNITY/robotapp` (**원본은 절대 수정 금지**)
- fork 일자: 2026-05-26
- fork 방식: `rsync -a --exclude="Library/" --exclude="Artifacts/" --exclude="Temp/" ...` (빌드 캐시 제외)

## 제거된 자산 (2026-05-26)
| 카테고리 | 경로 |
|---|---|
| 로봇팔 프리팹 | `Assets/Runtime/Robots/{FAIRINO_FR5, DOOSAN_M1013, MECA500, UR5e}` |
| Resources/Robots | `Assets/Runtime/Resources/Robots/FAIRINO_FR5*` |
| 그리퍼 | `Assets/Runtime/EndEffectors/PGEA_100_40` |
| Fairino SDK | `Assets/Plugins/Fairino`, `Assets/Scripts/App/Fairino` |
| Kinematics | `Assets/Scripts/Kinematics`, `Assets/Scripts/App/Runtime/*Kinematics*` |
| TCP Jog | `Assets/Scripts/UI/RobotControlV3/TcpJogController*` |
| FR5 씬/핸드오프 | `Assets/Scenes/FR5_Template_Demo.unity`, `Assets/Handoff/FR5_*` |
| FR5/Fairino 테스트 | `Assets/Tests/EditMode/{Integration/Fairino*, Core/*FR5*}` |
| 임시 파일 | `mono_crash.*.json`, `*.log`, `.git/`, `.github/` |

## 재활용 대상 (URHYNIX에서 활용)
- `Assets/Scripts/Visualization/Renderer/RobotRenderer.cs` — TurtleBot3 pose 실시간 표시
- `Assets/Scripts/Visualization/Shared/OrbitCameraController.cs` — 3D 뷰 카메라
- `Assets/Scripts/Visualization/Shared/FrameGizmo.cs` — 좌표계 시각화
- `Assets/Runtime/Prefabs/Teaching/` — UI 패널 구조
- `Assets/Scripts/UI/RobotControlV3/` (TcpJog 제외) — 상태 표시 패널 아키텍처

## 새로 작성해야 할 영역
1. **ROS-TCP 브릿지** — TurtleBot3 `/pose`, `/scan`, `/odom`, `/cmd_vel` 토픽 수신/송신
2. **LiDAR 시각화** — 점군 또는 occupancy grid 렌더링
3. **카메라 영상 표시 패널** — TurtleBot3 라즈베리 카메라 스트림
4. **장애물 인식 결과 오버레이** — 바운딩박스 + 클래스 라벨
5. **DB 기록 연동** — Supabase 또는 Postgres로 주행 데이터 push

## 다음 단계
1. Unity Editor (2022.3 LTS 권장)에서 `URHYNIX/unity-src/` 열기
2. 컴파일 에러 확인 — 제거된 Fairino/Kinematics를 참조하는 잔존 코드 fix
3. `Assets/Runtime/Robots/Common/` 잔존 여부 확인 후 TurtleBot3 모델 추가
4. ROS-TCP-Connector 패키지 설치 (`Packages/manifest.json` 수정)

## 주의사항
- **FR5UNITY 원본은 절대 수정하지 말 것.** 변경/실험은 모두 이 `unity-src/` 디렉토리에서만.
- Unity 빌드 캐시(`Library/`, `Temp/`, `Obj/`)는 `.gitignore` 처리됨.
- `Library/`는 Unity Editor 첫 실행 시 자동 재생성됨 (10~30분 소요 가능).
