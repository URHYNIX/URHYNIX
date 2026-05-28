# Session Handoff — 다음 세션 진입 캡슐

> **다음 세션의 AI 에이전트가 첫 5분 안에 컨텍스트를 잡기 위한 1페이지.**
> 이 파일만 읽으면 출발 가능. 자세한 건 아래 링크로 들어가면 됨.

**Last updated**: 2026-05-28 (Day-1 후속 — Unity ROS-TCP 재기동 ✅ / RPi USB serial 영구 식별 ✅ / **DB 선정 완료 ✅** `ueupkrxwybuuqxflstvg` / 로봇 셧다운 상태) · **세션 종료자**: 김주영

---

## 🎯 첫 5분에 읽을 것 (이 3개만)

1. **이 파일** (`docs/status/HANDOFF.md`)
2. `docs/status/PROJECT-STATUS.md` — 한 줄 상태 + 역할 매트릭스
3. `docs/status/DECISION-LOG.md` 가장 아래 5건 — 오늘까지의 결정 흐름

→ 이 3개 다 읽어도 5분 이내. 자세한 SSOT는 필요할 때 들어가면 됨.

---

## 🚀 지금 즉시 해야 할 일 (Top 1) — **DB 선정 + 마이그레이션 끝, 로봇 부팅만 남음 (2026-05-28)**

| # | 단계 | 담당 | 상태 |
|---|---|---|---|
| 0 | ~~DB 선정 + 마이그레이션~~ | 김주영 | ✅ 완료 (Supabase `ueupkrxwybuuqxflstvg`, 4테이블 + seed) |
| 1 | **로봇 메인 스위치 ON** (라즈베리파이 셧다운 상태) → 30초 부팅 대기 | 사람 (현장) | 🟥 대기 |
| 2 | `kim@192.168.0.138`에서 **`/etc/urhynix.env`** 작성 (service_role 키 주입) — **2026-05-28 작성 완료** | 박태진 | ✅ done |
| 2.5 | **`tb3-key-setup`** 한 번 (Mac/Ubuntu 머신마다 1회, 이후 비번 prompt 영구 사라짐) | 머신 주인 | 머신당 1회 |
| 3 | **`tb3-go`** (Mac/Ubuntu) — bringup + ros_tcp + arduino_bridge + 검증까지 한 방 | 박태진 | 2.5단계 후 |
| 4 | PIR 손 흔들기 → `sb-tail` 로 `events` row +1 확인 → **Day-1 완전 PASS** 🎉 | 다 같이 | 3단계 후 |
| 5 | LDR 가리기 (어두움 만들기) → `sb-dark` 로 event_type='dark' row +1 확인 | 다 같이 | 4단계 후 (선택) |

### 2단계: `/etc/urhynix.env` 작성 절차

```bash
tb3-ssh   # 또는: ssh kim@$(tb3-ip)
sudo tee /etc/urhynix.env >/dev/null <<'EOF'
SUPABASE_URL=https://ueupkrxwybuuqxflstvg.supabase.co
SUPABASE_KEY=<paste service_role legacy JWT — DO NOT commit>
URHYNIX_SESSION_ID=00000000-0000-0000-0000-000000000001
URHYNIX_ROBOT_ID=tb3_1
EOF
sudo chmod 600 /etc/urhynix.env
```

→ service_role JWT는 작업자 본인의 Supabase 대시보드 또는 어제 발급된 access token으로 `supabase projects api-keys --project-ref ueupkrxwybuuqxflstvg`로 다시 받기. 절대 GitHub/Slack/HTML 보드에 박지 말 것.

### 4단계: 검증 명령

```bash
# Supabase 대시보드에서:
#   Table Editor → events → ts desc 정렬 → MOTION 새 row 확인
# 또는 SQL editor에서:
select id, ts, event_type, severity, x, y, theta, raw_payload->>'label' as label
from public.events
where session_id='00000000-0000-0000-0000-000000000001'
order by ts desc limit 10;
```

### 0단계 외 독립 진행 가능 (Day-1 잔여)

