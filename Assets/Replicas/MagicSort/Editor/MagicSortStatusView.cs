using ReplicaProjects.Common.EditorTools;
using UnityEditor;
using UnityEngine;

namespace ReplicaProjects.MagicSort.EditorTools
{
    /// <summary>Solver result banner and difficulty metric bars.</summary>
    public static class MagicSortStatusView
    {
        private static readonly Color DepthColor = new(0.98f, 0.45f, 0.09f);
        private static readonly Color FragmentationColor = new(0.66f, 0.33f, 0.97f);
        private static readonly Color MeterBackground = new(0.15f, 0.15f, 0.2f);

        public static void DrawSolveResult(MagicSortLevelEditorLogic logic, SolveResult? solveResult)
        {
            if (logic.IsPuzzleSolved() && logic.MoveCount > 0)
                EditorDraw.Banner($"Solved! ({logic.MoveCount} Move)", MagicSortEditorPalette.Success);

            if (!solveResult.HasValue)
                return;

            var r = solveResult.Value;
            switch (r.Status)
            {
                case SolveStatus.Solved:
                    EditorDraw.Banner("Already Solved!", MagicSortEditorPalette.Success);
                    break;
                case SolveStatus.Solvable:
                    string extra = r.Attempts > 0 ? $"  ({r.Attempts} attempt)" : "";
                    EditorDraw.Banner($"Solvable — min {r.MinMoves} move{extra}", MagicSortEditorPalette.Success);
                    break;
                case SolveStatus.Unsolvable:
                    EditorDraw.Banner("Unsolvable", MagicSortEditorPalette.Failure);
                    break;
                case SolveStatus.Timeout:
                    EditorDraw.Banner($"Ambiguous — {r.StatesExplored / 1000}K case, reached limit", MagicSortEditorPalette.Warning);
                    break;
            }
        }

        public static void DrawMetrics(MagicSortLevelEditorLogic logic)
        {
            var m = LevelMetrics.Calculate(logic.Snapshot());
            DrawMeter("Depth", m.Depth, DepthColor);
            DrawMeter("Fragmentation", m.Fragmentation, FragmentationColor);
        }

        private static void DrawMeter(string label, float value, Color color)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, GUILayout.Width(100));

            var rect = EditorGUILayout.GetControlRect(false, 12, GUILayout.Width(140));
            EditorGUI.DrawRect(rect, MeterBackground);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width * value, rect.height), color);

            EditorGUILayout.LabelField($"{Mathf.RoundToInt(value * 100)}%", GUILayout.Width(40));
            EditorGUILayout.EndHorizontal();
        }
    }
}
