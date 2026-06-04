// ControlRoomApp.cs — 앱 부트스트랩. Scene 시작 시 1회 실행.
// 책임: default_robots.json 로드 → ControlRoomState 초기화 → Binder/Sim 깨우기.
// MonoBehaviour Awake에서 단방향 초기화. 직접 ROS/DB 호출 금지(Phase 5+).
using System;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using URHYNIX.ControlRoom.Data;

namespace URHYNIX.ControlRoom.App
{
    public class ControlRoomApp : MonoBehaviour
    {
        const string RobotsJsonResourcePath = "RobotConfig/default_robots";
        const string DefaultRosIp = "urhynix-robot.local";   // 젠지. TODO Phase 5: RobotInfo JSON에서 읽기.
        const int DefaultRosPort = 10000;

        void Awake()
        {
            LoadRobots();
            ConfigureRos();
            ControlRoomEvents.RaiseLogAdded("system", "INFO", "ControlRoom 부팅 완료.");
            ControlRoomEvents.RaiseLogAdded("gemma", "INFO", "⚪ Gemma 4 12B 대기 중");
        }

        void ConfigureRos()
        {
            var ros = ROSConnection.GetOrCreateInstance();
            ros.RosIPAddress = DefaultRosIp;
            ros.RosPort = DefaultRosPort;
            Debug.Log($"[ControlRoomApp] ROS IP set: {DefaultRosIp}:{DefaultRosPort}");
        }

        void LoadRobots()
        {
            var ta = Resources.Load<TextAsset>(RobotsJsonResourcePath);
            if (ta == null)
            {
                Debug.LogWarning($"[ControlRoomApp] {RobotsJsonResourcePath}.json 누락 — 빈 로봇 목록으로 시작");
                return;
            }
            try
            {
                var list = JsonUtility.FromJson<RobotInfoList>(ta.text);
                if (list?.robots != null)
                {
                    ControlRoomState.Instance.Robots.Clear();
                    ControlRoomState.Instance.Robots.AddRange(list.robots);
                    Debug.Log($"[ControlRoomApp] 로봇 {list.robots.Length}대 로드");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[ControlRoomApp] default_robots.json 파싱 실패: {e.Message}");
            }
        }
    }
}
