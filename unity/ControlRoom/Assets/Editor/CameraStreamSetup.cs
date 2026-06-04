// CameraStreamSetup.cs — ControlRoomMain.unity에 CameraStreamSubscriber GameObject를 idempotent하게 추가.
// Scene이 이미 존재해야 함 (ControlRoomSceneSetup 선행). 재실행 안전 (같은 이름이면 컴포넌트만 보장).
// 메뉴: URHYNIX/Setup Camera Stream (Genji)
// CLI:
//   /Applications/Unity/Hub/Editor/6000.3.16f1/Unity.app/Contents/MacOS/Unity \
//     -batchmode -quit -nographics \
//     -projectPath /Users/family/jason/URHYNIX/unity/ControlRoom \
//     -executeMethod URHYNIX.ControlRoom.Editor.CameraStreamSetup.Setup \
//     -logFile /tmp/unity-camera-stream-setup.log
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using URHYNIX.ControlRoom.Ros;

namespace URHYNIX.ControlRoom.Editor
{
    public static class CameraStreamSetup
    {
        const string ScenePath = "Assets/Scenes/ControlRoomMain.unity";
        const string GameObjectName = "CameraStreamSubscriber_Genji";
        const string DefaultTopic = "/tb3_2/camera/image_raw/compressed";
        const string DefaultLabel = "젠지";

        [MenuItem("URHYNIX/Setup Camera Stream (Genji)")]
        public static void Setup()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Debug.LogError($"[CameraStreamSetup] Scene 열기 실패: {ScenePath} (ControlRoomSceneSetup 먼저 실행하세요)");
                return;
            }

            var existing = GameObject.Find(GameObjectName);
            GameObject go;
            if (existing != null)
            {
                go = existing;
                Debug.Log($"[CameraStreamSetup] GameObject 재사용: {GameObjectName}");
            }
            else
            {
                go = new GameObject(GameObjectName);
                Debug.Log($"[CameraStreamSetup] GameObject 신규 생성: {GameObjectName}");
            }

            var sub = go.GetComponent<CameraStreamSubscriber>();
            if (sub == null)
            {
                sub = go.AddComponent<CameraStreamSubscriber>();
                Debug.Log($"[CameraStreamSetup] CameraStreamSubscriber 컴포넌트 추가");
            }

            if (string.IsNullOrEmpty(sub.topicName))     sub.topicName     = DefaultTopic;
            if (string.IsNullOrEmpty(sub.displayLabel))  sub.displayLabel  = DefaultLabel;

            EditorUtility.SetDirty(sub);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log($"[CameraStreamSetup] Scene 저장 완료 → topic={sub.topicName} label={sub.displayLabel}");
        }
    }
}
