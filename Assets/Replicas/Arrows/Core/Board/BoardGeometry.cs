using System.Collections.Generic;

namespace ReplicaProjects.Arrows
{
    /// <summary>Bounds, flat indexing and line-of-sight rays shared by generation and validation.</summary>
    public static class BoardGeometry
    {
        public static bool InBounds(GridCoord coord, int width, int height) =>
            coord.x >= 0 && coord.x < width && coord.y >= 0 && coord.y < height;

        public static int ToIndex(GridCoord coord, int width) => coord.y * width + coord.x;

        // Every cell from the head, stepping in its direction, to the board edge (excludes the head).
        public static void CollectLineOfSight(GridCoord from, Direction direction,
                                              int width, int height, HashSet<int> into)
        {
            var step = direction.ToOffset();
            if (step == GridCoord.Zero)
                return;

            var c = from + step;
            while (InBounds(c, width, height))
            {
                into.Add(ToIndex(c, width));
                c += step;
            }
        }
    }
}
