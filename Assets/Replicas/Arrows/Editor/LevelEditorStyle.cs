using UnityEngine;

namespace ReplicaProjects.Arrows.EditorTools
{
    /// <summary>Colors and glyphs of the Arrows level editor.</summary>
    public static class LevelEditorStyle
    {
        public static readonly Color EraseColor = new(0.85f, 0.30f, 0.30f);
        public static readonly Color SaveColor = new(0.30f, 0.72f, 0.38f);
        public static readonly Color RandomizeColor = new(0.30f, 0.55f, 0.90f);
        public static readonly Color ClearColor = new(0.80f, 0.45f, 0.25f);
        public static readonly Color CellColor = new(0.22f, 0.22f, 0.22f);
        public static readonly Color BorderColor = new(0.45f, 0.45f, 0.45f);
        public static readonly Color ViolationColor = new(0.60f, 0.20f, 0.20f);
        public static readonly Color SelectedColor = new(0.95f, 0.95f, 0.40f);

        private const int PoolSize = 50;

        // 50 distinct colors, no red (reserved for violations).
        // Golden-ratio hue step: each index lands as far as possible from all previous ones.
        private static readonly Color[] HeadColorPool = BuildColorPool();

        private static Color[] BuildColorPool()
        {
            var pool = new Color[PoolSize];
            float h = 0f;
            for (int i = 0; i < PoolSize; i++)
            {
                h = (h + 0.618034f) % 1f; // golden-ratio conjugate
                // Remap [0,1] → [0.08, 0.93] to keep red (~0° and ~360°) out of range.
                float safeHue = 0.08f + h * 0.85f;
                float sat = i % 2 == 0 ? 0.80f : 0.65f;
                float val = i % 3 == 0 ? 0.95f : i % 3 == 1 ? 0.80f : 1.00f;
                pool[i] = Color.HSVToRGB(safeHue, sat, val);
            }
            return pool;
        }

        public static Color HeadColor(int headIndex) => HeadColorPool[headIndex % HeadColorPool.Length];

        public static Color DirectionColor(Direction direction)
        {
            switch (direction)
            {
                case Direction.Up: return new Color(0.35f, 0.78f, 0.40f);    // green
                case Direction.Down: return new Color(0.95f, 0.55f, 0.20f);  // orange
                case Direction.Left: return new Color(0.30f, 0.55f, 0.95f);  // blue
                case Direction.Right: return new Color(0.92f, 0.80f, 0.20f); // yellow
                default: return Color.gray;
            }
        }

        public static string Glyph(Direction direction)
        {
            switch (direction)
            {
                case Direction.Up: return "▲";
                case Direction.Down: return "▼";
                case Direction.Left: return "◀";
                case Direction.Right: return "▶";
                default: return string.Empty;
            }
        }

        public static string LineGlyph(Direction direction) =>
            direction == Direction.None ? string.Empty : Glyph(direction) + "L";
    }
}
