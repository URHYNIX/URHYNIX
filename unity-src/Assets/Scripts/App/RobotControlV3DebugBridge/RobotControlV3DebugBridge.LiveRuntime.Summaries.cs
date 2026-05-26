// Folder: App - read-only runtime, layout, and diagnostics summaries for RobotControlV3 debug calls.
using System.Collections.Generic;
using System.IO;
using System.Text;
using KineTutor3D.UI.RobotControlV3;
using KineTutor3D.App.Fairino;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace KineTutor3D.App
{
    // Handles read-only summaries, layout inspection, and context/aux panel debug reporting.
    // Mutating panel/runtime debug actions and QA matrices stay in adjacent LiveRuntime partials.
    public static partial class RobotControlV3DebugBridge
    {
        public static string SetConnectionPreviewStateForDebug(string stateName)
        {
            var home = Object.FindFirstObjectByType<ConnectionHomeController>(FindObjectsInactive.Include);
            if (home == null)
            {
                throw new MissingReferenceException("ConnectionHomeController not found in RobotControlV3 scene.");
            }

            return home.SetPreviewStateForDebug(stateName);
        }


        public static string GetSafetyFaultFlowSummaryForDebug()
        {
            var home = Object.FindFirstObjectByType<ConnectionHomeController>(FindObjectsInactive.Include);
            var safety = Object.FindFirstObjectByType<SafetyDiagnosticsController>(FindObjectsInactive.Include);
            var popup = Object.FindFirstObjectByType<PopupCoordinatorV3>(FindObjectsInactive.Include);
            return $"home=[{home?.GetDebugSummary() ?? "missing"}] | safety=[{safety?.GetDebugSummary() ?? "missing"}] | popup=[{popup?.GetDebugSummary() ?? "missing"}]";
        }


        public static string GetPanelControllerSummary()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != "Assets/Scenes/RobotControlV3.unity")
            {
                throw new System.InvalidOperationException($"RobotControlV3 scene must be active. Current: {scene.path}");
            }

            var home = Object.FindFirstObjectByType<ConnectionHomeController>(FindObjectsInactive.Include);
            var easy = Object.FindFirstObjectByType<EasyMotionController>(FindObjectsInactive.Include);
            var jointJog = Object.FindFirstObjectByType<JointJogController>(FindObjectsInactive.Include);
            var tcpJog = Object.FindFirstObjectByType<TcpJogController>(FindObjectsInactive.Include);
            var pointMove = Object.FindFirstObjectByType<PointMoveController>(FindObjectsInactive.Include);
            var ioPanel = Object.FindFirstObjectByType<IoPanelController>(FindObjectsInactive.Include);
            var status = Object.FindFirstObjectByType<StatusCardController>(FindObjectsInactive.Include);
            var safety = Object.FindFirstObjectByType<SafetyDiagnosticsController>(FindObjectsInactive.Include);
            var runtime = Object.FindFirstObjectByType<RobotControlV3RuntimeController>(FindObjectsInactive.Include);
            var renderSurface = Object.FindFirstObjectByType<RobotStageRenderSurface>(FindObjectsInactive.Include);
            var shell = Object.FindFirstObjectByType<PendantV3ShellStateController>(FindObjectsInactive.Include);
            var binder = Object.FindFirstObjectByType<PendantV3Binder>(FindObjectsInactive.Include);
            var coordinator = Object.FindFirstObjectByType<PendantV3SceneCoordinator>(FindObjectsInactive.Include);
            var shellCount = Object.FindObjectsByType<PendantV3ShellStateController>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;
            var easyCount = Object.FindObjectsByType<EasyMotionController>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;
            var jointCount = Object.FindObjectsByType<JointJogController>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;
            var tcpCount = Object.FindObjectsByType<TcpJogController>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;
            var pointCount = Object.FindObjectsByType<PointMoveController>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;
            coordinator?.ForceBootstrap();
            ioPanel ??= Object.FindFirstObjectByType<IoPanelController>(FindObjectsInactive.Include);
            var ioCount = Object.FindObjectsByType<IoPanelController>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;
            binder?.ForceInitialize();
            home?.ForceInitialize();
            easy?.ForceInitialize();
            jointJog?.ForceInitialize();
            tcpJog?.ForceInitialize();
            pointMove?.ForceInitialize();
            ioPanel?.ForceInitialize();
            status?.ForceInitialize();
            safety?.ForceInitialize();
            var homeSummary = home != null ? home.GetDebugSummary() : "ConnectionHomeController missing";
            var easySummary = easy != null ? easy.GetDebugSummary() : "EasyMotionController missing";
            var jointJogSummary = jointJog != null ? jointJog.GetDebugSummary() : "JointJogController missing";
            var tcpJogSummary = tcpJog != null ? tcpJog.GetDebugSummary() : "TcpJogController missing";
            var pointMoveSummary = pointMove != null ? pointMove.GetDebugSummary() : "PointMoveController missing";
            var ioSummary = ioPanel != null ? ioPanel.GetDebugSummary() : "IoPanelController missing";
            var statusSummary = status != null ? $"instanceId={status.GetInstanceID()}" : "StatusCardController missing";
            var safetySummary = safety != null ? $"instanceId={safety.GetInstanceID()}" : "SafetyDiagnosticsController missing";
            var runtimeSummary = runtime != null ? runtime.GetDebugSummary() : "RobotControlV3RuntimeController missing";
            var renderSummary = renderSurface != null ? renderSurface.GetDebugSummary() : "RobotStageRenderSurface missing";
            var shellSummary = shell != null ? shell.GetDebugSummary() : "PendantV3ShellStateController missing";
            var binderSummary = binder != null ? binder.GetDebugSummary() : "PendantV3Binder missing";
            var coordinatorSummary = coordinator != null ? coordinator.GetDebugSummary() : "PendantV3SceneCoordinator missing";
            return $"counts=[shell={shellCount}; easy={easyCount}; joint={jointCount}; tcp={tcpCount}; point={pointCount}; io={ioCount}] | coordinator=[{coordinatorSummary}] | runtime=[{runtimeSummary}] | render=[{renderSummary}] | binder=[{binderSummary}] | shell=[{shellSummary}] | home=[{homeSummary}] | status=[{statusSummary}] | safety=[{safetySummary}] | easy=[{easySummary}] | joint=[{jointJogSummary}] | tcp=[{tcpJogSummary}] | point=[{pointMoveSummary}] | io=[{ioSummary}]";
        }


        public static string GetV3RuntimeSummary()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != "Assets/Scenes/RobotControlV3.unity")
            {
                throw new System.InvalidOperationException($"RobotControlV3 scene must be active. Current: {scene.path}");
            }

            var runtime = Object.FindFirstObjectByType<RobotControlV3RuntimeController>(FindObjectsInactive.Include);
            return runtime == null
                ? "RobotControlV3RuntimeController missing"
                : $"instanceId={runtime.GetInstanceID()}; {runtime.GetDebugSummary()}";
        }


        public static string GetHeaderNextActionForDebug()
        {
            var document = Object.FindFirstObjectByType<UIDocument>(FindObjectsInactive.Include);
            var label = document?.rootVisualElement?.Q<Label>("HeaderNextActionLabel");
            return label != null ? label.text : "HeaderNextActionLabel missing";
        }


        public static string GetSafetyDiagnosticsTextsForDebug()
        {
            var document = Object.FindFirstObjectByType<UIDocument>(FindObjectsInactive.Include);
            var root = document?.rootVisualElement;
            if (root == null)
            {
                return "safetyDiagnostics=root-missing";
            }

            var now = root.Q<Label>("RecoveryNow")?.text ?? "missing";
            var primary = root.Q<Label>("RecoveryPrimary")?.text ?? "missing";
            var why = root.Q<Label>("RecoveryWhy")?.text ?? "missing";
            return $"now={now}; primary={primary}; why={why}";
        }


        public static string GetMovementStateSummaryForDebug()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != "Assets/Scenes/RobotControlV3.unity")
            {
                throw new System.InvalidOperationException($"RobotControlV3 scene must be active. Current: {scene.path}");
            }

            var runtime = GetRuntimeController();
            var snapshot = runtime.CurrentSnapshot;
            return $"status={snapshot.StatusKind}; dryRun={snapshot.DryRunEnabled}; pending={snapshot.PendingCommandSummary}; feedback={snapshot.LastFeedback}; joints=[{string.Join(",", snapshot.JointValues)}]; tcp=[{string.Join(",", snapshot.TcpValues)}]; ghost={snapshot.HasGhostPreview}; path={snapshot.HasPredictedPath}; gripper={snapshot.GripperSummary}; gripperVisual={snapshot.GripperVisualAttached}; robotDo={snapshot.RobotDoSummary}; toolDo={snapshot.ToolDoSummary}; peripheral={snapshot.PeripheralFeedback}; gripperSdk={snapshot.GripperSdkSummary}; selected={snapshot.SelectedPartName}; liveBlocked={snapshot.LiveBlockedReason}";
        }


        public static string GetDocumentDebugSummary()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != "Assets/Scenes/RobotControlV3.unity")
            {
                throw new System.InvalidOperationException($"RobotControlV3 scene must be active. Current: {scene.path}");
            }

            var document = Object.FindFirstObjectByType<UIDocument>(FindObjectsInactive.Include);
            var bridge = Object.FindFirstObjectByType<PendantV3Document>(FindObjectsInactive.Include);
            if (document == null)
            {
                return "UIDocument missing";
            }

            var root = document.rootVisualElement;
            var childCount = root != null ? root.childCount : -1;
            var panelName = document.panelSettings != null ? document.panelSettings.name : "null";
            var treeName = document.visualTreeAsset != null ? document.visualTreeAsset.name : "null";
            var bridgeName = bridge != null ? bridge.GetType().Name : "null";
            var robotName = root.Q<Label>("RobotNameLabel")?.text ?? "missing";
            var easyHome = root.Q<Button>("BtnEasyHome") != null;
            var homePanel = root.Q<VisualElement>("HomePanelHost") != null;
            var easyPanel = root.Q<VisualElement>("EasyMotionPanelHost") != null;
            var homeSheet = root.Q<VisualElement>("HomeSheetHost");
            var easySheet = root.Q<VisualElement>("EasyMotionSheetHost");
            var homeHost = root.Q<VisualElement>("HomePanelHost");
            var easyHost = root.Q<VisualElement>("EasyMotionPanelHost");
            var workPanelBody = root.Q<VisualElement>("WorkPanelBody");
            var bottomSheetBody = root.Q<VisualElement>("BottomSheetBody");
            var whyCard = root.Q<VisualElement>("WhyItMoved");
            var workPanelHeader = root.Q<VisualElement>("WorkPanelHeader");
            var workPanelTitle = root.Q<Label>("WorkPanelTitle");
            var workPanelSummary = root.Q<Label>("WorkPanelSummary");
            var workPanelChipPrimary = root.Q<Label>("WorkPanelChipPrimary");
            var workPanelChipSecondary = root.Q<Label>("WorkPanelChipSecondary");
            var robotStageHost = root.Q<VisualElement>("RobotStageHost");
            var robotStageSurface = root.Q<VisualElement>("RobotStageRenderSurface");
            var robotStageDiagnostic = root.Q<Label>("RobotStageDiagnosticLabel");
            var viewportHost = root.Q<VisualElement>("ViewportHost");
            var viewportToolbarHost = root.Q<VisualElement>("ViewportToolbarHost");
            var cartesianOverlayHost = root.Q<VisualElement>("CartesianArrowsOverlayHost");
            var contextPanel = root.Q<VisualElement>("ContextPanel") != null;
            var descendantCount = CountDescendants(root);
            var homeHostHidden = homeHost?.ClassListContains("rc-hidden") ?? false;
            var easyHostHidden = easyHost?.ClassListContains("rc-hidden") ?? false;
            var easySheetHidden = easySheet?.ClassListContains("rc-hidden") ?? false;
            var workPanelBodyHidden = workPanelBody?.ClassListContains("rc-hidden") ?? false;
            var workPanelSummaryHidden = workPanelSummary?.ClassListContains("rc-hidden") ?? false;
            var bottomSheetBodyHidden = bottomSheetBody?.ClassListContains("rc-hidden") ?? false;
            var whyCardHidden = whyCard?.ClassListContains("rc-hidden") ?? false;
            var workPanelHeaderBounds = workPanelHeader != null ? $"{workPanelHeader.worldBound.x:0.#},{workPanelHeader.worldBound.y:0.#},{workPanelHeader.worldBound.width:0.#}x{workPanelHeader.worldBound.height:0.#}" : "missing";
            var robotStageHostDisplay = robotStageHost != null ? robotStageHost.resolvedStyle.display.ToString() : "missing";
            var robotStageSurfaceDisplay = robotStageSurface != null ? robotStageSurface.resolvedStyle.display.ToString() : "missing";
            var robotStageDiagnosticDisplay = robotStageDiagnostic != null ? robotStageDiagnostic.resolvedStyle.display.ToString() : "missing";
            var robotStageDiagnosticText = robotStageDiagnostic?.text ?? "missing";
            var viewportHostDisplay = viewportHost != null ? viewportHost.resolvedStyle.display.ToString() : "missing";
            var viewportToolbarDisplay = viewportToolbarHost != null ? viewportToolbarHost.resolvedStyle.display.ToString() : "missing";
            var viewportToolbarParent = viewportToolbarHost?.hierarchy.parent?.name ?? "missing";
            var cartesianOverlayParent = cartesianOverlayHost?.hierarchy.parent?.name ?? "missing";
            var robotStageHostBounds = robotStageHost != null ? $"{robotStageHost.worldBound.x:0.#},{robotStageHost.worldBound.y:0.#},{robotStageHost.worldBound.width:0.#}x{robotStageHost.worldBound.height:0.#}" : "missing";
            var viewportHostBounds = viewportHost != null ? $"{viewportHost.worldBound.x:0.#},{viewportHost.worldBound.y:0.#},{viewportHost.worldBound.width:0.#}x{viewportHost.worldBound.height:0.#}" : "missing";
            var viewportToolbarBounds = viewportToolbarHost != null ? $"{viewportToolbarHost.worldBound.x:0.#},{viewportToolbarHost.worldBound.y:0.#},{viewportToolbarHost.worldBound.width:0.#}x{viewportToolbarHost.worldBound.height:0.#}" : "missing";
            return $"panel={panelName}; tree={treeName}; rootChildren={childCount}; rootName={(root?.name ?? "null")}; bridge={bridgeName}; robotName={robotName}; easyHome={easyHome}; homeHost={homePanel}; easyHost={easyPanel}; context={contextPanel}; homeHostChildren={homeHost?.childCount ?? -1}; easyHostChildren={easyHost?.childCount ?? -1}; homeHostHidden={homeHostHidden}; easyHostHidden={easyHostHidden}; workPanelTitle={(workPanelTitle?.text ?? "missing")}; workPanelChipPrimary={(workPanelChipPrimary?.text ?? "missing")}; workPanelChipSecondary={(workPanelChipSecondary?.text ?? "missing")}; workPanelSummaryHidden={workPanelSummaryHidden}; workPanelHeaderBounds={workPanelHeaderBounds}; workPanelBodyHidden={workPanelBodyHidden}; homeSheetChildren={homeSheet?.childCount ?? -1}; easySheetChildren={easySheet?.childCount ?? -1}; easySheetHidden={easySheetHidden}; bottomSheetBodyHidden={bottomSheetBodyHidden}; robotStageHostDisplay={robotStageHostDisplay}; robotStageHostBounds={robotStageHostBounds}; robotStageSurfaceDisplay={robotStageSurfaceDisplay}; robotStageDiagnosticDisplay={robotStageDiagnosticDisplay}; robotStageDiagnostic={robotStageDiagnosticText}; viewportHostDisplay={viewportHostDisplay}; viewportHostBounds={viewportHostBounds}; viewportToolbarDisplay={viewportToolbarDisplay}; viewportToolbarParent={viewportToolbarParent}; viewportToolbarBounds={viewportToolbarBounds}; cartesianOverlayParent={cartesianOverlayParent}; whyCardHidden={whyCardHidden}; descendants={descendantCount}";
        }


        public static string SetPreferV3Route(bool value)
        {
            RobotControlScenePreference.SetPreferV3(value);
            return RobotControlScenePreference.GetDebugSummary();
        }


        public static string ScrollContextPanelToTopForDebug()
        {
            var scrollView = GetContextPanelScrollView();
            scrollView.scrollOffset = Vector2.zero;
            return GetContextPanelScrollSummary();
        }


        public static string ScrollContextPanelToBottomForDebug()
        {
            var scrollView = GetContextPanelScrollView();
            scrollView.scrollOffset = new Vector2(0f, 100000f);
            return GetContextPanelScrollSummary();
        }


        public static string GetContextPanelScrollSummary()
        {
            var scrollView = GetContextPanelScrollView();
            var viewportHeight = scrollView.contentViewport.layout.height;
            var contentHeight = scrollView.contentContainer.layout.height;
            return $"offsetY={scrollView.scrollOffset.y:F1}; viewportHeight={viewportHeight:F1}; contentHeight={contentHeight:F1}";
        }


        public static string RunContextPanelTabMatrixForDebug()
        {
            var contract = GetInputContract();
            var root = contract.GetComponent<UIDocument>()?.rootVisualElement;
            if (root == null)
            {
                throw new MissingReferenceException("RobotControlV3 UIDocument root not found.");
            }

            var controller = Object.FindFirstObjectByType<ContextPanelTabController>(FindObjectsInactive.Include);
            if (controller == null || !controller.ForceInitialize())
            {
                throw new MissingReferenceException("ContextPanelTabController not initialized.");
            }

            ClickUiButton("BtnContextTabStatus", "desktop", out _, out _, out _);
            var statusSummary = BuildContextTabStateSummary(root);
            var statusPass = IsContextTabState(root, expectStatus: true);

            ClickUiButton("BtnContextTabCoordinate", "desktop", out _, out _, out _);
            var coordinateSummary = BuildContextTabStateSummary(root);
            var coordinatePass = IsContextTabState(root, expectStatus: false);

            var pass = statusPass && coordinatePass;
            return $"{(pass ? "PASS" : "FAIL")}; status=({statusSummary}); coordinate=({coordinateSummary})";
        }


        private static string BuildContextTabStateSummary(VisualElement root)
        {
            return $"statusTab={HasClass<Button>(root, "BtnContextTabStatus", "rc-context-tab--active")}; "
                + $"coordTab={HasClass<Button>(root, "BtnContextTabCoordinate", "rc-context-tab--active")}; "
                + $"toolbarHidden={IsHidden(root, "ViewportToolbarHost")}; "
                + $"coordHidden={IsHidden(root, "CoordStripHost")}; "
                + $"statusHidden={IsHidden(root, "StatusCardHost")}; "
                + $"safetyHidden={IsHidden(root, "SafetyDiagnosticsHost")}; "
                + $"actionHidden={IsHidden(root, "ActionHint")}; "
                + $"whyHidden={IsHidden(root, "WhyItMoved")}";
        }


        private static bool IsContextTabState(VisualElement root, bool expectStatus)
        {
            var statusActive = HasClass<Button>(root, "BtnContextTabStatus", "rc-context-tab--active");
            var coordinateActive = HasClass<Button>(root, "BtnContextTabCoordinate", "rc-context-tab--active");
            var toolbarHidden = IsHidden(root, "ViewportToolbarHost");
            var coordHidden = IsHidden(root, "CoordStripHost");
            var statusHidden = IsHidden(root, "StatusCardHost");
            var safetyHidden = IsHidden(root, "SafetyDiagnosticsHost");
            var actionHidden = IsHidden(root, "ActionHint");
            var whyHidden = IsHidden(root, "WhyItMoved");

            return expectStatus
                ? statusActive && !coordinateActive && toolbarHidden && coordHidden && !statusHidden && !safetyHidden && !actionHidden && !whyHidden
                : !statusActive && coordinateActive && !toolbarHidden && !coordHidden && statusHidden && safetyHidden && actionHidden && whyHidden;
        }


        private static bool HasClass<TElement>(VisualElement root, string elementName, string className)
            where TElement : VisualElement
        {
            return root.Q<TElement>(elementName)?.ClassListContains(className) ?? false;
        }


        private static bool IsHidden(VisualElement root, string elementName)
        {
            return root.Q<VisualElement>(elementName)?.ClassListContains("rc-hidden") ?? true;
        }


        public static string GetAuxLayoutSummaryForDebug()
        {
            var contract = GetInputContract();
            var root = contract.GetComponent<UIDocument>()?.rootVisualElement;
            if (root == null)
            {
                throw new MissingReferenceException("RobotControlV3 UIDocument root not found.");
            }

            var viewportHost = root.Q<VisualElement>("ViewportHost");
            var viewportScroll = root.Q<ScrollView>("ViewportPanelScroll");
            var contextScroll = root.Q<ScrollView>("ContextPanelScroll");
            var viewportSummary = GetScrollLayoutSummary("viewport", viewportHost, viewportScroll);
            var contextSummary = GetScrollLayoutSummary("context", root.Q<VisualElement>("ContextPanel"), contextScroll);
            return $"{viewportSummary}; {contextSummary}";
        }


        public static string GetAuxPanelOrderSummaryForDebug()
        {
            var contract = GetInputContract();
            var root = contract.GetComponent<UIDocument>()?.rootVisualElement;
            if (root == null)
            {
                throw new MissingReferenceException("RobotControlV3 UIDocument root not found.");
            }

            var viewportScroll = root.Q<ScrollView>("ViewportPanelScroll");
            var content = viewportScroll?.contentContainer;
            var controlDock = root.Q<VisualElement>("ControlDockHost");
            return $"viewportOrder=[{BuildDirectChildOrder(content)}]; controlDockOrder=[{BuildDirectChildOrder(controlDock)}]; {GetAuxLayoutSummaryForDebug()}";
        }
    }
}
