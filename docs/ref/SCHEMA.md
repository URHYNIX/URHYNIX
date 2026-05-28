# Schema

> 디지털트윈경비로봇 — 이벤트/대응 로그 DB.
> 정본: 본 문서. `CONTRACT.md §2` 는 요약본. 변경 시 둘을 동시에 갱신한다.
> 저장소: Supabase (기본) 또는 로컬 Postgres 14+.
>
> ⚠️ **저장소 미선정 (2026-05-28)** — Supabase에 URHYNIX 전용 프로젝트가 아직 없고 `db/migrations/2026-05-27_init_security.sql`도 미작성. Day-1 "PIR → `events` insert"는 DB 선정 결정(`DECISION-LOG.md` 2026-05-28 "DB 선정 보류") 후 진행한다.

## Core Entities

```text
session_meta (1) ─── (N) events ─── (0..1) dispatches ─── (0..1) camera_captures
                                                                   │
                                                            (0..1) ai_labels (optional)
```

- 한 **세션**(시연 회차)은 다수 **이벤트**를 가진다.
- 한 **이벤트**는 최대 한 번의 **출동**으로 이어진다.
- 한 **출동**은 최대 한 번의 **카메라 확인**을 만든다.

## Tables

### `session_meta` — 시연/순찰 세션 메타

| Column | Type | NULL | Default | 비고 |
|---|---|---|---|---|
| `session_id` | UUID PK | NO | `gen_random_uuid()` | 한 세션 ID |
| `started_at` | TIMESTAMPTZ | NO | `now()` | UTC |
| `ended_at` | TIMESTAMPTZ | YES | NULL | 종료 시점 |
| `scenario` | TEXT | NO | — | `night_patrol`/`intrusion`/`noise`/`fire_mock`/`mixed` |
| `notes` | TEXT | YES | NULL | 자유 메모 |
| `recorded_by` | TEXT | YES | NULL | 운영자 이름 |

Index: 없음 (PK 단독).

### `events` — 센서 감지 이벤트

| Column | Type | NULL | 비고 |
|---|---|---|---|
| `id` | UUID PK | NO | SecurityEvent UUID |
| `session_id` | UUID FK → session_meta | NO | |
| `robot_id` | TEXT | NO | `tb3_1` / `tb3_2` |
| `ts` | TIMESTAMPTZ | NO | UTC ms |
| `event_type` | TEXT | NO | `dark` / `pir` / `noise` / `fire` |
| `severity` | SMALLINT | NO | 0~3 |
| `x` | DOUBLE PRECISION | NO | meters |
| `y` | DOUBLE PRECISION | NO | meters |
| `theta` | DOUBLE PRECISION | NO | radians |
| `raw_payload` | JSONB | YES | 센서 원본 값(임계값, ADC 값 등) |

Index:
- `idx_events_session` on `(session_id, ts)` — 세션별 시간순 조회
- `idx_events_type` on `(event_type)` — 시나리오별 집계
- `idx_events_robot` on `(robot_id, ts)`

### `dispatches` — tb3_2 출동 기록

| Column | Type | NULL | 비고 |
|---|---|---|---|
| `id` | UUID PK | NO | Dispatch UUID |
| `event_id` | UUID FK → events | NO | source 이벤트 |
| `target_robot_id` | TEXT | NO | 기본 `tb3_2` |
| `target_x` | DOUBLE PRECISION | NO | |
| `target_y` | DOUBLE PRECISION | NO | |
| `dispatched_at` | TIMESTAMPTZ | NO | 명령 발행 시각 |
| `arrived_at` | TIMESTAMPTZ | YES | nullable (도착 못 함 가능) |
| `response_time` | NUMERIC(6,2) | YES | seconds — `arrived_at - event.ts` 계산값 |
| `simulated` | BOOLEAN | NO | true=Unity 시뮬, false=실기 |

Index:
- `idx_dispatches_event` on `(event_id)`
- `idx_dispatches_session` join via events

### `camera_captures` — Pi Camera 확인 결과

| Column | Type | NULL | 비고 |
|---|---|---|---|
| `id` | UUID PK | NO | |
| `dispatch_id` | UUID FK → dispatches | NO | |
| `robot_id` | TEXT | NO | 보통 `tb3_2` |
| `ts` | TIMESTAMPTZ | NO | |
| `image_path` | TEXT | NO | 로컬 경로 또는 Supabase Storage URL |
| `result` | TEXT | NO | `confirmed`/`false_alarm`/`missed`/`unverified` |
| `ai_label` | TEXT | YES | M1 모델 결과 |
| `ai_confidence` | REAL | YES | 0~1 |
| `operator_note` | TEXT | YES | 운영자 코멘트 |

Index:
- `idx_captures_dispatch` on `(dispatch_id)`

## Migrations

- 초기 생성 SQL: `db/migrations/2026-05-27_init_security.sql` (Sprint 1 SCRUM-14에서 작성 예정)
- Supabase 사용 시 `supabase migration new` 워크플로 사용
- 모든 DDL 변경은 `CONTRACT.md` 동시 갱신 PR로

## Contracts

- ROS `SecurityEvent.id` ↔ `events.id` 동일 UUID
- ROS `Dispatch.id` ↔ `dispatches.id` 동일 UUID
- ROS `CameraConfirm.dispatch_id` ↔ `dispatches.id` 매칭
- `events.session_id`은 모든 후속 테이블에서 join 가능해야 한다 (직접 FK가 아니더라도 events 경유)

## 발표용 KPI 쿼리 (예시)

```sql
-- 시나리오별 평균 출동 시간 + 확인 성공률
SELECT
  s.scenario,
  COUNT(e.id) AS event_count,
  AVG(d.response_time) AS avg_response_sec,
  100.0 * SUM(CASE WHEN c.result = 'confirmed' THEN 1 ELSE 0 END) / NULLIF(COUNT(c.id), 0) AS confirm_rate
FROM session_meta s
JOIN events e ON e.session_id = s.session_id
LEFT JOIN dispatches d ON d.event_id = e.id
LEFT JOIN camera_captures c ON c.dispatch_id = d.id
GROUP BY s.scenario;
```

## Open Questions

- **DB 선정 미정 (2026-05-28, Day-1 차단)** — 신규 Supabase 프로젝트 `urhynix` / 기존 `vibe`에 `urhynix` 스키마 추가 / 라즈베리파이 로컬 Postgres 14+ 중 1개로 결정 필요. 결정자: 김주영 (M1). 결정 즉시 `db/migrations/2026-05-27_init_security.sql` 작성 → 박태진 Day-1 잔여 액션 재개.
- AI 분류(`ai_label`)를 별도 `ai_labels` 테이블로 분리할지, `camera_captures` 인라인 컬럼으로 둘지 → S3 SCRUM-21 진행 시 결정.
- Supabase Storage vs 로컬 파일 시스템 — Pi 저장소 한계 + 발표 영상 백업 정책 고려.
- 실기 2대 운영 시 `dispatches.simulated=false` 조회를 추가 인덱스로 가속할지.
