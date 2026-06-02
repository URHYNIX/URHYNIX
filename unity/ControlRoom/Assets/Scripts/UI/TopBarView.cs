// TopBarView.cs — 상단바 시계 + 경보 카운트만. 로봇 탭/전원은 Phase 2.5에서 별도 View로 분리(SRP).
using UnityEngine.UIElements;
using URHYNIX.ControlRoom.App;

namespace URHYNIX.ControlRoom.UI
{
    public class TopBarView
    {
        readonly Label clockLabel;
        readonly Label alertCountLabel;
        int alertCount;

        public TopBarView(VisualElement root)
        {
            clockLabel      = root.Q<Label>("clock-label");
            alertCountLabel = root.Q<Label>("alert-count-label");

            ControlRoomEvents.OnAlert += OnAlert;
        }

        void OnAlert(int severity, string message)
        {
            alertCount++;
            if (alertCountLabel != null) alertCountLabel.text = $"경보 {alertCount}";
        }

        public void UpdateClock(string text)
        {
            if (clockLabel != null) clockLabel.text = text;
        }
    }
}
