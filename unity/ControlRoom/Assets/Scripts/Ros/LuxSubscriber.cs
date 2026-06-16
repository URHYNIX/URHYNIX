// LuxSubscriber.cs — ROS2 /sensors/ldr (std_msgs/Int32, raw A0 0~1023) 구독 → percent 환산.
// Phase 2.8 듀얼: 인스턴스 1개당 robotId 1개 (현재 tb3_2 젠지만 결선). topicName 비우면 TopicRegistry lookup.
// Arduino LDR 회로: 5V — LDR — A0 — 10kΩ — GND. 빛↑ → LDR저항↓ → A0↑ → percent↑.
// 환산: 실측 캘리브레이션 — rawMin(손가락 가림 ≈ 30)/rawMax(밝은 실내 ≈ 300)를 Inspector 노출.
//   환경 따라 조정. raw 0~1023 전체 매핑은 시연 실내에서 비현실적(LDR + 10k 분압 → 실내는 ~50~250 정도).
// 함정: arduino_bridge가 latching 아님 → 첫 메시지까지 UI 미갱신 정상.
// Phase 2 alias: 이 Subscriber는 sensorId="light"로 SetSensorValue 호출 (코드 일관성).
//   SSOT default_sensors.json은 "lux"라 Phase 3 SensorRegistry 자동화 진입 시 통일 예정.
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Std;
using URHYNIX.ControlRoom.App;

namespace URHYNIX.ControlRoom.Ros
{
    public class LuxSubscriber : MonoBehaviour
    {
        [Header("Identity")]
        [Tooltip("robotId — \"tb3_2\" 젠지만 현재 결선. default_robots.json과 1:1.")]
        public string robotId = "tb3_2";

        [Header("ROS Topic")]
        [Tooltip("비워두면 robotId 기준으로 TopicRegistry에서 lookup")]
        public string topicName = "";

        [Header("Display")]
        [Tooltip("로그 라벨용 표시명")]
        public string displayLabel = "젠지조도";

        [Header("LDR raw range (calibration)")]
        [Tooltip("환산 0% 기준 raw — 손가락 가림 시 실측값 근처 (실측 51 → 30 권장)")]
        public int rawMin = 30;
        [Tooltip("환산 100% 기준 raw — 밝은 실내/직사광에서 측정한 최대치 (실측 195 → 300 권장)")]
        public int rawMax = 300;

        [Header("Disconnect detection")]
        [Tooltip("메시지 미수신 N초 후 경고")]
        public float disconnectTimeoutSeconds = 5f;

        bool subscribed;
        bool firstMessageLogged;
        float lastMessageTime = -1f;
        bool warnedDisconnect;

        void Start()
        {
            if (string.IsNullOrEmpty(topicName))
                topicName = TopicRegistry.GetLdrRaw(robotId);

            if (string.IsNullOrEmpty(topicName))
            {
                Debug.LogError($"[LuxSubscriber:{displayLabel}] topic resolve 실패 — robotId={robotId} 미등록");
                return;
            }

            var ros = ROSConnection.GetOrCreateInstance();
            ros.Subscribe<Int32Msg>(topicName, OnLdrMsg);
            subscribed = true;

            Debug.Log($"[LuxSubscriber:{displayLabel}] subscribed → {topicName} (robotId={robotId})");
        }

        void Update()
        {
            if (!subscribed || lastMessageTime < 0 || warnedDisconnect) return;
            if (Time.time - lastMessageTime > disconnectTimeoutSeconds)
            {
                warnedDisconnect = true;
                ControlRoomEvents.RaiseLogAdded(
                    "sensor", "WARN",
                    $"⚠️ 조도 토픽 끊김 {disconnectTimeoutSeconds:F0}초 이상 ({displayLabel} · {topicName})"
                );
            }
        }

        void OnLdrMsg(Int32Msg msg)
        {
            lastMessageTime = Time.time;

            if (warnedDisconnect)
            {
                warnedDisconnect = false;
                ControlRoomEvents.RaiseLogAdded(
                    "sensor", "INFO",
                    $"🟢 조도 토픽 복구 ({displayLabel})"
                );
            }

            int raw = msg.data;
            float span = Mathf.Max(1, rawMax - rawMin);
            float percent = Mathf.Clamp01((raw - rawMin) / span) * 100f;

            if (!firstMessageLogged)
            {
                firstMessageLogged = true;
                ControlRoomEvents.RaiseLogAdded(
                    "sensor",
                    "INFO",
                    $"🌞 조도 결선됨 ({displayLabel} · {topicName}) — raw={raw} → {percent:F1}%"
                );
            }

            // sensorId="light" — SensorCardListView가 light 케이스로 % + 상태 라벨 갱신.
            ControlRoomState.Instance.SetSensorValue(robotId, "light", percent);
        }
    }
}
