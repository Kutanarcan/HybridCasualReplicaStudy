using System;
using ReplicaProjects.Common.EditorTools;
using UnityEditor;
using UnityEngine;

namespace ReplicaProjects.MagicSort.EditorTools
{
    /// <summary>
    /// Draws the working board as columns of slots and handles designer moves: click a bar to pick
    /// its top ball, click another to drop it there (free-move rules).
    /// </summary>
    public sealed class MagicSortBoardView
    {
        public event Action Moved;
        public event Action RepaintRequested;

        private const int SlotW = 44;
        private const int SlotH = 38;
        private const int SlotGap = 3;
        private const int BarPadding = 4;
        private const int BarSpacing = 16;
        private const int MaxBarsPerRow = 6;

        private static readonly Color BarBackground = new(0.12f, 0.14f, 0.18f);
        private static readonly Color SelectedBackground = new(0.12f, 0.23f, 0.38f);
        private static readonly Color Border = new(0.2f, 0.22f, 0.27f);
        private static readonly Color SelectedBorder = new(0.38f, 0.65f, 0.98f);
        private static readonly Color EmptySlotBorder = new(0.2f, 0.22f, 0.27f, 0.4f);
        private static readonly Color NameColor = new(0.4f, 0.42f, 0.47f);

        public int SelectedBar { get; private set; } = -1;

        public void ClearSelection() => SelectedBar = -1;

        public void Draw(MagicSortLevelEditorLogic logic, int colorCount, int barHeight)
        {
            int total = logic.GetBarCount();
            int barsPerRow = Mathf.Min(total, MaxBarsPerRow);
            if (barsPerRow == 0)
                return;

            int rows = Mathf.CeilToInt((float)total / barsPerRow);
            int barW = SlotW + BarPadding * 2;
            int barTotalH = barHeight * (SlotH + SlotGap) + BarPadding;

            for (int row = 0; row < rows; row++)
            {
                int rowStart = row * barsPerRow;
                int rowCount = Mathf.Min(barsPerRow, total - rowStart);
                float totalW = rowCount * barW + (rowCount - 1) * BarSpacing;

                var rowRect = EditorGUILayout.GetControlRect(false, barTotalH + 22);
                float startX = rowRect.x + (rowRect.width - totalW) * 0.5f;

                for (int col = 0; col < rowCount; col++)
                {
                    var barRect = new Rect(startX + col * (barW + BarSpacing), rowRect.y, barW, barTotalH);
                    DrawBar(logic, rowStart + col, barRect, colorCount, barHeight);
                }
            }
        }

        private void DrawBar(MagicSortLevelEditorLogic logic, int barIndex, Rect barRect, int colorCount, int barHeight)
        {
            bool isSelected = SelectedBar == barIndex;

            EditorGUI.DrawRect(barRect, isSelected ? SelectedBackground : BarBackground);
            EditorDraw.Outline(barRect, isSelected ? SelectedBorder : Border);

            // Slots, bottom to top
            for (int slot = 0; slot < barHeight; slot++)
            {
                float slotY = barRect.yMax - BarPadding - (slot + 1) * (SlotH + SlotGap) + SlotGap;
                var slotRect = new Rect(barRect.x + BarPadding, slotY, SlotW, SlotH);

                if (slot < logic.GetBarSize(barIndex))
                    DrawBall(logic.GetColor(barIndex, slot), slotRect, slot == logic.GetBarSize(barIndex) - 1, isSelected);
                else
                    EditorDraw.Outline(slotRect, EmptySlotBorder);
            }

            HandleClick(logic, barIndex, barRect);
            DrawName(barIndex, barRect, colorCount, isSelected);
        }

        private static void DrawBall(int colorIndex, Rect slotRect, bool isTop, bool barSelected)
        {
            Color c = MagicSortEditorPalette.BallColor(colorIndex);
            if (!isTop || barSelected) c *= 0.8f;
            c.a = 1f;
            EditorGUI.DrawRect(slotRect, c);

            var labelStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 11,
                normal = { textColor = colorIndex == MagicSortEditorPalette.WhiteIndex ? new Color(0.1f, 0.1f, 0.15f) : Color.white }
            };
            GUI.Label(slotRect, MagicSortEditorPalette.BallLabel(colorIndex), labelStyle);

            // Glow on the lifted top ball
            if (isTop && barSelected)
                EditorDraw.Outline(slotRect, new Color(0.38f, 0.65f, 0.98f, 0.8f));
        }

        private static void DrawName(int barIndex, Rect barRect, int colorCount, bool isSelected)
        {
            var labelRect = new Rect(barRect.x, barRect.yMax + 2, barRect.width, 16);
            var nameStyle = new GUIStyle(EditorStyles.centeredGreyMiniLabel)
            {
                fontSize = 9,
                normal = { textColor = isSelected ? SelectedBorder : NameColor }
            };
            string name = barIndex >= colorCount
                ? $"Empty {barIndex - colorCount + 1}"
                : MagicSortEditorPalette.ColorNames[barIndex];
            GUI.Label(labelRect, name, nameStyle);
        }

        private void HandleClick(MagicSortLevelEditorLogic logic, int barIndex, Rect barRect)
        {
            var e = Event.current;
            if (e.type != EventType.MouseDown || !barRect.Contains(e.mousePosition))
                return;

            OnBarClicked(logic, barIndex);
            e.Use();
            RepaintRequested?.Invoke();
        }

        private void OnBarClicked(MagicSortLevelEditorLogic logic, int barIndex)
        {
            if (SelectedBar == -1)
            {
                if (!logic.IsBarEmpty(barIndex))
                    SelectedBar = barIndex;
                return;
            }

            bool moved = barIndex != SelectedBar && logic.TryMove(SelectedBar, barIndex);
            SelectedBar = -1;

            if (moved)
                Moved?.Invoke();
        }
    }
}
