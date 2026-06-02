# ROS2 Robot Tech Ref

ROS2, TurtleBot3, SLAM/Nav2, robot bringup 작업의 빠른 진입점이다.

## Read First

1. `docs/ref/TECH-INDEX.md`
2. `docs/ref/ARCHITECTURE.md`
3. `docs/ref/MAC-DOCKER-ROS2-PLAYBOOK.md`
4. `.claude/skills/slam-nav2-arena-survey/SKILL.md` for SLAM/Nav2 survey work
5. `scripts/tb3.sh` for helper aliases and robot connection flow

## Current Truth

- ROS domain: `ROS_DOMAIN_ID=230`.
- `tb3_1` / T1: vision-centered robot, host `t1@192.168.0.250`, hostname `rb`.
- `tb3_2` / Genji: sensor and confirmation robot, host `urhynix-robot`.
- Robot-side SLAM/Nav2 runs on Raspberry Pi or Linux robot host, not macOS Docker.
- RPi build rule: use `colcon build --symlink-install --parallel-workers 1 --executor sequential`.
- Do not delete `~/turtlebot3_ws/build`; install hooks can depend on build artifacts.

## Verify

- Robot connection: `ssh urhynix-robot hostname`.
- Topic smoke: `/scan`, `/odom`, `/battery_state`, `/tf` for TurtleBot basics.
- SLAM output: map saved under robot `~/maps/<name>.{pgm,yaml}` and local evidence/docs location when copied.
- Unity bridge: ROS-TCP endpoint listens and Unity subscribes to expected topics.

