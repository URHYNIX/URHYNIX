// SensorVerifyConsole.cs — Phase 2 검증 콘솔. unityctl exec로 호출하는 영구 자산.
// SensorRegistry.All 동적 순회 → 새 센서 추가 시 코드 0줄로 자동 포함.
// 호출: unityctl exec --code 'URHYNIX.ControlRoom.App.SensorVerifyConsole.Dump()'
// 보조: SwitchTo("tb3_2") / DumpRos() (토픽 매핑 확인)
// 함정: UXML 라벨 ID(sensor-light-value)와 SSOT sensorId(lux) 미스매치 — Phase 3에서 convention으로 통일.
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;
using URHYNIX.ControlRoom.Ros;
using URHYNIX.ControlRoom.Sensors;

namespace URHYNIX.ControlRoom.App
{
    public static class SensorVerifyConsole
    {
        // sensorId → UXML 라벨 ID 매핑.
        // Phase 2 미스매치 우회 (default_sensors.json은 lux/noise, 코드는 light/sound).
        // Phase 3 자동화 진입 시 convention `sensor-{sensorId}-value`로 통일하면 dict 제거 가능.
        static readonly Dictionary<string, string> SensorIdToLabelId = new()
        {
            { "battery", "battery-percent-label" }, // 특수: 탭마다 1개, 카드 X
            { "gas",     "sensor-gas-value" },
            { "noise",   "sensor-sound-value" },
            { "sound",   "sensor-sound-value" },
            { "lux",     "sensor-light-value" },
            { "light",   "sensor-light-value" },
            { "pir",     "sensor-pir-value" },
            { "fire",    "sensor-fire-value" },
        };

        public static string SwitchTo(string robotId)
        {
            ControlRoomState.Instance.SelectRobot(robotId);
            return $"selected -> {robotId}";
        }

        public static string Dump()
        {
            var s = ControlRoomState.Instance;
            var doc = UnityEngine.Object.FindObjectOfType<UIDocument>();
            var root = doc?.rootVisualElement;

            var sb = new StringBuilder();
            sb.AppendLine($"selected={s?.SelectedRobotId}");
            sb.AppendLine($"robots={(s?.Robots != null ? s.Robots.Count : 0)}");

            if (s?.Robots != null)
            {
                foreach (var r in s.Robots)
                {
                    sb.Append($"  [state {r.robotId} {r.displayName}] ");
                    if (s.LastSensorValues.TryGetValue(r.robotId, out var dict))
                    {
                        foreach (var kv in dict)
                            sb.Append($"{kv.Key}={kv.Value:F1} ");
                    }
                    else sb.Append("(empty)");
                    sb.AppendLine();
                }
            }

            sb.AppendLine("--- UI labels (현재 탭 기준) ---");
            foreach (var kv in SensorIdToLabelId)
            {
                var label = root?.Q<Label>(kv.Value);
                string text = label?.text ?? "(no label)";
                string classes = label != null ? string.Join("|", label.GetClasses()) : "";
                sb.AppendLine($"  {kv.Key,-8} → {kv.Value,-22} = '{text}' [{classes}]");
            }
            return sb.ToString();
        }

        public static string DumpRos()
        {
            var sb = new StringBuilder();
            sb.AppendLine("--- TopicRegistry 매핑 ---");
            foreach (var rid in new[] { "tb3_1", "tb3_2" })
            {
                sb.AppendLine($"  [{rid}]");
                sb.AppendLine($"    battery_state = {TopicRegistry.GetBatteryState(rid) ?? "(null)"}");
                sb.AppendLine($"    camera        = {TopicRegistry.GetCameraCompressed(rid) ?? "(null)"}");
                sb.AppendLine($"    ldr           = {TopicRegistry.GetLdrRaw(rid) ?? "(null)"}");
                sb.AppendLine($"    pir           = {TopicRegistry.GetPirState(rid) ?? "(null)"}");
            }
            return sb.ToString();
        }
    }
}
