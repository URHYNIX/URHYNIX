# Project Plan

> 디지털트윈경비로봇 — 7주 / 4 스프린트
> 모든 카드는 JIRA-MAP.md의 SCRUM ID와 1:1 매칭. 담당자는 공동(최대 3명)까지 허용한다.

## Intake Verdict

- verdict: `doc-sync`
- chosen skill: `task-intake-router`
- next skill: `decision-broadcast`, `ssot-board-sync`, `evidence-review`
- sub-agent needed: no
- reasoning: 박물관/미술관 액자 보호 컨셉, 카메라 중요물품 인식, LiDAR/PIR 외부자 판단, 조도 기반 LiDAR 강화, 좌표·사진·영상·사운드 DB 저장 요구를 로컬 SSOT와 Jira/Confluence에 동기화하는 요청이다.

## 병렬 작업 원칙

> **한 모듈 안에서는 순차, 모듈 간에는 병렬.** 직렬 대기 시간을 최소화하기 위해 각자의 모듈은 인터페이스(`CONTRACT.md`)만 합의된 상태에서 동시에 진행한다.

### 직렬 병목 (먼저 끝내야 다음이 가능)

- **SCRUM-8 합의 (1일)** → 모든 작업의 전제 조건
- **SCRUM-16 트랙 환경** → SCRUM-10 SLAM/Nav2
- **SCRUM-13 센서+이벤트 발행** → SCRUM-12 출동 / SCRUM-21 AI 분류
- **SCRUM-14 DB 스키마** → SCRUM-23 저장 확장(좌표 로그·미디어·보호 대상) / SCRUM-15 KPI 쿼리
- **SCRUM-19 카메라 스트림** → SCRUM-25 라이브 / SCRUM-23 이미지 저장
- **SCRUM-16 박물관/미술관 waypoint** → SCRUM-21 액자형 중요물품 인식 / SCRUM-24 발표 컷

### 주차 × 모듈 병렬 매트릭스 (7주)

| 주차 | M1 백엔드/AI | M2 아두이노 메인 | M3 Unity | M4 센서 | M5 터틀봇 | 공통 |
|---|---|---|---|---|---|---|
| **W1** | SCRUM-14 스키마 초안 | 키트 점검·핀 배선 도면 | SCRUM-9 UI 초안 | 회로 도면 작성 | — | SCRUM-8 합의(1일), SCRUM-16 트랙 |
| **W2** | (SCRUM-14 계속) + `protected_assets` 예정 스키마 초안 | 적층 부품 발주 (스페이서·모듈) | (SCRUM-9 계속) | 센서 도착·동작 확인 | SCRUM-10 tb3_1 SLAM | 박물관/미술관 waypoint 초안 |
| **W3** | pose log 설계 | SCRUM-13 브릿지 노드 | SCRUM-11 pose+마커 | SCRUM-13 회로/펌웨어 | SCRUM-20 timestamp sync | 액자형 사진 타깃 설치 |
| **W4** | media path 정책 | (SCRUM-13 계속) | SCRUM-22 야간모드 UI + LiDAR 강화 표시 | (SCRUM-13 계속) | SCRUM-19 Pi Camera | 보호 대상 카메라 확인 |
| **W5** | SCRUM-23 저장 확장(좌표·사진·영상·사운드) | — | SCRUM-25 라이브 스트림 | SCRUM-17 추가 센서 | SCRUM-12 tb3_2 출동 | — |
| **W6** | SCRUM-21 AI 분류 + 액자 인식 라벨 | — | (SCRUM-25 계속) | (SCRUM-17 계속) | (SCRUM-12 계속) | — |
| **W7** | SCRUM-15 지표 시연 | — | SCRUM-24 발표 영상 | — | — | SCRUM-18 시연 환경 |

### S1 W1 Day-1 (2026-05-27 확정) — 즉시 시작 3팀

| 팀 | 인원 | 작업 | 관련 SCRUM | 오늘 끝 산출물 |
|---|---|---|---|---|
| **Pi+DB팀** | 김주영, 임현찬 | Pi Camera 스트림 확인 + DB 연결 테스트 (Supabase 또는 로컬 PG) | SCRUM-19, SCRUM-14 | `/tb3_1/camera/image_raw` 토픽 확인 영상 + `events` sample insert |
| **PIR+DB팀** | 박태진 | Arduino + PIR 센서값 → 시리얼 → DB insert까지 한 줄 통과 | SCRUM-13, SCRUM-14 | Arduino `.ino` + DB insert 로그 |
| **Unity 문서팀** | 김선일 | Unity 관제 UI 기능 정의 문서 (UI 명세) | SCRUM-9, SCRUM-22 | UI 기능 명세 1장 (운영 대시보드 / 이벤트 패널 / 카메라 패널 / 모드 토글) |

**Day-1 통합 검증 라인**: 박태진의 Arduino+PIR이 발행한 이벤트가 김주영·임현찬의 DB `events` 테이블에 한 줄 저장되면 Day-1 PASS.

### S1 W1 잔여 (Day-2 이후)

| 모듈 | 작업 | 담당 |
|---|---|---|
| SCRUM-8 합의 마무리 | 5 모듈 매트릭스 + Day-1 결과 공유 | 김주영 |
| SCRUM-16 실내 트랙 | 야간 광량 환경 + waypoint 측정 | 박태진·임현찬 |
| 부품 발주 | 소리/불꽃 센서 (키트 미포함분), 짧은 USB-B 케이블 | 박태진 |
| OpenCR 5V 패드 도면 | 점퍼 배선 위치 실측 후 회로도 | 임현찬·김주영 |

## Sprint 1 — 단일 로봇 베이스라인 (2주)

