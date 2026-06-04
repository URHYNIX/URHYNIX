// SensorInfo.cs — 센서 메타 정보 POCO. default_sensors.json 1행과 1:1 매핑.
// 외부 의존 없는 순수 데이터. JSON 직렬화 친화 (JsonUtility).
using System;

namespace URHYNIX.ControlRoom.Data
{
    [Serializable]
    public class SensorInfo
    {
        public string sensorId;          // "gas" / "noise" / "lux" / "pir" / "fire"
        public string displayName;       // "가스" / "소음" / "조도" / "PIR" / "화재"
        public string sensorType;        // "analog" / "digital" / "boolean"
        public string unit;              // "ppm" / "dB" / "lux" / "" / ""
        public string topicName;         // 예: "/tb3_2/sensor/gas" (Phase 5에서 사용)
        public float warningThreshold;   // 경고 임계값 (sensorType=boolean이면 0/1 의미)
        public string iconName;          // IconNames 상수와 매핑
        public string robotId;           // 어느 로봇 소속인지 ("tb3_1" / "tb3_2")
    }

    [Serializable]
    public class SensorInfoList
    {
        public SensorInfo[] sensors;
    }
}
