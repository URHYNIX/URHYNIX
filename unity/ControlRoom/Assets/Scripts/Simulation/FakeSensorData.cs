// FakeSensorData.cs — 실기기 미연결 상태에서 쓰는 fake 센서값 generator.
// Perlin/Sin 기반(random walk 아님)이라 부드러운 변화. 1초당 1~2회 발화.
// 실제 ROS 연결되면 본 컴포넌트는 비활성(enabled=false).
using UnityEngine;
using URHYNIX.ControlRoom.App;

namespace URHYNIX.ControlRoom.Simulation
{
    public class FakeSensorData : MonoBehaviour
    {
        [Header("Tick rate (Hz)")]
        public float tickHz = 1.5f;

        [Header("Robot IDs to fake")]
        public string[] robotIds = { "tb3_1", "tb3_2" };

        float nextTick;
        float startTime;

        void OnEnable()
        {
            startTime = Time.time;
            nextTick = Time.time + 0.5f;
        }

        void Update()
        {
            if (Time.time < nextTick) return;
            nextTick = Time.time + (1f / Mathf.Max(0.1f, tickHz));

            float t = Time.time - startTime;
            foreach (var rid in robotIds)
            {
                // Battery: 87% 근처에서 ±3% 천천히 변동 (Perlin)
                float battery = 87f + (Mathf.PerlinNoise(t * 0.05f, rid.GetHashCode() * 0.001f) - 0.5f) * 6f;
                ControlRoomEvents.RaiseBatteryChanged(rid, battery);
                ControlRoomState.Instance.SetSensorValue(rid, "battery", battery);

                // Gas: 0~100, sin 기반 부드러운 파동
                float gas = 25f + Mathf.Sin(t * 0.6f + rid.GetHashCode() * 0.01f) * 8f;
                ControlRoomState.Instance.SetSensorValue(rid, "gas", gas);

                // Sound: 0~100, Perlin
                float sound = 30f + Mathf.PerlinNoise(t * 0.3f, 1f + rid.GetHashCode() * 0.001f) * 30f;
                ControlRoomState.Instance.SetSensorValue(rid, "sound", sound);

                // Light: 100~900 lux
                float light = 500f + Mathf.Sin(t * 0.15f + rid.GetHashCode() * 0.02f) * 250f;
                ControlRoomState.Instance.SetSensorValue(rid, "light", light);
            }
        }

        void OnDisable()
        {
            // Domain Reload 안전: 다음 OnEnable에서 시작점 재계산.
            nextTick = 0;
        }
    }
}
