# URHYNIX Git Workflow — 팀 합의 문서

> 4명이 같은 repo에서 서로 영역 침범 없이 일하기 위한 최소 규칙.
> 이 문서는 **정본**이고, 바뀌면 슬랙 채널 `C0B5Q43A27R`에 공지한다.

## 1. 팀 역할과 폴더 owner

| 팀원 | 영역 | 주 폴더 |
|---|---|---|
| **김주영** (gmdqn2tp) | Unity DT + 통합 QA | `unity-src/`, `docs/status/` |
| **임현찬** | ROS 메인 + 브릿지 | `ros-ws/src/urhynix_bringup/`, `urhynix_unity_bridge/` |
| **김선일** | Vision + DB | `ros-ws/src/urhynix_obstacle_detect/`, `urhynix_logger/`, `vision/`, `backend/db/` |
| **박태진** | HW + Demo | `demo/`, `docs/evidence/`, HW 셋팅 |

> 자세한 owner는 `.github/CODEOWNERS` 참고.

## 2. 브랜치 전략 (3티어)

```
main          ← 시연 시점 안정 버전만. 직접 푸시 금지. PR 머지만.
 └ dev        ← 매일 통합 브랜치. 모든 feature가 여기로 머지.
      └ feature/SCRUM-{N}-{kebab-desc}   ← 티켓당 1 브랜치
```

**브랜치명 규칙:**
- `feature/SCRUM-13-vision-pipeline` ✅
- `feature/SCRUM-10-slam-bringup` ✅
- `fix/SCRUM-22-camera-ui-overflow` ✅ (버그)
- `chore/dependency-bump` ✅ (티켓 없는 정리)
- `kim-test`, `tmp`, `wip` ❌ 금지

## 3. PR 정책

| 룰 | 값 | 이유 |
|---|---|---|
| 최대 변경 라인 | 400줄 권장, 800줄 한계 | 리뷰어 부담 |
| 머지 방식 | **Squash and merge** | 커밋 히스토리 깔끔 |
| 리뷰어 | CODEOWNERS 자동 지정 + 1명 이상 승인 | 사일로 방지 |
| CI | 통과 필수 (secret-scan, ros build) | 깨진 코드 통합 X |
| 머지 후 | 브랜치 자동 삭제 | repo 정리 |
| 작성자 셀프 머지 | OK (승인 받은 후) | 빠른 진행 |

## 4. 매일 의식 (Daily Ritual)

```bash
# ── 작업 시작 (아침)
git checkout dev
git pull origin dev
git checkout -b feature/SCRUM-N-xxx     # 새 작업이면

# ── 작업 중 (수시)
git add <file>
git commit -m "feat: ..."               # 자주 커밋, 작게

# ── 작업 종료 (저녁)
git pull --rebase origin dev            # 다른 사람 변경 끌어옴 (충돌 발견)
git push -u origin feature/SCRUM-N-xxx
gh pr create --base dev --title "SCRUM-N: ..." \
   --body-file .github/PULL_REQUEST_TEMPLATE.md
# 슬랙에 PR 링크 공유 → CODEOWNERS가 리뷰
```

> morning-orchestrator가 평일 08시에 `git pull --rebase` 후 충돌이 있으면 슬랙으로 즉시 알림 (자동).

## 5. 충돌 방지 — 인터페이스 계약

**ROS 메시지·DB 스키마·Unity 인터페이스**는 변경 전 `docs/ref/CONTRACT.md`에 합의 기록.
한쪽이 일방 변경하면 다른 둘 빌드 깨짐 → **PR에 CONTRACT.md 수정 포함하지 않으면 머지 차단**.

## 6. Unity 특수 룰

| 룰 | 이유 |
|---|---|
| 같은 `.unity` 씬 동시편집 금지 | 머지 거의 불가능 → 슬랙에 "지금 MainScene 작업" 락 메시지 |
| 새 prefab 생성 시 `/Assets/Prefabs/<owner>/...` 폴더 분리 | 충돌 영역 격리 |
| `.meta` 파일은 본체와 항상 같이 커밋 | `.cs` 만 커밋하고 `.cs.meta` 빼면 다른 사람 Unity가 깨짐 |
| Unity Smart Merge 권장 | `git config merge.tool unityyamlmerge` |

## 7. 절대 금지 (Hard Rules)

- ❌ `main`에 직접 push (보호 룰로 차단됨)
- ❌ `git push --force` (단, 본인 feature 브랜치는 `--force-with-lease` 허용)
- ❌ `.env`, `*.key`, 토큰 커밋 → `secret-scan` 통과 못 함
- ❌ FR5UNITY 원본(`/Users/family/jason/FR5UNITY`) 절대 수정
- ❌ Co-author 표기 없는 큰 PR (AI 사용 시 명시)

## 8. 위기 대응

| 상황 | 대응 |
|---|---|
| 머지 후 main이 깨짐 | `git revert <commit>` → 즉시 새 PR (절대 `reset --hard` 금지) |
| 다른 사람 PR이 내 작업과 충돌 | 슬랙에서 `@해당팀원` 5분 짧은 미팅 → 누가 먼저 머지할지 합의 |
| Unity 씬 머지 충돌 발생 | 늦게 push한 쪽이 양보 → 새 브랜치에서 다시 작업 |
| 시연 직전 (D-1) 큰 변경 필요 | Pause → Slack 전원 확인 → 별도 hotfix 브랜치 |

## 9. 도구

- **PR/이슈**: GitHub (URHYNIX.git)
- **작업 트래커**: Jira (SCRUM 프로젝트)
- **알림**: Slack `C0B5Q43A27R` (자동 PR 알림 + daily-recap)
- **CI**: GitHub Actions (`.github/workflows/ci.yml`)

## 한줄정리
브랜치는 `feature/SCRUM-N-xxx` → PR로 dev에 머지 → 주 1회 dev → main, CODEOWNERS가 자동 리뷰어 지정, ROS/DB/Unity 인터페이스 변경은 CONTRACT.md 동시 갱신 필수.
