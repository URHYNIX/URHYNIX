# Vision Camera Tech Ref

Pi Camera, Intel RealSense D435, compressed ROS image topics, YOLO/vision work의 빠른 진입점이다.

## Read First

1. `docs/ref/TECH-INDEX.md`
2. `.claude/skills/robot-camera-bringup/SKILL.md`
3. `.claude/skills/unity-camera-panel/SKILL.md` when the target is Unity rendering
4. `docs/status/PROJECT-STATUS.md` Evidence Status camera rows
5. `docs/ref/ARCHITECTURE.md` dual robot role section

## Current Truth

- T1 (`tb3_1`) camera: Intel RealSense D435, not D435i, no IMU.
- Genji (`tb3_2`) camera: Raspberry Pi Camera Module v2, Sony IMX219.
- ROS domain for current dual-robot camera work: `ROS_DOMAIN_ID=230`.
- Unity live topics:
  - `/tb3_2/camera/image_raw/compressed`
  - `/tb3_1/camera/color/image_raw/compressed`
- Unity dual camera live display is verified as of 2026-06-02 (`image-20260602-031954.png`, Confluence `2026.06.02`).
- MVP presentation vision classes: robot, person, important item, fire.
- Fast classifier spike: Google Teachable Machine -> TensorFlow/Keras, classes `empty space`, `box`, `mouse-black/white`, `hand` (Korean labels in Jira: 빈공간, 박스, 마우스(검정/흰색), 손).
- Mac RealSense streaming is not the trusted path; robot/Windows smoke has been validated.

## Verify

- ROS topic hz around 30Hz for camera streams.
- Compressed image transport installed where compressed topics are expected.
- Unity panel renders both camera streams when both robot topics are live.
- Keras classifier emits class + confidence for camera frames before claiming the Teachable Machine spike complete.
- Evidence file gets updated when camera model, topic, frame rate, or host changes.
