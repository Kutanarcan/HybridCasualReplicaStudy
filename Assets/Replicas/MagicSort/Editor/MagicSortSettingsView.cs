using System;
using UnityEditor;
using UnityEngine;

namespace ReplicaProjects.MagicSort.EditorTools
{
    /// <summary>Generation settings: color count and spare bars (these reshape the board) plus difficulty levels.</summary>
    public sealed class MagicSortSettingsView
    {
        /// <summary>Color count or spare-bar count changed; the board must be rebuilt.</summary>
        public event Action Reconfigured;

        private static readonly Color CountColor = new(0.3f, 0.5f, 0.9f);
        private static readonly Color DepthColor = new(0.9f, 0.5f, 0.1f);
        private static readonly Color FragmentationColor = new(0.6f, 0.3f, 0.9f);

        public int ColorCount { get; private set; } = 3;
        public int EmptyBars { get; private set; }
        public int DepthLevel { get; private set; } = 1;
        public int FragLevel { get; private set; } = 1;

        /// <summary>Restores the shape controls from a loaded level asset (no rebuild).</summary>
        public void LoadFrom(MagicSortLevel level)
        {
            ColorCount = Mathf.Max(1, level.colorCount);
            EmptyBars = Mathf.Max(0, level.barCount - level.colorCount);
        }

        public void Draw()
        {
            EditorGUILayout.LabelField("Settings", EditorStyles.boldLabel);

            // Any click on a shape button rebuilds the board, even on the active value (acts as a reset).
            bool reshaped = NumberRow("Color", 3, 10, ColorCount, CountColor, out int colors);
            reshaped |= NumberRow("Extra Empty Bar", 0, 3, EmptyBars, CountColor, out int empties);
            DepthLevel = LevelRow("Depth", DepthLevel, DepthColor);
            FragLevel = LevelRow("Fragmentation", FragLevel, FragmentationColor);

            if (!reshaped)
                return;

            ColorCount = colors;
            EmptyBars = empties;
            Reconfigured?.Invoke();
        }

        private static bool NumberRow(string label, int min, int max, int current, Color activeColor, out int picked)
        {
            bool clicked = false;
            picked = current;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, GUILayout.Width(100));
            for (int n = min; n <= max; n++)
            {
                GUI.backgroundColor = n == current ? activeColor : Color.white;
                if (GUILayout.Button(n.ToString(), GUILayout.Width(28), GUILayout.Height(22)))
                {
                    picked = n;
                    clicked = true;
                }
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();
            return clicked;
        }

        private static int LevelRow(string label, int current, Color activeColor)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, GUILayout.Width(100));
            for (int i = 0; i < MagicSortEditorPalette.LevelLabels.Length; i++)
            {
                GUI.backgroundColor = i == current ? activeColor : Color.white;
                if (GUILayout.Button(MagicSortEditorPalette.LevelLabels[i], GUILayout.Width(60), GUILayout.Height(22)))
                    current = i;
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();
            return current;
        }
    }
}
