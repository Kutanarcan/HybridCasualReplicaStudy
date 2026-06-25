using UnityEngine;

namespace ReplicaProjects.Arrows
{
    public static class GridMath
    {
        public static int CoordinatesToIndex(int x, int y, int width) => y * width + x;

        public static Vector2Int IndexToCoordinates(int index, int width) => new Vector2Int(index % width, index / width);

        public static bool IsInBounds(int index, int length) => index >= 0 && index < length;
    }
}
