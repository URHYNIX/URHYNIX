# Jira Map

> SCRUM 프로젝트 (https://jason1127.atlassian.net/jira/software/projects/SCRUM)
> ID는 그대로 유지하고, 제목·담당자·Sprint·모듈만 다중 경비 로봇 방향에 맞춰 재할당했다.
> 실제 Jira 본문 갱신은 `editJiraIssue`로 일괄 처리.

## Epic

- `SCRUM-7`: [UR HYNIX] 디지털트윈경비로봇 — 박물관/미술관 액자 보호 + 듀얼 TurtleBot 이벤트 응답 + 관제 + DB

## 모듈 범례

- **M1** 백엔드 DB / ROS-TCP 라벨링 / AI (김주영·김선일)
- **M2** 아두이노 메인 보드·통신 (박태진·임현찬·김주영)
- **M3** 유니티 관제UI · ROS-TCP 통신 · 영상 라이브 (김선일·박태진)
- **M4** 아두이노 센서 (김주영·임현찬·박태진)
- **M5** 터틀봇 LiDAR · 카메라 · SLAM · Nav (임현찬·김선일)

## 카드 매핑 (SCRUM-8 ~ SCRUM-25)

| ID | 새 제목 | 담당 | 모듈 | Sprint |
|---|---|---|---|---|
| SCRUM-8 | MVP 범위·역할 매트릭스·SSOT 합의 (디지털트윈경비로봇 방향 전환) | 김주영 · 김선일 | 공통 | S1 |
| SCRUM-9 | Unity 박물관/미술관 모델 + 듀얼 로봇 표시 가능한 관제 UI 초안 | 김선일 · 박태진 | M3 | S1 |
| SCRUM-10 | tb3_1 SLAM/Nav2 기본 순찰 주행 베이스라인 | 임현찬 · 김선일 | M5 | S1 |
| SCRUM-11 | Unity에 tb3_1 위치·경로·이벤트 마커 표시 | 김선일 · 박태진 | M3 | S2 |
| SCRUM-12 | tb3_2 출동 로직 — `/security/dispatch` 수신 → Nav2 goal | 임현찬 · 김선일 | M5 | S3 |
| SCRUM-13 | 아두이노 센서 1종(PIR 또는 조도) 연동 + `/security/event` 발행 | 김주영 · 임현찬 · 박태진 | M4 | S2 |
| SCRUM-14 | DB 스키마 초안 + `events` 테이블 마이그레이션 + 확장 테이블 계획 | 김주영 · 김선일 | M1 | S1 |
| SCRUM-15 | 박물관/미술관 액자 보호 시나리오 4종 통합 시연 + 발표 지표 표 | 김주영 · 김선일 | M1 | S4 |
| SCRUM-16 | 실내 트랙 + 박물관/미술관 경비 구역 + 야간 모드 환경 세팅 | 박태진 · 임현찬 | 공통 | S1 |
| SCRUM-17 | 추가 센서(소리·불꽃 모의) 회로 + 임계값 캘리브레이션 | 김주영 · 임현찬 · 박태진 | M4 | S3 |
| SCRUM-18 | 최종 시연 환경 — 2대 동시 구동 + 백업 부품 + 광량 준비 | 박태진 · 임현찬 | 공통 | S4 |
| SCRUM-19 | Pi Camera 설치 + `/tb3_*/camera/image_raw` 스트림 발행 | 박태진 · 임현찬 | M3 / M5 | S2 |
| SCRUM-20 | tb3_1 pose ↔ 센서 이벤트 timestamp 동기화 | 임현찬 · 김선일 | M5 | S2 |
| SCRUM-21 | 라벨링 + AI 오탐/실탐 분류 보조 모델 + 액자형 중요물품 인식 | 김주영 · 김선일 | M1 | S3 |
| SCRUM-22 | 야간 모드 / 이벤트 패널 / 운영 대시보드 UI | 김선일 · 박태진 | M3 | S2 |
| SCRUM-23 | 이벤트·좌표·사진·영상·사운드·보호대상 저장 구조 확장(camera_captures + pose_logs + media_artifacts + protected_assets) | 김선일 · 김주영 | M1 | S3 |
| SCRUM-24 | 발표용 화면/영상/시나리오 컷 캡처 | 박태진 · 김선일 | M3 | S4 |
| SCRUM-25 | Pi Camera 라이브 스트리밍 → Unity 패널 | 김선일 · 박태진 | M3 | S3 |

## 정리/검토 후보

- `SCRUM-3` (팀명 UR HYNIX) — 이미 Done. 유지.
- `SCRUM-5`, `SCRUM-6` — 예전 테스트성 카드. 상태 확인 후 보드에서 제외 검토.

## 동기화 절차

1. 본 문서를 정본으로 잠근다.
2. `mcp__atlassian__searchJiraIssuesUsingJql` 로 SCRUM-8~25 현재 상태를 가져온다.
3. 각 카드를 `editJiraIssue` 로 위 표의 제목·담당·Sprint·라벨로 갱신한다.
4. 갱신 후 다시 JQL 조회해 일치 검증.
