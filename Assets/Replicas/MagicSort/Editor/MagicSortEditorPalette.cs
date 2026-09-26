using UnityEngine;

namespace ReplicaProjects.MagicSort.EditorTools
{
    /// <summary>Ball colors, labels and difficulty names of the Magic Sort level editor.</summary>
    public static class MagicSortEditorPalette
    {
        public static readonly Color[] BarColors =
        {
            new(0.86f, 0.15f, 0.15f), // Red
            new(0.15f, 0.39f, 0.92f), // Blue
            new(0.92f, 0.70f, 0.03f), // Yellow
            new(0.09f, 0.64f, 0.26f), // Green
            new(0.98f, 0.45f, 0.09f), // Orange
            new(0.58f, 0.20f, 0.92f), // Purple
            new(0.02f, 0.71f, 0.83f), // Cyan
            new(0.93f, 0.27f, 0.60f), // Pink
            new(0.47f, 0.44f, 0.40f), // Brown
            new(0.94f, 0.96f, 0.98f), // White
        };

        public const int WhiteIndex = 9;

        public static readonly string[] ColorLabels = { "R", "B", "Y", "G", "O", "P", "C", "Pi", "Br", "W" };

        public static readonly string[] ColorNames =
            { "Red", "Blue", "Yellow", "Green", "Orange", "Purple", "Cyan", "Pink", "Brown", "White" };

        public static readonly string[] LevelLabels = { "Easy", "Medium", "Hard" };

        public static readonly Color Success = new(0.1f, 0.5f, 0.2f);
        public static readonly Color Failure = new(0.7f, 0.15f, 0.15f);
        public static readonly Color Warning = new(0.7f, 0.5f, 0.1f);

        public static Color BallColor(int colorIndex) =>
            colorIndex >= 0 && colorIndex < BarColors.Length ? BarColors[colorIndex] : Color.gray;

        public static string BallLabel(int colorIndex) =>
            colorIndex >= 0 && colorIndex < ColorLabels.Length ? ColorLabels[colorIndex] : "?";
    }
}