목표: tb3_1 한 대로 순찰 주행과 Unity pose 표시까지 라인을 통하게 만든다. 모든 후속 작업의 토대.

| 작업 | Jira | 담당 | 모듈 |
|---|---|---|---|
| MVP 범위·역할 매트릭스·SSOT 합의 | SCRUM-8 | 김주영 · 김선일 | 공통 |
| Unity 박물관/미술관 모델 + 듀얼 로봇 표시 가능한 관제 UI 초안 | SCRUM-9 | 김선일 · 박태진 | M3 |
| tb3_1 SLAM/Nav2 기본 순찰 주행 | SCRUM-10 | 임현찬 · 김선일 | M5 |
| 실내 트랙·박물관/미술관 경비 구역·야간 모드 환경 세팅 | SCRUM-16 | 박태진 · 임현찬 | 공통 |
| DB 스키마 초안 (`events`, `dispatches`, `camera_captures`, `session_meta`) | SCRUM-14 | 김주영 · 김선일 | M1 |

## Sprint 2 — 센서 + 이벤트 1종 (2주)

목표: 아두이노 센서 1종으로 이벤트를 발행해 Unity 마커까지 띄운다. "감지→표시" 한 줄을 통과.

| 작업 | Jira | 담당 | 모듈 |
|---|---|---|---|
| 아두이노 센서 노드 1종(PIR 또는 조도) 연동 + `/security/event` 발행 | SCRUM-13 | 김주영 · 임현찬 · 박태진 | M4 |
| Pi Camera 설치·스트림 토픽 발행 | SCRUM-19 | 박태진 · 임현찬 | M3 / M5 |
| tb3_1 pose ↔ 센서 이벤트 timestamp 동기화 | SCRUM-20 | 임현찬 · 김선일 | M5 |
| Unity 로봇 위치/경로/이벤트 마커 표시 | SCRUM-11 | 김선일 · 박태진 | M3 |
| 야간 모드 / 이벤트 패널 / 운영 대시보드 UI | SCRUM-22 | 김선일 · 박태진 | M3 |

## Sprint 3 — DB + 2호기 출동 시뮬 (2주)

목표: 모든 이벤트가 DB에 저장되고, tb3_2가 (시뮬레이션 또는 실기) 출동해 카메라로 확인하는 흐름을 완성.

| 작업 | Jira | 담당 | 모듈 |
|---|---|---|---|
| tb3_2 출동 로직 (이벤트 수신 → Nav2 goal 발행) | SCRUM-12 | 임현찬 · 김선일 | M5 |
| 추가 센서(소리/불꽃) 회로 + 모의 입력 인터페이스 | SCRUM-17 | 김주영 · 임현찬 · 박태진 | M4 |
| 라벨링 + AI 오탐/실탐 분류 보조 모델 + 액자형 중요물품 인식 | SCRUM-21 | 김주영 · 김선일 | M1 |
| 이벤트·이미지·대응 시간·이동좌표·영상·사운드 저장 구조 확장 | SCRUM-23 | 김선일 · 김주영 | M1 |
| Pi Camera 라이브 스트리밍 → Unity 패널 | SCRUM-25 | 김선일 · 박태진 | M3 |

## Sprint 4 — 2대 실기 확장 + 발표 데모 (1주)

목표: 시나리오 4종을 시연하고 발표 지표 표를 만든다. 가능하면 2대 실기 동시 주행.

| 작업 | Jira | 담당 | 모듈 |
|---|---|---|---|
| 시나리오 4종(야간/PIR/소리/화재 모의) + 박물관/미술관 액자 보호 통합 시연 + 지표 표 | SCRUM-15 | 김주영 · 김선일 | M1 |
| 최종 시연 환경(2대 동시 구동, 백업 부품, 광량) 준비 | SCRUM-18 | 박태진 · 임현찬 | 공통 |
| 발표용 화면/영상/시나리오 컷 캡처 | SCRUM-24 | 박태진 · 김선일 | M3 |

## 추천 진행 순서

1. SSOT 합의 + Jira 재라벨링 (S1 시작 전 1~2일)
2. 박물관/미술관 실내 트랙·액자형 보호 대상 waypoint 고정
3. tb3_1 SLAM/Nav2 순찰 베이스라인
4. Unity pose + 듀얼 로봇 표시 준비
5. 센서 연결 방식 스파이크 (Arduino 보드→Raspberry Pi USB serial / OpenCR GPIO·ADC / Raspberry Pi GPIO·I2C·UART 비교)
6. 아두이노 센서 1종 + `/security/event` 발행
7. Unity 이벤트 마커 + 카메라 패널
8. DB 저장 (현재: events → dispatches → camera_captures, SCRUM-23 예정: pose_logs/media_artifacts/protected_assets)
9. tb3_2 출동 시뮬
10. 추가 센서 + AI 분류 + 액자형 중요물품 인식
11. 2대 실기 확장 + 시나리오 4종 시연 + 발표 지표

## Open Questions

- (모두 해결됨) 아두이노 센서 연결 방식은 2026-05-27 결정으로 **Arduino Uno R3 + 미니 브레드보드 → Raspberry Pi USB serial**로 확정. OpenCR GPIO/ADC 및 RPi GPIO 직접 연결 후보는 폐기. 상세는 `docs/status/DECISION-LOG.md` "센서 연결·적층 구조 확정" 항목 참조.

## 축소안 (일정 지연 시)

- S3 종료 시점에 v4 어렵다고 판단되면 **2대 실기 → 1대 실기 + 1대 Unity 시뮬레이션**으로 축소.
- 시나리오 4종 중 안정성 낮은 화재/소리 항목은 모의 입력만 시연, 발표에서 명시.
- 라벨링/AI 분류(SCRUM-21)는 발표 직전 1일에 단순 임계값 비교로 대체 가능.
