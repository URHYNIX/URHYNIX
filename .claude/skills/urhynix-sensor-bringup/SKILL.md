---
name: urhynix-sensor-bringup
description: URHYNIX 젠지 Arduino 센서(LDR/PIR + 후속 가스/화재) 결선 표준 — arduino_bridge.py 자급 launch + ROS2 std_msgs/Int32·Bool Subscriber + LDR 캘리브레이션(rawMin/rawMax) + 5단계 라벨 + PIR latching X 함정. urhynix-battery-bringup 자매. 새 센서 추가 시 1:1 재사용.
---

# urhynix-sensor-bringup

> URHYNIX Unity 관제 UI에 **젠지 Arduino 센서**(현재 조도 LDR + 인체감지 PIR, 후속 가스/화재)를 ROS2 → Unity Subscriber → 우측 패널 카드로 결선하는 표준. `urhynix-battery-bringup`의 자매 스킬.

## 사용 호스트

| 로봇 | IP / 사용자 | Arduino USB | 발행 토픽 |
|---|---|---|---|
| 젠지 (tb3_2) | `kim@192.168.10.87` | `/dev/tb3_arduino` (심링크) | `/sensors/ldr` (Int32 raw 0~1023), `/sensors/pir` (Bool) |

티원(tb3_1)은 현재 Arduino 미연결 — 후속 작업 범위.

## arduino_bridge.py 표준 launch

```bash
# 1) 심링크 박기 (USB 재꽂이/재부팅마다 ttyACM 번호 변동 → 함정 #26)
ssh kim@192.168.10.87 'echo "---ttyACM 모델 확인---";
  for d in /dev/ttyACM*; do
    v=$(udevadm info -q property -n "$d" 2>/dev/null | grep ID_VENDOR= | head -1);
    echo "$d $v";
  done'
# 결과 예: /dev/ttyACM0 ID_VENDOR=Arduino__www.arduino.cc_   ← 이걸 심링크

ssh kim@192.168.10.87 'echo 1234 | sudo -S ln -sf /dev/ttyACM<N> /dev/tb3_arduino; ls -la /dev/tb3_arduino'

# 2) bridge launch (base64 self-kill 회피 패턴, 함정 #24)
ssh kim@192.168.10.87 'nohup bash -c "
  source /opt/ros/jazzy/setup.bash &&
  export ROS_DOMAIN_ID=230 &&
  export URHYNIX_ROBOT_ID=tb3_2 &&
  exec python3 ~/arduino_bridge.py
" > /tmp/arduino_bridge.log 2>&1 & disown; echo BRIDGE_FIRED'
```

bridge 발행:
- `/sensors/ldr` `std_msgs/Int32` (raw A0 0~1023, edge-trigger — Arduino 스케치가 변화 시에만 라인 발신)
- `/sensors/pir` `std_msgs/Bool` (true=motion, false=clear)

## TopicRegistry SSOT (Unity 측)

```csharp
// Assets/Scripts/Ros/TopicRegistry.cs
public const string GenjiLdrRaw   = "/sensors/ldr";
public const string GenjiPirState = "/sensors/pir";

public static string GetLdrRaw(string robotId)   => robotId == "tb3_2" ? GenjiLdrRaw   : null;
public static string GetPirState(string robotId) => robotId == "tb3_2" ? GenjiPirState : null;
```

Phase 2: bridge가 root namespace `/sensors/*`로 발행. SSOT `default_sensors.json`은 `/tb3_2/sensor/*` 약속 — Phase 3에서 정합.

## Unity Subscriber 1:1 모델

| Subscriber | 메시지 타입 | sensorId | Inspector 필드 |
|---|---|---|---|
| `LuxSubscriber` | `Std.Int32Msg` | `"light"` (alias of SSOT `lux`) | `rawMin=30`, `rawMax=300`, `disconnectTimeoutSeconds=5` |
| `PirSubscriber` | `Std.BoolMsg` | `"pir"` | (없음 — latching 아니라 timeout 검사 불필요) |

코드 골격은 `BatterySubscriber.cs` 패턴 1:1 복제:
- `[Header("Identity")]` `robotId="tb3_2"` (현재 젠지만 결선)
- `[Header("ROS Topic")]` `topicName=""` (빈값 → TopicRegistry lookup)
- `[Header("Display")]` `displayLabel="젠지조도"` / `"젠지PIR"`
- `ros.Subscribe<MsgType>(topicName, OnMsg)` → `ControlRoomState.SetSensorValue(robotId, sensorId, value)`

