// Folder: UI - HUD/view components only; no kinematics logic.
using UnityEngine;
using UnityEngine.UIElements;

namespace KineTutor3D.UI.RobotControlV3
{
    /// <summary>
    /// Legacy I/O host compatibility shim. Gripper operation now lives in EasyMotionController.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class IoPanelController : MonoBehaviour
    {
        [SerializeField] private UIDocument document;

        private VisualElement root;
        private VisualElement ioPanelHost;
        private VisualElement ioSheetHost;
        private bool isInitialized;
        private bool isInitializing;

        private void OnEnable()
        {
            TryInitialize();
        }

        private void OnDisable()
        {
            isInitialized = false;
            isInitializing = false;
        }

        public bool ForceInitialize()
        {
            return TryInitialize();
        }

        public string GetDebugSummary()
        {
            return $"initialized={isInitialized}; deprecated=True; mergedInto=EasyMotion; panelChildren={ioPanelHost?.childCount ?? -1}; sheetChildren={ioSheetHost?.childCount ?? -1}";
        }

        public void SetShellState(string activeNavSection, string activeWorkTab, string activeTabletTab)
        {
            if (!isInitialized)
            {
                TryInitialize();
            }

            HideLegacyHosts();
        }

        private bool TryInitialize()
        {
            if (isInitialized)
            {
                return true;
            }

            if (isInitializing)
            {
                return false;
            }

            isInitializing = true;
            try
            {
                document ??= GetComponent<UIDocument>();
                root = document?.rootVisualElement;
                if (root == null)
                {
                    return false;
                }

                ioPanelHost = root.Q<VisualElement>("IoPanelHost");
                ioSheetHost = root.Q<VisualElement>("IoSheetHost");
                if (ioPanelHost == null || ioSheetHost == null)
                {
                    return false;
                }

                HideLegacyHosts();
                isInitialized = true;
                return true;
            }
            finally
            {
                isInitializing = false;
            }
        }

        private void HideLegacyHosts()
        {
            ioPanelHost?.Clear();
            ioSheetHost?.Clear();
            ioPanelHost?.EnableInClassList("rc-hidden", true);
            ioSheetHost?.EnableInClassList("rc-hidden", true);
        }
    }
}
