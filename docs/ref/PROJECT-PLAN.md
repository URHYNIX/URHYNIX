# Project Plan

## Sprint 1: 기획 / 기본 화면 (1~2주)

| 작업 | Jira | 담당 |
|---|---|---|
| MVP 범위와 팀 역할 확정 | SCRUM-8 | 김주영 |
| Unity 경기장 모델과 관제 UI 초안 | SCRUM-9 | 김주영 |
| 주행 트랙과 장애물 소품 준비 | SCRUM-16 | 박태진 |

## Sprint 2: ROS / 카메라 입력 (2~3주)

| 작업 | Jira | 담당 |
|---|---|---|
| TurtleBot3 SLAM/Nav2 기본 주행 확인 | SCRUM-10 | 임현찬 |
| Unity-ROS 로봇 위치/경로 연동 | SCRUM-11 | 임현찬 |
| TurtleBot3 카메라 설치/스트림 확인 | SCRUM-19 | 박태진 |
| 카메라 토픽과 로봇 pose 동기화 | SCRUM-20 | 임현찬 |

## Sprint 3: 장애물 / Vision / Unity 표시 (2주)

| 작업 | Jira | 담당 |
|---|---|---|
| LiDAR 장애물 감지와 회피/정지 기준 구현 | SCRUM-12 | 임현찬 |
| 카메라 기반 실시간 장애물 인식 파이프라인 | SCRUM-13 | 김선일 |
| LiDAR 장애물 테스트 환경 세팅 | SCRUM-17 | 박태진 |
| 장애물 클래스와 촬영 데이터셋 구축 | SCRUM-21 | 김선일 |
| Unity에 카메라 화면과 인식 결과 표시 | SCRUM-22 | 김주영 |

## Sprint 4: DB / QA / Demo (1~2주)

| 작업 | Jira | 담당 |
|---|---|---|
| 주행 데이터와 인식 결과 저장 | SCRUM-14 | 김선일 |
| LiDAR only vs 이미지 인식 비교 데모 | SCRUM-15 | 김주영 |
| 최종 시연 환경과 예비 영상 준비 | SCRUM-18 | 박태진 |
| 카메라 이미지와 인식 로그 저장 구조 확장 | SCRUM-23 | 김선일 |
| 카메라 인식 발표용 화면/영상 준비 | SCRUM-24 | 박태진 |
| 카메라 인식 주행 시나리오 검증 (QA) | SCRUM-25 | 김주영 |

## 추천 진행 순서

1. 기획 확정
2. 경기장/트랙 고정
3. TurtleBot3 SLAM/Nav2 확인
4. Unity pose 표시
5. 카메라 스트림 확인
6. 카메라 pose 동기화
7. Vision 인식
8. Unity 표시
9. DB 저장
10. 발표용 검증

