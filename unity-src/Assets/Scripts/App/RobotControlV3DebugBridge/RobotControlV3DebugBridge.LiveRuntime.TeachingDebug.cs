// Folder: App - teaching/function/block debug entry points for the V3 point move panel.
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
    // Handles teaching sequence, function, and block sequence debug actions mapped to the split runtime teaching surface.
    // PointMove CRUD and motion-panel debug actions stay in separate LiveRuntime partials.
    public static partial class RobotControlV3DebugBridge
    {
        public static string SetTeachingSubviewForDebug(string subviewName)
        {
            var pointMove = GetPointMoveController();
            return pointMove.SetTeachingSubviewForDebug(subviewName);
        }


        public static string SetTeachingLoopForDebug(bool enabled)
        {
            var runtime = GetRuntimeController();
            runtime.SetTeachingLoopEnabled(enabled);
            return runtime.GetTeachingLoopSummaryForDebug();
        }


        public static string GetTeachingLoopSummaryForDebug()
        {
            return GetRuntimeController().GetTeachingLoopSummaryForDebug();
        }


        public static string RunTeachingSequenceFromPointForDebug(string pointName)
        {
            return GetRuntimeController().ExecuteTeachingSequenceFromPointForDebug(pointName);
        }


        public static string CreateTeachingFunctionForDebug(string functionName)
        {
            var pointMove = GetPointMoveController();
            return pointMove.CreateFunctionForDebug(functionName);
        }


        public static string SelectTeachingFunctionForDebug(string functionName)
        {
            var pointMove = GetPointMoveController();
            return pointMove.SelectFunctionForDebug(functionName);
        }


        public static string SetTeachingFunctionNameForDebug(string functionName)
        {
            var pointMove = GetPointMoveController();
            return pointMove.SetFunctionNameForDebug(functionName);
        }


        public static string RenameTeachingFunctionForDebug(string functionName)
        {
            var pointMove = GetPointMoveController();
            return pointMove.RenameFunctionForDebug(functionName);
        }


        public static string DuplicateTeachingFunctionForDebug()
        {
            var pointMove = GetPointMoveController();
            return pointMove.DuplicateFunctionForDebug();
        }


        public static string DeleteTeachingFunctionForDebug()
        {
            var pointMove = GetPointMoveController();
            return pointMove.DeleteFunctionForDebug();
        }


        public static string ToggleTeachingFunctionSelectionForDebug(string functionName)
        {
            var pointMove = GetPointMoveController();
            return pointMove.ToggleFunctionSelectionForDebug(functionName);
        }


        public static string ToggleTeachingFunctionActionsForDebug()
        {
            var pointMove = GetPointMoveController();
            return pointMove.ToggleFunctionActionsForDebug();
        }


        public static string DuplicateSelectedTeachingFunctionsForDebug()
        {
            var pointMove = GetPointMoveController();
            return pointMove.DuplicateSelectedFunctionsForDebug();
        }


        public static string DeleteSelectedTeachingFunctionsForDebug()
        {
            var pointMove = GetPointMoveController();
            return pointMove.DeleteSelectedFunctionsForDebug();
        }


        public static string DeleteAllTeachingFunctionsForDebug()
        {
            var pointMove = GetPointMoveController();
            pointMove.ForceInitialize();
            return GetRuntimeController().DeleteAllTeachingFunctionsForDebug();
        }


        public static string RunTeachingFunctionForDebug()
        {
            var pointMove = GetPointMoveController();
            return pointMove.RunFunctionForDebug();
        }


        public static string AddSelectedPointToFunctionForDebug(string pointName)
        {
            var pointMove = GetPointMoveController();
            return pointMove.AddSelectedPointToFunctionForDebug(pointName);
        }


        public static string ClearFunctionPointSelectionForDebug()
        {
            var pointMove = GetPointMoveController();
            return pointMove.ClearFunctionPointSelectionForDebug();
        }


        public static string RunTeachingFunctionFromSelectedForDebug(string pointName)
        {
            var pointMove = GetPointMoveController();
            return pointMove.RunFunctionFromSelectedForDebug(pointName);
        }


        public static string GetTeachingFunctionUiSummaryForDebug()
        {
            var pointMove = GetPointMoveController();
            return pointMove.GetFunctionCompactDebugSummary();
        }


        public static string GetTeachingFunctionSourceSummaryForDebug()
        {
            var pointMove = GetPointMoveController();
            return pointMove.GetFunctionSourceDebugSummary();
        }


        public static string GetTeachingSequenceLibrarySummaryForDebug()
        {
            var pointMove = GetPointMoveController();
            pointMove.ForceInitialize();
            return pointMove.GetSequenceLibrarySummaryForDebug();
        }


        public static string SelectTeachingSequenceForDebug(string sequenceName)
        {
            var pointMove = GetPointMoveController();
            return pointMove.SelectSequenceForDebug(sequenceName);
        }


        public static string RunSelectedTeachingSequenceOnceForDebug()
        {
            var pointMove = GetPointMoveController();
            return pointMove.RunSelectedSequenceOnceForDebug();
        }


        public static string RunSelectedTeachingSequenceLoopForDebug()
        {
            var pointMove = GetPointMoveController();
            return pointMove.RunSelectedSequenceLoopForDebug();
        }


        public static string DeleteSelectedTeachingSequenceForDebug()
        {
            var pointMove = GetPointMoveController();
            return pointMove.DeleteSelectedSequenceForDebug();
        }


        public static string GetTeachingBlockSequenceSummaryForDebug()
        {
            return GetRuntimeController().GetTeachingBlockSequenceSummaryForDebug();
        }


        public static string AddTeachingBlockPointForDebug(string pointName)
        {
            return GetRuntimeController().AddTeachingBlockPoint(pointName);
        }


        public static string AddTeachingBlockBundleForDebug(string bundleName)
        {
            return GetRuntimeController().AddTeachingBlockBundle(bundleName);
        }


        public static string MoveTeachingBlockForDebug(int index, int direction)
        {
            return GetRuntimeController().MoveTeachingBlock(index, direction);
        }


        public static string DeleteTeachingBlockForDebug(int index)
        {
            return GetRuntimeController().DeleteTeachingBlock(index);
        }


        public static string ClearTeachingBlockSequenceForDebug()
        {
            return GetRuntimeController().ClearTeachingBlockSequenceForDebug();
        }


        public static string PreviewTeachingBlockSequenceForDebug()
        {
            return GetRuntimeController().PreviewTeachingBlockSequence();
        }


        public static string RunTeachingBlockSequenceForDebug()
        {
            return GetRuntimeController().ExecuteTeachingBlockSequenceDryRun();
        }


        public static string AddBuchimgaeCookingSequenceForDebug()
        {
            return $"preset=unavailable; {new TeachingBlockSequenceStore().BuildSummary()}";
        }


        public static string ToggleTeachingSequenceSelectionForDebug(string sequenceName)
        {
            var pointMove = GetPointMoveController();
            return pointMove.ToggleSequenceSelectionForDebug(sequenceName);
        }


        public static string ToggleTeachingSequenceActionsForDebug()
        {
            var pointMove = GetPointMoveController();
            return pointMove.ToggleSequenceActionsForDebug();
        }


        public static string DeleteSelectedTeachingSequencesForDebug()
        {
            var pointMove = GetPointMoveController();
            return pointMove.DeleteSelectedSequencesForDebug();
        }
    }
}
