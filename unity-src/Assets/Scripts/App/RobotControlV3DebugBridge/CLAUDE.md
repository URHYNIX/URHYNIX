# RobotControlV3DebugBridge/

`unityctl exec`와 QA matrix를 위한 RobotControlV3 디버그 브리지 폴더입니다.

## 주요 역할
- runtime debug entrypoints
- panel-specific debug actions
- QA matrix harness
- summaries / layout inspection

## 주 소비자
- `unityctl exec`
- Codex debug/QA loops

## 하지 말아야 할 것
- production runtime policy 변경
- panel controller business logic 직접 구현
- broad live safety policy 이동

## naming rule
- scene/controller lookup helper는 `RobotControlV3DebugBridge.cs`에 둔다.
- generic shared helper는 `RobotControlV3DebugBridge.Core.cs`에 둔다.
- product-like live QA artifact runner는 `RobotControlV3DebugBridge.LiveQa.cs`에 둔다.
- panel/runtime debug entry는 `RobotControlV3DebugBridge.LiveRuntime.*.cs` partial에 둔다.

## 현재 파일 인덱스
- `RobotControlV3DebugBridge.cs`
- `RobotControlV3DebugBridge.Core.cs`
- `RobotControlV3DebugBridge.LiveQa.cs`
- `RobotControlV3DebugBridge.LiveRuntime.cs`
- `RobotControlV3DebugBridge.LiveRuntime.LiveControl.cs`
- `RobotControlV3DebugBridge.LiveRuntime.PointMoveDebug.cs`
- `RobotControlV3DebugBridge.LiveRuntime.TeachingDebug.cs`
- `RobotControlV3DebugBridge.LiveRuntime.JointTcpDebug.cs`
- `RobotControlV3DebugBridge.LiveRuntime.Matrices.cs`
- `RobotControlV3DebugBridge.LiveRuntime.Summaries.cs`
- `RobotControlV3DebugBridge.LiveRuntime.StageViewportDebug.cs`
