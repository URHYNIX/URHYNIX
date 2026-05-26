# Architecture

## 전체 구조

```text
TurtleBot3
  -> ROS2 / SLAM / Nav2
  -> LiDAR scan, map, tf, odom
  -> Camera image
  -> Vision recognition result
  -> DB / log
  -> Unity Digital Twin UI
```

## 역할 분리

### TurtleBot3 / ROS2

- 실제 로봇 주행
- SLAM map 생성
- Nav2 goal 이동
- LiDAR 장애물 감지
- 카메라 이미지 topic 제공
- 로봇 pose와 timestamp 제공

### Vision

- 카메라 프레임 입력
- 장애물 class/confidence 출력
- image_id, timestamp, robot_pose와 연결

### Unity Digital Twin

- 경기장 모델 표시
- 로봇 현재 위치 표시
- 이동 경로 표시
- 카메라 이미지 또는 캡처 표시
- 인식 결과 표시
- DB 요약 결과 표시

### DB / Log

- run_id
- timestamp
- robot_pose
- obstacle_event
- image_id / image_path
- vision_class
- confidence
- stop_count / travel_time

## 중요한 원칙

- 실제 주행 판단은 ROS/TurtleBot3 기준으로 한다.
- Unity는 관제와 시각화 중심으로 둔다.
- 카메라 인식 결과는 LiDAR 안전 판단을 보조하는 데이터로 사용한다.
- 발표에서는 "로봇이 본 것", "로봇의 위치", "판단 결과"가 동시에 보이게 한다.