핀 매핑 정렬 잔여 1건: 현재 검증 코드는 PIR=D7·LED=D2지만 SSOT는 PIR=D2. **PIR=D2 / LED=D8(또는 D11)**로 재배선 + 스케치 갱신 후 재플래시 (`arduino-flash` 스킬 그대로 재사용). LDR=A0은 이미 일치. DB 선정과 무관하게 진행 가능.

```bash
# (0단계 결정 후 2단계 검증용 예시)
# 라즈베리파이에서:
ls -l /dev/tb3_arduino          # ttyACM1 자동 심링크 확인
python3 - <<'PY'
import serial, time
s = serial.Serial('/dev/tb3_arduino', 9600, timeout=1); time.sleep(2)
while True:
    line = s.readline().decode('utf-8', errors='replace').strip()
    if line.startswith('[MOTION]'):
        # → 여기서 DB insert (선정된 DB 클라이언트로)
        print('PIR EVENT', line)
PY

# 1건 이상 row가 보이면 Day-1 완전 PASS, W2 진입 OK.
```

---

## 📊 현재 상태 (2026-05-27 21:00 기준)

| 영역 | 상태 |
|---|---|
| 방향 합의 | ✅ 완료 (다중 경비 로봇 디지털 트윈) |
| SSOT 9개 | ✅ 갱신 완료 |
| HTML 보드 8개 + 단일 번들 (465KB) | ✅ 갱신 완료 |
| Jira 18개 티켓 | ✅ 재작성 + 6개 Day-1 본문 |
| Confluence 4페이지 | ✅ 모두 v최신 |
| 신규 스킬 2개 (`ssot-board-sync`, `decision-broadcast`) | ✅ 등록 |
| Day-1 작업 진행 | ⏳ 진행 중 (3팀 동시) |
| Day-1 통합 검증 | 🟡 절반 PASS (PIR + LDR(A0) + 시리얼 ✅ 2026-05-28 / DB 단계 대기) |
| Arduino PIR + LDR 플래시 자동화 | ✅ `arduino-cli` 1.5.0 + `sketches/pir_led/` 검증 (`MOTION`/`CLEAR` + `[LDR] A0=...` 로그) |
| TurtleBot ROS2/RViz/Unity smoke | ✅ 성공 기록 완료 |
| TurtleBot helper aliases | ⚠️ Mac 완료, robot-side 실행은 SSH timeout으로 대기 |
| 팀 Slack 채널 봇 권한 | ⚠️ 채널 `C0B5Q43A27R`에 봇 초대 필요 |

---

## 🧭 Day-1 작업 분담 (오늘 진행 중)

- **김주영 + 임현찬** → Pi Camera 스트림 + DB 연결 테스트 (SCRUM-19, 14)
- **박태진** → Arduino + PIR → 시리얼 → DB 한 줄 통과 (SCRUM-13, 14)
- **김선일** → Unity 관제 UI 기능 명세 문서화 (SCRUM-9)

---

## ⏭️ Day-2 (W2) 진입 시 첫 작업

Day-1 PASS 확인 후 다음 액션:

1. SCRUM-10 (tb3_1 SLAM/Nav2 베이스라인) 시작 — 임현찬·김선일
2. SCRUM-16 (실내 트랙·waypoint 측정) 시작 — 박태진·임현찬
3. 부족 센서 발주: 소리·불꽃 (키트 미포함분)
4. OpenCR 5V 패드 위치 실측 + 점퍼 배선 도면 — 임현찬·김주영
5. 김선일 Unity 기능 문서 → SCRUM-11/22 분해

---

## 🔗 외부 시스템 진입점

