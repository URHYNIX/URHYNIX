// TopicRegistry.cs — ROS2 토픽 이름 SSOT. 토픽 하드코딩 금지(Ros/CLAUDE.md 규칙).
// 카메라 토픽 정본: 젠지 /tb3_2/camera/image_raw/compressed (Pi Camera v2 IMX219, 30Hz)
//                  티원 /tb3_1/camera/color/image_raw/compressed (RealSense D435, 30Hz)
// robotId 컨벤션: "tb3_1" 티원 / "tb3_2" 젠지 (default_robots.json과 1:1).
namespace URHYNIX.ControlRoom.Ros
{
    public static class TopicRegistry
    {
        public const string GenjiCameraCompressed = "/tb3_2/camera/image_raw/compressed";
        public const string T1CameraCompressed    = "/tb3_1/camera/color/image_raw/compressed";

        public static string GetCameraCompressed(string robotId)
        {
            switch (robotId)
            {
                case "tb3_1": return T1CameraCompressed;
                case "tb3_2": return GenjiCameraCompressed;
                default:      return null;
            }
        }
    }
}
