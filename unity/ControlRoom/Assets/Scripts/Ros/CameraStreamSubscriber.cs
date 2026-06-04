// CameraStreamSubscriber.cs — ROS2 CompressedImage 토픽 구독 → JPEG decode → 정적 이벤트로 Texture 전달.
// unity-smoke `CameraStreamPanel.cs` 패턴을 ControlRoom UI Toolkit 베이스에 맞춰 재이식.
// 토픽 정본: 젠지 `/tb3_2/camera/image_raw/compressed` @ 30Hz (docs/ref/tech/VISION-CAMERA.md).
// View 측은 OnFrameUpdated 이벤트만 구독 — ROS msg 타입을 직접 만지지 않음.
using System;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Sensor;
using URHYNIX.ControlRoom.App;

namespace URHYNIX.ControlRoom.Ros
{
    public class CameraStreamSubscriber : MonoBehaviour
    {
        [Header("ROS Topic")]
        [Tooltip("기본값: 젠지 Pi Camera v2 (/tb3_2/camera/image_raw/compressed)")]
        public string topicName = "/tb3_2/camera/image_raw/compressed";

        [Header("Display")]
        [Tooltip("로그/UI 라벨에 표시할 이름")]
        public string displayLabel = "젠지";

        // View가 구독하는 정적 이벤트. (Texture2D currentFrame, float hz)
        public static event Action<Texture2D, float> OnFrameUpdated;

        Texture2D streamTexture;
        int frameCount;
        float lastHzCheck;
        float currentHz;
        bool subscribed;
        bool firstFrameLogged;

        void Start()
        {
            streamTexture = new Texture2D(2, 2, TextureFormat.RGB24, false);

            var ros = ROSConnection.GetOrCreateInstance();
            ros.Subscribe<CompressedImageMsg>(topicName, OnImageReceived);
            subscribed = true;
            lastHzCheck = Time.time;

            Debug.Log($"[CameraStreamSubscriber:{displayLabel}] subscribed → {topicName}");
        }

        void OnImageReceived(CompressedImageMsg msg)
        {
            if (streamTexture == null) return;
            if (streamTexture.LoadImage(msg.data))
            {
                frameCount++;
                if (!firstFrameLogged)
                {
                    firstFrameLogged = true;
                    ControlRoomEvents.RaiseLogAdded(
                        "camera",
                        "INFO",
                        $"🟢 Pi Camera 연결됨 ({displayLabel} · {topicName})"
                    );
                }
                OnFrameUpdated?.Invoke(streamTexture, currentHz);
            }
        }

        void Update()
        {
            if (!subscribed) return;
            float dt = Time.time - lastHzCheck;
            if (dt >= 1.0f)
            {
                currentHz = frameCount / dt;
                frameCount = 0;
                lastHzCheck = Time.time;
            }
        }

        void OnDestroy()
        {
            if (streamTexture != null) Destroy(streamTexture);
        }
    }
}
