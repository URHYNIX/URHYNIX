// Folder: App - stage and viewport debug entry points for RobotControlV3.
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
    // Handles stage capture, viewport selection, and render-surface debug helpers.
    // Generic summaries and QA matrices stay in adjacent LiveRuntime partials.
    public static partial class RobotControlV3DebugBridge
    {
        public static string CaptureStageCameraForDebug(string outputPath)
        {
            return GetRuntimeController().CaptureStageCameraForDebug(outputPath);
        }


        public static string GetStagePoseSignatureForDebug()
        {
            return BuildStagePoseSignature(GetRuntimeController());
        }


        public static string GetRobotStageRenderSummary()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != "Assets/Scenes/RobotControlV3.unity")
            {
                throw new System.InvalidOperationException($"RobotControlV3 scene must be active. Current: {scene.path}");
            }

            var surface = Object.FindFirstObjectByType<RobotStageRenderSurface>(FindObjectsInactive.Include);
            surface?.ForceInitialize();
            return surface == null
                ? "RobotStageRenderSurface missing"
                : $"instanceId={surface.GetInstanceID()}; {surface.GetDebugSummary()}";
        }


        public static string SelectRobotPartAtViewportForDebug(float normalizedX, float normalizedY)
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != "Assets/Scenes/RobotControlV3.unity")
            {
                throw new System.InvalidOperationException($"RobotControlV3 scene must be active. Current: {scene.path}");
            }

            var runtime = Object.FindFirstObjectByType<RobotControlV3RuntimeController>(FindObjectsInactive.Include);
            if (runtime == null)
            {
                throw new MissingReferenceException("RobotControlV3RuntimeController not found in RobotControlV3 scene.");
            }

            var selected = runtime.SelectRobotPartAtViewport(new Vector2(normalizedX, normalizedY));
            return $"selected={selected}; {runtime.GetDebugSummary()}";
        }


        public static string SelectRobotPartCenterForDebug()
        {
            return SelectRobotPartAtViewportForDebug(0.5f, 0.5f);
        }


        public static string ToggleViewportDescriptionForDebug()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != "Assets/Scenes/RobotControlV3.unity")
            {
                throw new System.InvalidOperationException($"RobotControlV3 scene must be active. Current: {scene.path}");
            }

            var controller = Object.FindFirstObjectByType<ViewportAuxInfoController>(FindObjectsInactive.Include);
            if (controller == null)
            {
                throw new MissingReferenceException("ViewportAuxInfoController not found in RobotControlV3 scene.");
            }

            controller.ForceInitialize();
            return controller.ToggleDescriptionForDebug();
        }


        public static string ToggleViewportSelectionForDebug()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != "Assets/Scenes/RobotControlV3.unity")
            {
                throw new System.InvalidOperationException($"RobotControlV3 scene must be active. Current: {scene.path}");
            }

            var controller = Object.FindFirstObjectByType<ViewportAuxInfoController>(FindObjectsInactive.Include);
            if (controller == null)
            {
                throw new MissingReferenceException("ViewportAuxInfoController not found in RobotControlV3 scene.");
            }

            controller.ForceInitialize();
            return controller.ToggleSelectionForDebug();
        }


        public static string PreviewEasyMotionForDebug(string presetName)
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != "Assets/Scenes/RobotControlV3.unity")
            {
                throw new System.InvalidOperationException($"RobotControlV3 scene must be active. Current: {scene.path}");
            }

            var runtime = Object.FindFirstObjectByType<RobotControlV3RuntimeController>(FindObjectsInactive.Include);
            if (runtime == null)
            {
                throw new MissingReferenceException("RobotControlV3RuntimeController not found in RobotControlV3 scene.");
            }

            runtime.PreviewPreset(presetName);
            return runtime.GetDebugSummary();
        }
    }
}
