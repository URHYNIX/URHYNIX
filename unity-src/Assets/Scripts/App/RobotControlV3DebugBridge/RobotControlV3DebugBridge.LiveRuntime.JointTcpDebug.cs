// Folder: App - joint and TCP motion panel debug entry points.
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
    // Handles joint/tcp panel inputs, nudges, preview helpers, and shell selection debug hooks.
    // Live QA matrices and read-only summaries stay in adjacent LiveRuntime partials.
    public static partial class RobotControlV3DebugBridge
    {
        public static string SetJointJogShellState(string navSection, string workTab, string tabletTab)
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != "Assets/Scenes/RobotControlV3.unity")
            {
                throw new System.InvalidOperationException($"RobotControlV3 scene must be active. Current: {scene.path}");
            }

            var jointJog = Object.FindFirstObjectByType<JointJogController>(FindObjectsInactive.Include);
            if (jointJog == null)
            {
                throw new MissingReferenceException("JointJogController not found in RobotControlV3 scene.");
            }

            jointJog.SetShellState(navSection, workTab, tabletTab);
            return jointJog.GetDebugSummary();
        }


        public static string NudgeTcpAxisForDebug(string axisLabel, int direction)
        {
            var tcpJog = GetTcpJogController();
            return tcpJog.NudgeAxisForDebug(axisLabel, direction);
        }


        public static string SetTcpCoordSystemForDebug(string coordSystem)
        {
            var tcpJog = GetTcpJogController();
            return tcpJog.SetCoordSystemForDebug(coordSystem);
        }


        public static string GetJointRowSummary(int axisNumber)
        {
            var jointJog = GetJointJogController();
            return jointJog.GetJointRowDebugSummary(axisNumber);
        }


        public static string FocusJointInputForDebug(int axisNumber)
        {
            var jointJog = GetJointJogController();
            return jointJog.FocusJointInputForDebug(axisNumber);
        }


        public static string SetJointSliderForDebug(int axisNumber, float value)
        {
            var jointJog = GetJointJogController();
            return jointJog.SetJointSliderForDebug(axisNumber, value);
        }


        public static string SetJointInputForDebug(int axisNumber, string rawValue)
        {
            var jointJog = GetJointJogController();
            return jointJog.SetJointInputForDebug(axisNumber, rawValue);
        }


        public static string SetJointTargetsForDebug(double[] jointAnglesDeg)
        {
            var jointJog = GetJointJogController();
            return jointJog.SetJointTargetsForDebug(jointAnglesDeg);
        }


        public static string NudgeJointForDebug(int axisNumber, int direction)
        {
            var jointJog = GetJointJogController();
            return jointJog.NudgeJointForDebug(axisNumber, direction);
        }


        public static string PreviewJointJogForDebug()
        {
            return GetJointJogController().PreviewCurrentValuesForDebug();
        }


        public static string ApplyJointJogForDebug()
        {
            return GetJointJogController().ApplyCurrentValuesForDebug();
        }


        public static string RestoreJointJogForDebug()
        {
            return GetJointJogController().RestoreCurrentValuesForDebug();
        }


        public static string PreviewJointNudgeForDebug(int axisNumber, int direction)
        {
            var increment = PendantV3LocalState.Normalize(LocalSettingsStore.LoadOrDefault()).JogIncrement;
            return PreviewJointDeltaForDebug(axisNumber, (direction >= 0 ? 1f : -1f) * increment);
        }


        public static string PreviewJointDeltaForDebug(int axisNumber, float deltaDeg)
        {
            var runtime = GetRuntimeController();
            var axisIndex = Mathf.Clamp(axisNumber - 1, 0, 5);
            var baseline = runtime.CurrentRobotStateForDebug.JointPosDeg;
            if (baseline == null || baseline.Length < 6)
            {
                return "joint baseline missing";
            }

            var target = (double[])baseline.Clone();
            target[axisIndex] += deltaDeg;
            var setSummary = SetJointTargetsForDebug(target);
            var previewSummary = GetJointJogController().PreviewCurrentValuesForDebug();
            return $"axis={axisIndex + 1}; delta={deltaDeg:0.###}; targetJ{axisIndex + 1}={target[axisIndex]:0.###}; set={setSummary}; preview={previewSummary}; runtime={runtime.GetDebugSummary()}";
        }


        public static string PreviewJointDeltaNoVisualForDebug(int axisNumber, float deltaDeg)
        {
            var runtime = GetRuntimeController();
            var axisIndex = Mathf.Clamp(axisNumber - 1, 0, 5);
            var baseline = runtime.CurrentRobotStateForDebug.JointPosDeg;
            if (baseline == null || baseline.Length < 6)
            {
                return "joint baseline missing";
            }

            var target = (double[])baseline.Clone();
            target[axisIndex] += deltaDeg;
            runtime.PreviewJointAnglesForDebugNoVisual(target, $"관절 {axisIndex + 1} MoveJ 후보");
            return $"axis={axisIndex + 1}; delta={deltaDeg:0.###}; targetJ{axisIndex + 1}={target[axisIndex]:0.###}; preview=no-visual; runtime={runtime.GetDebugSummary()}";
        }


        public static string PrepareJointDeltaNoUiForDebug(int axisNumber, float deltaDeg)
        {
            var runtime = GetRuntimeControllerWithoutForceInitializeForDebug();
            var axisIndex = Mathf.Clamp(axisNumber - 1, 0, 5);
            var baseline = runtime.CurrentRobotStateForDebug.JointPosDeg;
            if (baseline == null || baseline.Length < 6)
            {
                return "joint baseline missing";
            }

            var target = (double[])baseline.Clone();
            target[axisIndex] += deltaDeg;
            var prepared = runtime.TryPrepareJointAnglesForDebugNoUi(target, $"관절 {axisIndex + 1} MoveJ 후보", out var message);
            return $"axis={axisIndex + 1}; delta={deltaDeg:0.###}; targetJ{axisIndex + 1}={target[axisIndex]:0.###}; noUi=True; {message}; prepared={prepared}";
        }


        public static string PrepareJointOneDegreeNoUiForDebug(int axisNumber, int direction)
        {
            return PrepareJointDeltaNoUiForDebug(axisNumber, direction >= 0 ? 1f : -1f);
        }


        public static string ProbeRuntimeNoInitForDebug()
        {
            var runtime = GetRuntimeControllerWithoutForceInitializeForDebug();
            var baseline = runtime.CurrentRobotStateForDebug.JointPosDeg;
            return baseline == null
                ? "runtimeFound=True; baseline=null"
                : $"runtimeFound=True; joints={baseline.Length}; j6={(baseline.Length >= 6 ? baseline[5].ToString("0.###") : "missing")}";
        }


        private static RobotControlV3RuntimeController GetRuntimeControllerWithoutForceInitializeForDebug()
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

            return runtime;
        }
    }
}
