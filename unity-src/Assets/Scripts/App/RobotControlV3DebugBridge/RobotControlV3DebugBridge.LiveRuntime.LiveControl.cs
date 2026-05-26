// Folder: App - runtime live control, gripper, IO, approval, and evidence debug entry points.
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
    // Handles runtime live/session debug controls and gripper/IO product-like debug entry points.
    // PointMove, Teaching, Joint/TCP, summaries, and matrix harnesses stay in adjacent LiveRuntime partials.
    public static partial class RobotControlV3DebugBridge
    {
        public static string SetGripperOpenForDebug(bool open)
        {
            var result = GetRuntimeController().SetGripperOpen(open);
            return $"{result.Message}; {GetMovementStateSummaryForDebug()}";
        }


        public static string SetGripperPositionForDebug(int positionPercent)
        {
            var result = GetRuntimeController().SetGripperPositionPercent(positionPercent);
            return $"{result.Message}; {GetMovementStateSummaryForDebug()}";
        }


        public static string SetEasyMotionGripperInputForDebug(float positionPercent, bool apply = false)
        {
            var easy = Object.FindFirstObjectByType<EasyMotionController>(FindObjectsInactive.Include);
            if (easy == null)
            {
                return "EasyMotionController missing";
            }

            return easy.SetGripperInputForDebug(positionPercent, apply);
        }


        public static string GetEasyMotionGripperInputStateForDebug()
        {
            var easy = Object.FindFirstObjectByType<EasyMotionController>(FindObjectsInactive.Include);
            if (easy == null)
            {
                return "EasyMotionController missing";
            }

            return easy.GetGripperInputStateForDebug();
        }


        public static string ConnectDefaultForDebug()
        {
            var result = GetRuntimeController().ConnectDefault();
            return $"{result.Message}; {GetMovementStateSummaryForDebug()}";
        }


        public static string ConnectDefaultPollingSuppressedForDebug()
        {
            var runtime = GetRuntimeController();
            var result = runtime.ConnectDefault();
            runtime.SetLivePollingSuppressedForDebug(true);
            return $"{result.Message}; pollSuppressed=True; {runtime.GetLiveReadbackProbeSummaryForDebug()}; {GetMovementStateSummaryForDebug()}";
        }


        public static string SetLivePollingSuppressedForDebug(bool suppressed)
        {
            var runtime = GetRuntimeController();
            runtime.SetLivePollingSuppressedForDebug(suppressed);
            return $"pollSuppressed={suppressed}; {runtime.GetLiveReadbackProbeSummaryForDebug()}";
        }


        public static string SetMockModeForDebug(bool mock)
        {
            return $"{GetRuntimeController().SetMockModeForDebug(mock)}; {GetMovementStateSummaryForDebug()}";
        }


        public static string SyncCurrentStateForDebug()
        {
            var result = GetRuntimeController().SyncCurrentState();
            return $"{result.Message}; {GetMovementStateSummaryForDebug()}";
        }


        public static string EnableServoForDebug()
        {
            var result = GetRuntimeController().EnableServo();
            return $"{result.Message}; {GetMovementStateSummaryForDebug()}";
        }

        public static string RequestAutoModeForDebug()
        {
            var result = GetRuntimeController().RequestAutoMode();
            return $"{result.Message}; {GetMovementStateSummaryForDebug()}";
        }


        public static string RequestManualModeForDebug()
        {
            var result = GetRuntimeController().RequestManualMode();
            return $"{result.Message}; {GetMovementStateSummaryForDebug()}";
        }


        public static string ToggleDryRunForDebug()
        {
            GetRuntimeController().ToggleDryRun();
            return GetV3RuntimeSummary();
        }


        public static string ExecutePrimaryActionForDebug()
        {
            var runtime = GetRuntimeController();
            runtime.ExecutePrimaryAction();
            return GetMovementStateSummaryForDebug();
        }


        public static string ExecutePreparedPreviewForDebug()
        {
            return GetRuntimeController().ExecutePreparedPreviewForDebug();
        }


        public static string SetLiveSessionModeForDebug(string sessionMode)
        {
            return GetRuntimeController().SetLiveSessionModeForDebug(sessionMode);
        }


        public static string GetLiveSessionModeSummaryForDebug()
        {
            return GetRuntimeController().GetLiveSessionModeSummaryForDebug();
        }


        public static string GetLiveGripperOperatorGateSummaryForDebug()
        {
            return GetRuntimeController().GetLiveGripperOperatorGateSummaryForDebug();
        }


        public static string GrantLiveSessionApprovalForDebug(string commandKind, int ttlSeconds = 15)
        {
            return GetRuntimeController().GrantLiveCommandApprovalForDebug(commandKind, ttlSeconds);
        }


        public static string GetLiveSessionApprovalStateForDebug()
        {
            return $"sessionApproved={GetRuntimeController().HasActiveLiveSessionApprovalForProduct()}; {GetRuntimeController().GetLiveCommandApprovalSummaryForDebug()}";
        }


        public static string GetLiveEvidenceGateSummaryForDebug()
        {
            return GetRuntimeController().GetLiveEvidenceGateSummaryForDebug();
        }


        public static string GetTinyMoveJGateSummaryForDebug()
        {
            return GetRuntimeController().GetTinyMoveJGateSummaryForDebug();
        }


        public static string RefreshLiveEvidenceForDebug()
        {
            return GetRuntimeController().RefreshLiveEvidenceForDebug();
        }


        public static string RecordCachedLiveEvidenceForDebug()
        {
            return GetRuntimeController().RecordCachedLiveEvidenceForDebug();
        }


        public static string DisconnectForDebug()
        {
            var result = GetRuntimeController().Disconnect();
            return $"{result.Message}; {GetMovementStateSummaryForDebug()}";
        }


        public static string GetGripperVisualSummaryForDebug()
        {
            var runtime = GetRuntimeController();
            return runtime.GetGripperVisualSummaryForDebug();
        }


        public static string RecaptureGripperAuthoredOpenForDebug()
        {
            return GetRuntimeController().RecaptureGripperAuthoredOpenForDebug();
        }


        public static string RecaptureGripperAuthoredClosedForDebug()
        {
            return GetRuntimeController().RecaptureGripperAuthoredClosedForDebug();
        }


        public static string ClearGripperAuthoredClosedForDebug()
        {
            return GetRuntimeController().ClearGripperAuthoredClosedForDebug();
        }


        public static string GetGripperSdkSummaryForDebug(bool includeReadback = true)
        {
            var runtime = GetRuntimeController();
            return runtime.GetGripperSdkSummaryForDebug(includeReadback);
        }


        public static string ProbeLiveGripperForDebug()
        {
            var runtime = GetRuntimeController();
            return runtime.ProbeLiveGripperForDebug();
        }


        public static string ProbeLiveGripperActivationSequencesForDebug()
        {
            var runtime = GetRuntimeController();
            return runtime.ProbeLiveGripperActivationSequencesForDebug();
        }


        public static string ProbeLiveGripperIndexCandidatesForDebug(int minIndex = 1, int maxIndex = 4)
        {
            var runtime = GetRuntimeController();
            return runtime.ProbeLiveGripperIndexCandidatesForDebug(minIndex, maxIndex);
        }


        public static string SetRobotDoForDebug(int channel, bool value)
        {
            var result = GetRuntimeController().SetRobotDigitalOutput(channel, value);
            return $"{result.Message}; {GetMovementStateSummaryForDebug()}";
        }


        public static string SetToolDoForDebug(int channel, bool value)
        {
            var result = GetRuntimeController().SetToolDigitalOutput(channel, value);
            return $"{result.Message}; {GetMovementStateSummaryForDebug()}";
        }
    }
}