## LDR 캘리브레이션 공식

```csharp
float span = Mathf.Max(1, rawMax - rawMin);
float percent = Mathf.Clamp01((raw - rawMin) / span) * 100f;
```

실측 기준 기본값:
- `rawMin=30` (손가락 가림 raw ~50 → percent 7.4%)
- `rawMax=300` (밝은 실내 raw ~200 → percent 63%, 책상 램프 raw 300+ → 100%)

⚠️ raw 0~1023 전체 매핑은 시연 실내에서 비현실적 (LDR + 10k 분압 → raw 50~250). Inspector에서 환경 따라 조정 권장.

## SensorCardListView 5단계 라벨

```csharp
case "light":
    string label = value switch {
        > 80f => "매우 밝음",
        > 60f => "밝음",
        > 40f => "보통",
        > 20f => "어두움",
        _     => "매우 어두움",
    };
    lightValue.text = $"{value:F0}% · {label}";
    break;
case "pir":
    bool detected = value >= 0.5f;
    SetSensorState(pirValue,
        detected ? "감지!" : "감지 안 됨",
        detected ? "sensor-danger" : "sensor-ok");
    break;
```

PIR 라벨 한글 컨벤션: **`감지!` / `감지 안 됨`** (`OnScenarioTriggered`도 동일 토글).

## OnRobotChanged 캐시 redraw (즉각 표시 패턴)

```csharp
void OnRobotChanged(string robotId)
{
    var s = ControlRoomState.Instance;
    if (s != null && s.LastSensorValues != null
        && s.LastSensorValues.TryGetValue(robotId, out var dict))
    {
        // Subscriber는 두 로봇 모두 항상 sub중. 탭 전환 시 캐시에서 즉시 redraw.
        if (dict.TryGetValue("light", out var l)) OnSensorChanged(robotId, "light", l);
        if (dict.TryGetValue("pir",   out var p)) OnSensorChanged(robotId, "pir",   p);
        // ...other sensors
    }
}
```

→ 메시지 도착 대기 없이 마지막 알려진 값 즉시 표시. **TelemetryPanelView(배터리) 패턴과 동일**.

## Scene 박는 패턴

| GameObject | 컴포넌트 | Inspector 값 |
|---|---|---|
| `LuxSubscriber_G` | `LuxSubscriber` | `robotId=tb3_2`, `topicName=""`, `displayLabel=젠지조도`, `rawMin=30`, `rawMax=300` |
| `PirSubscriber_G` | `PirSubscriber` | `robotId=tb3_2`, `topicName=""`, `displayLabel=젠지PIR` |

Editor 불가 시 `.unity` YAML 직접 patch → `unity-scene-yaml-patch` 스킬 참고.

## 함정표

