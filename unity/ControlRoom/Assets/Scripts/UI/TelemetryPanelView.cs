// TelemetryPanelView.cs — 우측 패널 배터리 게이지만. 센서 5종은 SensorCardListView, 하드웨어는 HardwarePanelView로 분리(SRP).
using UnityEngine.UIElements;
using URHYNIX.ControlRoom.App;

namespace URHYNIX.ControlRoom.UI
{
    public class TelemetryPanelView
    {
        readonly Label batteryPercentLabel;
        readonly VisualElement batteryBarFill;

        public TelemetryPanelView(VisualElement root)
        {
            batteryPercentLabel = root.Q<Label>("battery-percent-label");
            batteryBarFill      = root.Q<VisualElement>("battery-bar-fill");

            ControlRoomEvents.OnBatteryChanged += OnBatteryChanged;
        }

        bool IsCurrent(string robotId) =>
            robotId == ControlRoomState.Instance.SelectedRobotId;

        void OnBatteryChanged(string robotId, float percent)
        {
            if (!IsCurrent(robotId)) return;
            if (batteryPercentLabel != null) batteryPercentLabel.text = $"{percent:F1} %";
            if (batteryBarFill != null)
                batteryBarFill.style.width = Length.Percent(UnityEngine.Mathf.Clamp(percent, 0f, 100f));
        }
    }
}
