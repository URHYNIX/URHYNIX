# Decision Log

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