| # | 함정 | 우회 |
|---|---|---|
| 1 | **PIR latching 없음** — Bool 메시지는 motion/clear 변화 시에만 발행. Unity Subscribe 직후 메시지 없음 정상. | UI 초기값 `감지 안 됨` (sensor-ok) 유지. ros2 topic pub --rate로 강제 발행 검증 가능. |
| 2 | **LDR edge-trigger** — Arduino 스케치가 `[LDR] A0=N` 라인을 변화 시에만 발신. 가만히 있으면 메시지 없음. | 라이트/손가락 가림으로 변화 유도. 또는 Arduino 스케치 수정해서 5초마다 강제 발신. |
| 3 | **arduino_bridge ROS_DOMAIN_ID 미설정** → 토픽 invisible | bridge launch에 `export ROS_DOMAIN_ID=230` 명시 (.bashrc 의존 X). |
| 4 | **`/dev/tb3_arduino` 심링크 stale** — 재부팅/재꽂이로 ttyACM 번호 변동 (urhynix-battery-bringup 함정 #26과 동일) | udev rule 영구 매핑 권장 (`SUBSYSTEM=="tty", ATTRS{idVendor}=="2341", ATTRS{idProduct}=="0043", SYMLINK+="tb3_arduino"`) |
| 5 | **LDR rawMax 기본값 1023이 시연 실내에서 비현실적** — raw 195도 19%로 "매우 어두움" 오인 | rawMin=30, rawMax=300 Inspector에서 조정. 환경 측정 후 보정. |
| 6 | **sensorId 미스매치** — SSOT `default_sensors.json` = `lux`/`noise`, 코드 = `light`/`sound` | Phase 2는 alias dict로 우회 (`SensorVerifyConsole.SensorIdToLabelId`). Phase 3 자동화 진입 시 통일. |

## 검증 명령

```bash
# 1) bridge 살아있는지
ssh kim@192.168.10.87 'pgrep -fl arduino_bridge | head; tail -5 /tmp/arduino_bridge.log'

# 2) 토픽 publisher 확인 (ROS_DOMAIN_ID=230)
ssh kim@192.168.10.87 'source /opt/ros/jazzy/setup.bash; export ROS_DOMAIN_ID=230;
  ros2 topic info /sensors/ldr; ros2 topic info /sensors/pir'
# 기대: Publisher count: 1

# 3) LDR raw echo
ssh kim@192.168.10.87 'source /opt/ros/jazzy/setup.bash; export ROS_DOMAIN_ID=230;
  timeout 10 ros2 topic echo /sensors/ldr --once 2>&1 | head'
# 기대: data: 50~300 (환경 따라)

# 4) PIR 강제 발행 (Unity 측 UI 토글 검증용)
ssh kim@192.168.10.87 'source /opt/ros/jazzy/setup.bash; export ROS_DOMAIN_ID=230;
  ros2 topic pub /sensors/pir std_msgs/Bool "data: true" --rate 3 &
  sleep 4; pkill -f "topic pub"'

# 5) Unity 측 검증 — SensorVerifyConsole (urhynix-sensor-verify-console 스킬 참고)
unityctl exec --project ... --code 'URHYNIX.ControlRoom.App.SensorVerifyConsole.Dump()'
```

## 박물관 시연 매핑

| 시연 흐름 | 토픽 | UI 위치 |
|---|---|---|
| 조도 게이지 (밝음/어두움 5단계) | `/sensors/ldr` | 우측 패널 `sensor-light-value` |
| 인체감지 (감지!/감지 안 됨 토글) | `/sensors/pir` | 우측 패널 `sensor-pir-value` |
| 환경 어두움 → 박물관 폐관 모드 시나리오 | LDR raw < 80 (`매우 어두움`) | LogPanelView 자동 trigger |
| 침입자 감지 → 보안 경보 시나리오 | PIR Bool true | `OnScenarioTriggered("intruder")` |

## 후속 가스/화재 추가 절차 (재사용 패턴)

1. Arduino 스케치에 가스/화재 센서 추가 + `[GAS] N=...`, `[FIRE] N=...` 라인 발신
2. `arduino_bridge.py`에 RE 정규식 + publisher 2개 추가 (`/sensors/gas` Float32, `/sensors/fire` Bool)
3. `TopicRegistry.cs`에 `GenjiGas`/`GenjiFire` 상수 + lookup 추가
4. `Assets/Scripts/Ros/GasSubscriber.cs`/`FireSubscriber.cs` 신규 (LuxSubscriber/PirSubscriber 패턴 1:1 복제)
5. `SensorCardListView.cs` switch에 `case "gas":`/`case "fire":` 분기 추가
6. Scene에 `GasSubscriber_G`/`FireSubscriber_G` GameObject 박기
7. `SensorVerifyConsole.SensorIdToLabelId`에 매핑 1줄 추가
8. 검증 명령 §1~§5 반복

## 관련 자산

- 코드 — `unity/ControlRoom/Assets/Scripts/Ros/{TopicRegistry,LuxSubscriber,PirSubscriber}.cs`
- 코드 — `unity/ControlRoom/Assets/Scripts/UI/SensorCardListView.cs` (light/pir 분기 + OnRobotChanged 캐시 redraw)
- 코드 — `unity/ControlRoom/Assets/Scripts/App/SensorVerifyConsole.cs` (영구 검증 콘솔)
- 스크립트 — `scripts/arduino_bridge.py` (시리얼 → ROS publisher)
- SSOT — `unity/ControlRoom/Assets/Resources/SensorConfig/default_sensors.json`
- 자매 스킬 — `urhynix-battery-bringup` (배터리 트랙, 같은 1:1 모델)
- 자매 스킬 — `arduino-flash` (Arduino 펌웨어 + 시리얼 라벨 표준)
- 보조 스킬 — `urhynix-sensor-verify-console` (검증 콘솔 패턴)
- 보조 스킬 — `unity-scene-yaml-patch` (Editor 불가 시 Scene 박기)
