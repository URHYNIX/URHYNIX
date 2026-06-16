// SensorCardListView.cs — 우측 패널 센서 카드 5종(가스/소음/조도/PIR/화재).
// OnSensorChanged 이벤트로 가스/소음/조도/PIR 갱신. 화재는 시나리오 트리거 시 정상↔위험 토글.
// 조도(light): 0~100% + 5단계 상태 라벨(매우어두움/어두움/보통/밝음/매우밝음).
// PIR(pir): 0/1 → 감지 안 됨/감지! + USS sensor-ok/sensor-danger 토글.
// Phase 3에서 default_sensors.json + SensorRegistry 기반 자동 생성으로 swap.
using UnityEngine.UIElements;
using URHYNIX.ControlRoom.App;

namespace URHYNIX.ControlRoom.UI
{
    public class SensorCardListView
    {
        readonly Label gasValue;
        readonly Label soundValue;
        readonly Label lightValue;
        readonly Label pirValue;
        readonly Label fireValue;

        public SensorCardListView(VisualElement root)
        {
            gasValue   = root.Q<Label>("sensor-gas-value");
            soundValue = root.Q<Label>("sensor-sound-value");
            lightValue = root.Q<Label>("sensor-light-value");
            pirValue   = root.Q<Label>("sensor-pir-value");
            fireValue  = root.Q<Label>("sensor-fire-value");

            ControlRoomEvents.OnSensorChanged    += OnSensorChanged;
            ControlRoomEvents.OnRobotChanged     += OnRobotChanged;
            ControlRoomEvents.OnScenarioTriggered += OnScenarioTriggered;
        }

        bool IsCurrent(string robotId) =>
            robotId == ControlRoomState.Instance.SelectedRobotId;

        void OnSensorChanged(string robotId, string sensorId, float value)
        {
            if (!IsCurrent(robotId)) return;
            switch (sensorId)
            {
                case "gas":   if (gasValue   != null) gasValue.text   = $"{value:F0}"; break;
                case "sound": if (soundValue != null) soundValue.text = $"{value:F0}"; break;
                case "light":
                    if (lightValue == null) break;
                    string label = value switch
                    {
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
            }
        }

        void OnRobotChanged(string robotId)
        {
            // 탭 전환 시 State 캐시에서 즉시 redraw — Subscriber는 두 로봇 모두 항상 sub중.
            // 메시지 도착 대기 없이 마지막 알려진 값 그대로 표시.
            var s = ControlRoomState.Instance;
            if (s != null && s.LastSensorValues != null
                && s.LastSensorValues.TryGetValue(robotId, out var dict))
            {
                if (dict.TryGetValue("gas",   out var g)) OnSensorChanged(robotId, "gas",   g);
                if (dict.TryGetValue("sound", out var n)) OnSensorChanged(robotId, "sound", n);
                if (dict.TryGetValue("noise", out var n2)) OnSensorChanged(robotId, "sound", n2); // alias
                if (dict.TryGetValue("light", out var l)) OnSensorChanged(robotId, "light", l);
                if (dict.TryGetValue("lux",   out var l2)) OnSensorChanged(robotId, "light", l2); // alias
                if (dict.TryGetValue("pir",   out var p)) OnSensorChanged(robotId, "pir",   p);
                if (dict.TryGetValue("fire",  out var f)) OnSensorChanged(robotId, "fire",  f);
                // PIR/화재가 dict에 없으면 reset
                if (!dict.ContainsKey("pir"))  SetSensorState(pirValue,  "감지 안 됨", "sensor-ok");
                if (!dict.ContainsKey("fire")) SetSensorState(fireValue, "정상",       "sensor-ok");
            }
            else
            {
                // State 비었음 → 전체 reset
                if (gasValue   != null) gasValue.text   = "--";
                if (soundValue != null) soundValue.text = "--";
                if (lightValue != null) lightValue.text = "--";
                SetSensorState(pirValue,  "감지 안 됨", "sensor-ok");
                SetSensorState(fireValue, "정상",       "sensor-ok");
            }
        }

        void OnScenarioTriggered(string scenarioId)
        {
            switch (scenarioId)
            {
                case "intruder": SetSensorState(pirValue, "감지!", "sensor-danger"); break;
                case "fire":     SetSensorState(fireValue, "위험!", "sensor-danger"); break;
            }
        }

        void SetSensorState(Label label, string text, string statusClass)
        {
            if (label == null) return;
            label.text = text;
            label.RemoveFromClassList("sensor-ok");
            label.RemoveFromClassList("sensor-warn");
            label.RemoveFromClassList("sensor-danger");
            label.AddToClassList(statusClass);
        }
    }
}
