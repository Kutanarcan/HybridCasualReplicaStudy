using System;
using UnityEditor;
using UnityEngine;

namespace ReplicaProjects.Arrows.EditorTools
{
    /// <summary>Brush selection: direction stamps, line painting and erase, via buttons or W/A/S/D/L/E.</summary>
    public sealed class LevelBrushBar
    {
        public event Action<BrushMode> ModeChanged;

        public BrushMode Mode { get; private set; } = BrushMode.Direction;
        public Direction Brush { get; private set; } = Direction.Up;

        public void Draw()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Brush:", GUILayout.Width(45f));
            DrawBrushButton(Direction.Up);
            DrawBrushButton(Direction.Down);
            DrawBrushButton(Direction.Left);
            DrawBrushButton(Direction.Right);

            GUILayout.Space(8f);
            DrawModeToggle(BrushMode.Line, "Line");
            GUILayout.Space(4f);
            DrawModeToggle(BrushMode.Erase, "Erase");
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField(
                "Heads: left-click stamps, right-click erases.  Line: click a head to select it, then click adjacent cells to extend its line; right-click trims.",
                EditorStyles.miniLabel);
            EditorGUILayout.LabelField(
                "Keys: W/A/S/D direction, L line, E erase, R randomize, C clear.",
                EditorStyles.miniLabel);
        }

        /// <summary>Handles brush shortcut keys. Returns true if the key was consumed.</summary>
        public bool HandleKey(KeyCode key)
        {
            switch (key)
            {
                case KeyCode.W: SetBrush(Direction.Up); return true;
                case KeyCode.S: SetBrush(Direction.Down); return true;
                case KeyCode.A: SetBrush(Direction.Left); return true;
                case KeyCode.D: SetBrush(Direction.Right); return true;
                case KeyCode.L: SetMode(BrushMode.Line); return true;
                case KeyCode.E: SetMode(Mode == BrushMode.Erase ? BrushMode.Direction : BrushMode.Erase); return true;
                default: return false;
            }
        }

        private void DrawModeToggle(BrushMode mode, string label)
        {
            bool active = Mode == mode;
            Color prev = GUI.backgroundColor;
            GUI.backgroundColor = active ? LevelEditorStyle.EraseColor : prev;
            bool on = GUILayout.Toggle(active, label, "Button", GUILayout.Width(60f));
            GUI.backgroundColor = prev;

            if (on && !active)
                SetMode(mode);
        }

        private void DrawBrushButton(Direction direction)
        {
            bool active = Mode == BrushMode.Direction && Brush == direction;

            Color prev = GUI.backgroundColor;
            Color c = LevelEditorStyle.DirectionColor(direction);
            // Full color when selected, dimmed toward the default otherwise.
            GUI.backgroundColor = active ? c : Color.Lerp(prev, c, 0.45f);

            bool clicked = GUILayout.Toggle(active, LevelEditorStyle.Glyph(direction), "Button", GUILayout.Width(34f));
            GUI.backgroundColor = prev;

            if (clicked && !active)
                SetBrush(direction);
        }

        private void SetBrush(Direction direction)
        {
            Brush = direction;
            SetMode(BrushMode.Direction);
        }

        private void SetMode(BrushMode mode)
        {
            Mode = mode;
            ModeChanged?.Invoke(mode);
        }
    }
}
