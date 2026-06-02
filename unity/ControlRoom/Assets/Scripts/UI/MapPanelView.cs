// MapPanelView.cs — 중앙 맵 패널 2D/3D 토글. 2D는 placeholder, 3D는 Phase 6 안내.
// 토글 시 visible class on/off + hidden class on/off로 컨테이너 전환.
using UnityEngine.UIElements;
using URHYNIX.ControlRoom.App;

namespace URHYNIX.ControlRoom.UI
{
    public class MapPanelView
    {
        readonly Button btn2D;
        readonly Button btn3D;
        readonly VisualElement container2D;
        readonly VisualElement container3D;

        public MapPanelView(VisualElement root)
        {
            btn2D       = root.Q<Button>("btn-map-2d");
            btn3D       = root.Q<Button>("btn-map-3d");
            container2D = root.Q<VisualElement>("map-2d-container");
            container3D = root.Q<VisualElement>("map-3d-container");

            if (btn2D != null) btn2D.clicked += () => SetMode("2d");
            if (btn3D != null) btn3D.clicked += () => SetMode("3d");

            ControlRoomEvents.OnMapViewModeChanged += SyncUI;
            SyncUI(ControlRoomState.Instance.MapViewMode);
        }

        void SetMode(string mode)
        {
            ControlRoomState.Instance.SetMapViewMode(mode);
            ControlRoomEvents.RaiseLogAdded("map", "INFO",
                mode == "3d"
                    ? "3D 맵 — Phase 6에서 URDF Importer로 채워질 예정"
                    : "2D 맵 모드");
        }

        void SyncUI(string mode)
        {
            bool is3D = mode == "3d";
            btn2D?.EnableInClassList("active", !is3D);
            btn3D?.EnableInClassList("active",  is3D);
            container2D?.EnableInClassList("hidden",  is3D);
            container3D?.EnableInClassList("hidden", !is3D);
        }
    }
}
