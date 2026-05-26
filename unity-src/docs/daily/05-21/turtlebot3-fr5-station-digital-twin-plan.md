# TurtleBot3 + FR5 Station Digital Twin Plan

Date: 2026-05-21 KST

## Request

- Summarize the full planning conversation into one optimal implementation direction.
- Lock the first hardware approach as `LiDAR + Raspberry Pi Camera + AprilTag`.
- Keep the existing Pendant V3 teaching pendant separate from the new station digital twin work.

## Decision

Created the canonical planning draft:

- `/Users/family/jason/FR5UNITY/robotapp/docs/ref/product/robots/turtlebot3-fr5-station-digital-twin-plan.md`

## Locked Direction

- Use TurtleBot3 LiDAR for SLAM, Nav2, 2D map, and broad localization.
- Use Raspberry Pi Camera for video and AprilTag detection.
- Use AprilTag for final docking alignment.
- Keep Unity as the operator-facing digital twin and station UI.
- Keep ROS2 Gateway as the execution and safety authority.
- Keep Pendant V3 as the FR5 setup, teaching, point/loop validation, and manual recovery surface.

## Notes

- The new page should not try to clone RViz.
- Polycam/RGB-D/3D reconstruction remain future visual-quality upgrades, not MVP dependencies.
- The first useful result is a reliable station state machine, not a perfect 3D scan.
- Follow-up clarification added a `Physical Truth Plan` section to separate robot-trusted execution data from visual-only assets such as Polycam, 3DGS, and decorative Unity models.
- Follow-up clarification added a `SLAM Photo Placement And Unityctl Import Plan` section: TurtleBot3 can capture photos while SLAM/Nav2 runs, save each image with `map -> base_link` pose metadata, and use `unityctl exec` to import those photos into Unity as visual-only photo anchors, wall-snap previews, or later texture projections. This improves realism for general viewers but must not become a robot execution truth source.
