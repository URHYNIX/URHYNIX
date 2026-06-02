// CameraPanelView.cs — 카메라 placeholder. 단색 텍스처를 배경으로 깔고 라벨/Hz 표시.
// Phase 5에서 ROS-TCP-Connector + CompressedImage 구독 코드 추가.
using UnityEngine;
using UnityEngine.UIElements;
using URHYNIX.ControlRoom.App;

namespace URHYNIX.ControlRoom.UI
{
    public class CameraPanelView
    {
        readonly VisualElement imagePlaceholder;
        readonly Label hzLabel;

        public CameraPanelView(VisualElement root)
        {
            imagePlaceholder = root.Q<VisualElement>("camera-image");
            hzLabel          = root.Q<Label>("camera-hz");

            // placeholder 색상은 USS에서 처리. 후속 phase에서 RawImage 대체.
            if (hzLabel != null) hzLabel.text = "0.0 Hz (placeholder)";

            ControlRoomEvents.OnRobotChanged += OnRobotChanged;
        }

        void OnRobotChanged(string robotId)
        {
            ControlRoomEvents.RaiseLogAdded("camera", "INFO", $"카메라 토픽 전환 요청 → {robotId} (Phase 5 ROS 연결 필요)");
        }
    }
}
