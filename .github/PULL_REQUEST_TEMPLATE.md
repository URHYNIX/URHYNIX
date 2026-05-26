## 📌 Jira

- 티켓: SCRUM-{N}
- 링크: https://jason1127.atlassian.net/browse/SCRUM-{N}

## 🎯 변경 목적 (1-2줄)

<!-- 무엇을, 왜 바꾸는지 -->

## 🔍 변경 요약

<!-- 코드/문서 변경 핵심을 bullet으로 -->

- 
- 

## ✅ 체크리스트

- [ ] 브랜치명이 `feature/SCRUM-N-xxx` 규칙 따름
- [ ] 변경 라인 800줄 이하 (그 이상이면 PR을 쪼개주세요)
- [ ] `docs/ref/CONTRACT.md` 변경 필요 여부 확인 (ROS msg / DB schema / Unity bridge 건드렸으면 필수)
- [ ] `docs/status/DECISION-LOG.md`에 큰 결정 1줄 기록 (해당 시)
- [ ] `docs/status/PROJECT-STATUS.md` 갱신 (Sprint 진행 영향 시)
- [ ] `secret-scan` 통과 (`bash .claude/skills/secret-scan/scan.sh`)
- [ ] Unity `.meta` 파일도 같이 커밋됐는지 확인 (Unity 변경 시)
- [ ] CODEOWNERS가 자동 지정한 리뷰어 모두 ping

## 🧪 테스트한 것

<!-- 어떻게 검증했는지 -->

- [ ] 로컬 빌드 통과
- [ ] (해당 시) 단위 테스트 실행
- [ ] (해당 시) 실기기 확인

## 🤝 영향받는 팀원

<!-- 다른 영역 owner를 명시 (`@TEAM_ROS @TEAM_VISION` 등) -->

## 📸 스크린샷 / 영상 (선택)

<!-- UI 변경 시 -->

## 🚨 리뷰어가 주의해서 봐주실 부분

<!-- 위험한 부분, 임시 hack, TODO 등 -->

---

🤖 *AI 어시스턴트 사용 시 코드에 Co-Authored-By 명시 권장*
