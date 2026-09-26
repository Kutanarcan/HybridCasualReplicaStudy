using System;
using ReplicaProjects.Common;
using ReplicaProjects.Common.EditorTools;
using UnityEditor;
using UnityEngine;

namespace ReplicaProjects.Arrows.EditorTools
{
    /// <summary>Whole-board commands: resize, randomize and clear (buttons plus R / C shortcuts).</summary>
    public sealed class LevelBoardControls
    {
        public event Action Changed;

        private readonly LevelEditBuffer _buffer;
        private readonly LinePainter _painter;
        private readonly LevelGenerator _generator = new(new SeededRandomSource());
        private int _headCount = 5;

        public LevelBoardControls(LevelEditBuffer buffer, LinePainter painter)
        {
            _buffer = buffer;
            _painter = painter;
        }

        public void DrawDimensions()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();

            GUILayout.Label("Width", GUILayout.Width(45f));
            int w = Mathf.Max(1, EditorGUILayout.IntField(_buffer.Width, GUILayout.Width(50f)));
            GUILayout.Space(12f);
            GUILayout.Label("Height", GUILayout.Width(50f));
            int h = Mathf.Max(1, EditorGUILayout.IntField(_buffer.Height, GUILayout.Width(50f)));

            if (EditorGUI.EndChangeCheck())
                Resize(w, h);

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        public void DrawRandomizer()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Head Count", GUILayout.Width(75f));
            _headCount = Mathf.Max(0, EditorGUILayout.IntField(_headCount, GUILayout.Width(50f)));
            GUILayout.Space(8f);
            if (EditorDraw.ColorButton("Randomize", LevelEditorStyle.RandomizeColor, GUILayout.Width(100f)))
                Randomize();
            if (EditorDraw.ColorButton("Clear", LevelEditorStyle.ClearColor, GUILayout.Width(60f)))
                ClearHeads();
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>R randomizes and C clears, both behind a confirmation. Returns true if the key was consumed.</summary>
        public bool HandleKey(KeyCode key)
        {
            switch (key)
            {
                case KeyCode.R:
                    if (EditorUtility.DisplayDialog("Randomize Level",
                            "Replace the current layout with a new random one?", "Randomize", "Cancel"))
                        Randomize();
                    return true;

                case KeyCode.C:
                    if (EditorUtility.DisplayDialog("Clear Level",
                            "Are you sure you want to clear the level?", "Clear", "Cancel"))
                        ClearHeads();
                    return true;

                default:
                    return false;
            }
        }

        private void Resize(int width, int height)
        {
            // Dropping heads that fall outside the new bounds needs confirmation.
            if (_buffer.HasHeadsOutside(width, height) && !EditorUtility.DisplayDialog("Resize Board",
                    "Shrinking will remove heads outside the new bounds. Continue?", "Remove", "Cancel"))
                return;

            _buffer.Resize(width, height);
            _painter.DropSelectionOutsideBoard();
            Changed?.Invoke();
        }

        private void Randomize()
        {
            _buffer.ReplaceHeads(_generator.GenerateRandomLevel(_buffer.Width, _buffer.Height, _headCount));
            _painter.Deselect();
            Changed?.Invoke();
        }

        private void ClearHeads()
        {
            _buffer.Clear();
            _painter.Deselect();
            Changed?.Invoke();
        }
    }
}
