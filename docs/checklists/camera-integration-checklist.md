# Camera Integration Checklist

## Hardware

- [ ] 카메라가 TurtleBot3에 단단히 고정되어 있다.
- [ ] 주행 중 케이블이 바퀴나 센서에 걸리지 않는다.
- [ ] 카메라 시야에 트랙과 장애물이 들어온다.
- [ ] 조명 변화가 너무 심하지 않다.

## ROS / Data

- [ ] 카메라 이미지 topic이 확인된다.
- [ ] 이미지 timestamp가 확인된다.
- [ ] 로봇 pose timestamp가 확인된다.
- [ ] image_id와 robot_pose를 연결할 수 있다.
- [ ] 샘플 이미지와 pose manifest를 저장할 수 있다.

## Vision

- [ ] 장애물 class 목록이 정해져 있다.
- [ ] 각 class별 대표 이미지가 있다.
- [ ] 실시간 또는 준실시간 인식 결과가 나온다.
- [ ] confidence가 낮을 때의 표시 기준이 있다.

## Unity

- [ ] 카메라 이미지 또는 캡처가 표시된다.
- [ ] class/confidence가 표시된다.
- [ ] 로봇 위치와 인식 결과가 같은 화면에서 이해된다.
- [ ] 인식 실패 상태가 구분된다.

## DB

- [ ] image_id가 저장된다.
- [ ] image_path가 저장된다.
- [ ] robot_pose가 저장된다.
- [ ] vision_class와 confidence가 저장된다.
- [ ] run_id로 주행 1회를 묶을 수 있다.

