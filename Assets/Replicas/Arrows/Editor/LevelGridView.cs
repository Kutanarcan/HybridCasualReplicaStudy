using System;
using System.Collections.Generic;
using ReplicaProjects.Common.EditorTools;
using UnityEditor;
using UnityEngine;

namespace ReplicaProjects.Arrows.EditorTools
{
    /// <summary>Draws the working board as a cell grid ((0,0) bottom-left) and reports cell clicks.</summary>
    public sealed class LevelGridView
    {
        private const float CellSize = 40f;

        /// <summary>Cell and mouse button (0 = left, 1 = right).</summary>
        public event Action<GridCoord, int> CellClicked;

        private GUIStyle _glyphStyle;
        private GUIStyle _lineGlyphStyle;
        private GUIStyle _numberStyle;

        public void Draw(LevelEditBuffer buffer, HashSet<GridCoord> violations, GridCoord? selectedHead)
        {
            EnsureStyles();

            var area = GUILayoutUtility.GetRect(buffer.Width * CellSize, buffer.Height * CellSize,
                GUILayout.ExpandWidth(false), GUILayout.ExpandHeight(false));

            for (int y = 0; y < buffer.Height; y++)
            for (int x = 0; x < buffer.Width; x++)
            {
                var coord = new GridCoord(x, y);
                // Invert y so (0,0) is bottom-left.
                var cell = new Rect(area.x + x * CellSize, area.y + (buffer.Height - 1 - y) * CellSize, CellSize, CellSize);

                DrawCell(buffer, coord, cell, violations.Contains(coord), selectedHead == coord);
                HandleMouse(cell, coord);
            }
        }

        private void DrawCell(LevelEditBuffer buffer, GridCoord coord, Rect cell, bool violation, bool selected)
        {
            // Border = full cell painted in the border color, then an inset fill.
            EditorGUI.DrawRect(cell, LevelEditorStyle.BorderColor);
            var inner = new Rect(cell.x + 1f, cell.y + 1f, cell.width - 2f, cell.height - 2f);

            if (buffer.TryGetHeadIndex(coord, out int headIndex))
                DrawHead(buffer.Heads[headIndex], headIndex, inner, violation, selected);
            else if (buffer.TryGetLineOwner(coord, out int owner, out int cellIndex))
                DrawLineCell(buffer.Heads[owner].line[cellIndex], owner, inner, violation);
            else
                EditorGUI.DrawRect(inner, violation ? LevelEditorStyle.ViolationColor : LevelEditorStyle.CellColor);
        }

        private void DrawHead(HeadData head, int headIndex, Rect inner, bool violation, bool selected)
        {
            var color = LevelEditorStyle.HeadColor(headIndex);
            EditorGUI.DrawRect(inner, violation ? LevelEditorStyle.ViolationColor : Color.Lerp(LevelEditorStyle.CellColor, color, 0.55f));

            if (head.direction != Direction.None)
            {
                _glyphStyle.normal.textColor = violation ? Color.white : color;
                GUI.Label(inner, LevelEditorStyle.Glyph(head.direction), _glyphStyle);
            }

            // Index links a head to its line cells.
            DrawIndex(inner, headIndex);

            if (selected)
                EditorDraw.Outline(inner, LevelEditorStyle.SelectedColor, 2f);
        }

        private void DrawLineCell(LineCell lineCell, int owner, Rect inner, bool violation)
        {
            var color = LevelEditorStyle.HeadColor(owner);
            EditorGUI.DrawRect(inner, violation ? LevelEditorStyle.ViolationColor : Color.Lerp(LevelEditorStyle.CellColor, color, 0.35f));

            // Direction glyph shows flow direction so the designer can see the tail.
            if (lineCell.direction != Direction.None)
            {
                _lineGlyphStyle.normal.textColor = new Color(color.r, color.g, color.b, 0.60f);
                GUI.Label(inner, LevelEditorStyle.LineGlyph(lineCell.direction), _lineGlyphStyle);
            }

            DrawIndex(inner, owner);
        }

        private void DrawIndex(Rect inner, int index) =>
            GUI.Label(new Rect(inner.x + 4f, inner.y + 3f, inner.width, 20f), index.ToString(), _numberStyle);

        private void HandleMouse(Rect rect, GridCoord coord)
        {
            var e = Event.current;
            if (e.type != EventType.MouseDown || !rect.Contains(e.mousePosition) || e.button > 1)
                return;

            CellClicked?.Invoke(coord, e.button);
            e.Use();
        }

        private void EnsureStyles()
        {
            if (_glyphStyle != null)
                return;

            _glyphStyle = new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleCenter, fontSize = 18 };
            _lineGlyphStyle = new GUIStyle(_glyphStyle) { fontSize = 16 };
            _numberStyle = new GUIStyle(EditorStyles.miniBoldLabel) { alignment = TextAnchor.UpperLeft, fontSize = 9 };
            _numberStyle.normal.textColor = Color.white;
        }
    }
}
