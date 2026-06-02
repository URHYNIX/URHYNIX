// SensorCardListView.cs — 우측 패널 센서 카드 5종(가스/소음/조도/PIR/화재).
// OnSensorChanged 이벤트로 가스/소음/조도 갱신. PIR/화재는 시나리오 트리거 시 정상↔위험 토글.
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
                case "light": if (lightValue != null) lightValue.text = $"{value:F0} lx"; break;
            }
        }

        void OnRobotChanged(string robotId)
        {
            // 탭 전환 시 PIR/화재는 정상 reset (시나리오 초기화)
            SetSensorState(pirValue, "정상", "sensor-ok");
            SetSensorState(fireValue, "정상", "sensor-ok");
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
