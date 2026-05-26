# URHYNIX

프로젝트 루트의 짧은 진입점이다.

## Loading Order

1. `CLAUDE.md`
2. `AGENTS.md`
3. `AGENT.md`
4. `docs/status/PROJECT-STATUS.md`
5. `docs/ref/PROJECT-PLAN.md`
6. `docs/ref/STACK-PROFILES.md`
7. `docs/ref/ARCHITECTURE.md`
8. `.claude/skills/README.md`
9. 필요 시 `docs/ref/PRD.md`, `docs/ref/SCHEMA.md`

## Hard Rules

1. 읽기 전 편집 금지
2. 중요한 액션 전 목적 명시
3. 추측보다 구현과 실행 결과 우선
4. 검증 없는 완료 선언 금지
5. 문서 드리프트 방치 금지
6. 파일이 300줄 근처면 분리를 검토
7. 새 폴더가 경계를 가지면 로컬 `AGENTS.md` 또는 `CLAUDE.md` 추가
8. 새 요청은 `/intake` 또는 `task-intake-router` 우선
9. 완료 전 `evidence-review` 또는 `Evidence Status` 갱신
