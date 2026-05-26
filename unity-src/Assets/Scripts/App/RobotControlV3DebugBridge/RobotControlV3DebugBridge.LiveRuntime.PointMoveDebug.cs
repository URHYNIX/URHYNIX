// Folder: App - point move panel debug entry points for CRUD, timing, and selected-run actions.
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
    // Handles PointMoveController-facing debug actions for point CRUD, timing, and panel state changes.
    // Teaching/function/block debug entry points stay in the TeachingDebug partial.
    public static partial class RobotControlV3DebugBridge
    {
        public static string SavePointMoveForDebug()
        {
            var pointMove = GetPointMoveController();
            return pointMove.SavePointForDebug();
        }


        public static string RecallPointMoveForDebug(string pointName)
        {
            var pointMove = GetPointMoveController();
            return pointMove.RecallPointForDebug(pointName);
        }


        public static string DeletePointMoveForDebug(string pointName)
        {
            var pointMove = GetPointMoveController();
            return pointMove.DeletePointForDebug(pointName);
        }


        public static string GetPointMoveListSummaryForDebug()
        {
            var pointMove = GetPointMoveController();
            return pointMove.GetPointListSummaryForDebug();
        }


        public static string RenamePointMoveForDebug(string oldName, string newName)
        {
            var pointMove = GetPointMoveController();
            return pointMove.RenamePointForDebug(oldName, newName);
        }


        public static string MovePointMoveForDebug(string pointName, int direction)
        {
            var pointMove = GetPointMoveController();
            return pointMove.MovePointForDebug(pointName, direction);
        }


        public static string OverwritePointMoveWithReadbackForDebug(string pointName)
        {
            var pointMove = GetPointMoveController();
            return pointMove.OverwritePointWithReadbackForDebug(pointName);
        }


        public static string DuplicatePointMoveForDebug(string pointName)
        {
            var pointMove = GetPointMoveController();
            return pointMove.DuplicatePointForDebug(pointName);
        }


        public static string GetPointMoveDetailForDebug()
        {
            var pointMove = GetPointMoveController();
            pointMove.ForceInitialize();
            return pointMove.GetSelectedPointDetailForDebug();
        }


        public static string GetPointActionModalSummaryForDebug()
        {
            var pointMove = GetPointMoveController();
            pointMove.ForceInitialize();
            return pointMove.GetPointActionModalSummaryForDebug();
        }


        public static string SetPointMoveTimingForDebug(string speedPreset, double dwellSec)
        {
            var pointMove = GetPointMoveController();
            return pointMove.SetPointTimingForDebug(speedPreset, dwellSec);
        }


        public static string ApplyPointMoveTimingForDebug()
        {
            var pointMove = GetPointMoveController();
            return pointMove.ApplyPointTimingForDebug();
        }


        public static string SetPointMoveEditLockedForDebug(bool locked)
        {
            var pointMove = GetPointMoveController();
            return pointMove.SetSequenceEditLockedForDebug(locked);
        }


        public static string TogglePointMoveLoopForDebug()
        {
            var pointMove = GetPointMoveController();
            return pointMove.ToggleLoopForDebug();
        }


        public static string ClearSelectedPointRowsForDebug()
        {
            var pointMove = GetPointMoveController();
            return pointMove.ClearSelectedPointsForDebug();
        }


        public static string RunPointMoveFromSelectedForDebug(string pointName)
        {
            var pointMove = GetPointMoveController();
            return pointMove.RunFromSelectedForDebug(pointName);
        }


        public static string ScrollPointMovePanelForDebug(float verticalOffset)
        {
            var document = Object.FindFirstObjectByType<UIDocument>(FindObjectsInactive.Include);
            var scrollView = document?.rootVisualElement?.Q<ScrollView>("ViewportPanelScroll");
            if (scrollView == null)
            {
                return "ViewportPanelScroll missing";
            }

            scrollView.scrollOffset = new Vector2(0f, Mathf.Max(0f, verticalOffset));
            return $"scrollOffset={scrollView.scrollOffset.y:0.0}; {GetAuxLayoutSummaryForDebug()}";
        }


        public static string ExportPointMoveForDebug()
        {
            var pointMove = GetPointMoveController();
            return pointMove.ExportPointsForDebug();
        }


        public static string CleanupPointMoveForDebug()
        {
            var pointMove = GetPointMoveController();
            return pointMove.CleanupPointsForDebug();
        }


        public static string SetPointMoveNameForDebug(string pointName)
        {
            var pointMove = GetPointMoveController();
            return pointMove.SetPointNameForDebug(pointName);
        }


        public static string SetPointMoveValueForDebug(string axisLabel, float value)
        {
            var pointMove = GetPointMoveController();
            return pointMove.SetPointValueForDebug(axisLabel, value);
        }
    }
}
