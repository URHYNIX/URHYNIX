// CameraPanelView.cs — 카메라 패널 View. CameraStreamSubscriber로부터 라이브 Texture 받아 ui:Image에 표시.
// UXML element 잡기 + frame 이벤트 구독만. ROS msg 타입은 직접 다루지 않음 (Ros/ 폴더 책임).
// Phase 2.7 — 젠지 (/tb3_2/camera/image_raw/compressed @ 30Hz) 라이브 연결.
using UnityEngine;
using UnityEngine.UIElements;
using URHYNIX.ControlRoom.App;
using URHYNIX.ControlRoom.Ros;

namespace URHYNIX.ControlRoom.UI
{
    public class CameraPanelView
    {
        readonly Image cameraImage;
        readonly Label hzLabel;
        readonly Label placeholderText;

        public CameraPanelView(VisualElement root)
        {
            cameraImage     = root.Q<Image>("camera-image");
            hzLabel         = root.Q<Label>("camera-hz");
            placeholderText = root.Q<Label>("camera-placeholder-text");

            if (hzLabel != null) hzLabel.text = "-- Hz";

            CameraStreamSubscriber.OnFrameUpdated += OnFrameUpdated;
            ControlRoomEvents.OnRobotChanged      += OnRobotChanged;
        }

        void OnFrameUpdated(Texture2D tex, float hz)
        {
            if (cameraImage != null && tex != null)
            {
                cameraImage.image = tex;
                if (placeholderText != null && placeholderText.style.display != DisplayStyle.None)
                    placeholderText.style.display = DisplayStyle.None;
            }
            if (hzLabel != null) hzLabel.text = $"{hz:F1} Hz";
        }

        void OnRobotChanged(string robotId)
        {
            ControlRoomEvents.RaiseLogAdded("camera", "INFO", $"카메라 토픽 전환 요청 → {robotId} (Phase 5 정식 결선)");
        }
    }
}