| 시스템 | 페이지/ID | 역할 |
|---|---|---|
| **Confluence 327681** | [기획안 (UR HYNIX) v10](https://jason1127.atlassian.net/wiki/spaces/SCRUM/pages/327681) | **외부 정본 SSOT** |
| Confluence 1605636 | [역할 분배 보드](https://jason1127.atlassian.net/wiki/spaces/SCRUM/pages/1605636) | 5×4 매트릭스 |
| Confluence 1048633 | [2026-05-27 회의록](https://jason1127.atlassian.net/wiki/spaces/SCRUM/pages/1048633) | Day-1 분담 |
| Confluence 1540099 | [브레인스토밍 마인드맵](https://jason1127.atlassian.net/wiki/spaces/SCRUM/pages/1540099) | 방향 전환 근거 |
| Confluence 2555905 | [상세 UR (사용자 요구사항) v3](https://jason1127.atlassian.net/wiki/spaces/SCRUM/pages/2555905) | 40 UR + 5 NFR |
| Jira | [SCRUM-7 에픽](https://jason1127.atlassian.net/browse/SCRUM-7) | 18 카드 부모 |
| Jira | [SCRUM 보드](https://jason1127.atlassian.net/jira/software/projects/SCRUM/boards/1) | 칸반 |
| Slack | 채널 `C0B5Q43A27R` (⚠️ 봇 초대 필요) | 팀 소통 |
| GitHub | [URHYNIX/URHYNIX](https://github.com/URHYNIX/URHYNIX) | 코드 |

---

## 📦 오늘 만든 자산 (다음 세션도 활용)

| 자산 | 위치 | 용도 |
|---|---|---|
| 단일 HTML 보드 | `docs/dev-plan-bundle.html` (465KB) | 더블클릭으로 7페이지 다 열림. 팀 공유용 |
| 7페이지 HTML | `docs/dev-plan*.html` | 페이지별 분리 보기 |
| 역할 매트릭스 PNG | `docs/whiteboards/role_matrix.png`, `role_graph.png` | Confluence/Whiteboard 첨부용 |
| 번들 빌더 | `docs/whiteboards/build_bundle.py` | HTML/번들 갱신 후 재빌드 |
| PNG 생성기 | `docs/whiteboards/generate_role_board.py` | 역할 변경 시 PNG 재생성 |
| 스킬 `ssot-board-sync` | `.claude/skills/ssot-board-sync/SKILL.md` | SSOT ↔ HTML 동기화 |
| 스킬 `decision-broadcast` | `.claude/skills/decision-broadcast/SKILL.md` | 결정 → 5채널(DECISION-LOG/SSOT/HTML/Jira/Slack) 동기화 |
| 스킬 `arduino-flash` | `.claude/skills/arduino-flash/SKILL.md` | Arduino UNO 4종 센서 GUI-less 플래시 파이프라인 (compile→upload→serial verify) |
| 표준 PIR+LDR 스케치 | `sketches/pir_led/pir_led.ino` | HW-740 PIR(D7) + LED(D2) + LDR(A0, 10kΩ 분압) 합본. 워밍업·엣지트리거·2초 주기 LDR 보고·`[LDR] A0=` 라벨 (소리/불꽃 복제 베이스) |
| TurtleBot live smoke evidence | `docs/evidence/2026-05-27-live-turtlebot-ros-rviz-unity-smoke.md` | ROS2/RViz/ROS-TCP/Unity 미니 UI 성공 경로와 재현 명령 |
| TurtleBot env check evidence | `docs/evidence/2026-05-27-unity-ros2-turtlebot-env-check.md` | Mac/Unity/ROS2/참고 repo 환경 점검 결과 |
| 스킬 `urhynix-turtlebot-unity-ros2-success-pattern` | `/Users/family/.codex/skills/urhynix-turtlebot-unity-ros2-success-pattern/SKILL.md` | TurtleBot3 Burger → ROS2 → RViz → ROS-TCP → Unity smoke 재현 패턴 |
| Mac TurtleBot helpers | `/Users/family/.zshrc` | `tb3-ip`, `tb3-ssh`, `tb3-vnc`, `tb3-port`, `tb3-unity`, `tb3-help` |

---

## 🛠️ 다음 세션 출발 명령 (체크리스트)

```bash
# 1. 현재 git 상태 확인
git status

# 2. 단일 번들 최신인지 (필요 시 재빌드)
python3 docs/whiteboards/build_bundle.py

# 3. HTML 파싱 OK 확인
python3 -c "
from html.parser import HTMLParser
import glob
for f in sorted(glob.glob('docs/dev-plan*.html')):
    HTMLParser().feed(open(f).read())
print('All HTML OK')
"

# 4. 옛 방향 잔재 0건 확인
grep -rn '/turtlebot/\|LiDAR only vs\|expansion plate' docs/ref docs/status docs/dev-plan*.html 2>/dev/null | grep -v DECISION-LOG
# 0건이면 OK

# 5. Day-1 통합 검증 (Top 1 작업)
# 5a. Arduino 보드/스케치 살아있는지 (PIR 절반 회귀 검증)
arduino-cli board list  # /dev/cu.usbmodemNNNN가 Arduino UNO로 잡혀야 함
# 필요 시 30초 시리얼 캡처:
# stty -f /dev/cu.usbmodem1101 9600 cs8 -cstopb -parenb -echo raw
# (cat /dev/cu.usbmodem1101 & CAT_PID=$!; sleep 30; kill $CAT_PID 2>/dev/null) | head -40
# 5b. DB 한 줄 통과 확인:
# psql -c "SELECT * FROM events ORDER BY ts DESC LIMIT 5;"

# 6. TurtleBot live smoke 재현/복구 출발점 (필요 시)
source ~/.zshrc
tb3-help
tb3-ip
tb3-ssh

# 로봇 SSH 접속 후, robot-side helper 설치가 아직 안 끝났다면
python3 /tmp/install_tb3_helpers.py
source ~/.bashrc
tb3-help
```

---

## 🚨 미해결 이슈

- ~~DB 미선정~~ → ✅ **해소 (2026-05-28)**: Supabase `ueupkrxwybuuqxflstvg` 잠금. DECISION-LOG "DB 선정 완료".
- **로봇 셧다운 상태 (2026-05-28)** — `sudo shutdown -h now` 후 ping/SSH 모두 무응답. LiDAR도 메인 LiPo로 도는 걸 막기 위해 메인 슬라이드 스위치 OFF 권장. 다음 세션 첫 행동은 **사람이 메인 스위치 ON** → 30초 부팅 대기.
- **`/etc/urhynix.env` 미작성** — 로봇 부팅 직후 한 번만 (service_role JWT 주입). 코드 commit 절대 금지.
- **팀 Slack 채널 봇 권한 부족** — 채널 `C0B5Q43A27R`에 Claude 봇 초대 필요. 그때까지 결정 공지는 본인 DM으로만 가능.
- **TurtleBot helper 설치 마무리 대기** — Mac helper는 `/Users/family/.zshrc`에 반영 완료. Robot helper installer는 `/tmp/install_tb3_helpers.py`까지 복사됐으나, teardown 이후 `192.168.0.138:22` SSH가 timeout되어 원격 실행은 미검증. 로봇 전원/네트워크가 돌아오면 위 `python3 /tmp/install_tb3_helpers.py` 실행으로 마무리. (2026-05-28 재접속 확인 — 다음 세션에서 즉시 실행 가능)
- (그 외 미해결 결정 없음)

---

## 📜 한줄정리

PIR + LDR(A0) + Arduino + 시리얼은 2026-05-28에 `arduino-cli` 자동화로 절반 통과 (LDR은 SSOT 정렬 완료). 남은 일은 **PIR D7→D2 정렬 + 시리얼 → DB 한 줄 insert 1건**. 통과되면 W2(SCRUM-10 SLAM, SCRUM-16 트랙)로 진입. 나머지는 모두 잠겨 있어요.

---

## 🔁 HANDOFF 갱신 규칙

이 파일은 **세션 종료 시마다** 갱신한다:
1. 첫 5분에 읽을 것 (변경 거의 없음)
2. 지금 즉시 해야 할 일 Top 1 (매번 갱신)
3. 현재 상태 표 (매번 갱신)
4. 미해결 이슈 (해결 시 제거, 새 이슈 시 추가)
5. Last updated 갱신
