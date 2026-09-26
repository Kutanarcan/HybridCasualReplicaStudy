using System;

namespace ReplicaProjects.Arrows
{
    /// <summary>
    /// Integer board cell. Field names mirror Vector2Int's (x, y) so level assets serialized with
    /// Vector2Int deserialize into this type unchanged.
    /// </summary>
    [Serializable]
    public struct GridCoord : IEquatable<GridCoord>
    {
        public int x;
        public int y;

        public GridCoord(int x, int y)
        {
            this.x = x;
            this.y = y;
        }

        public static GridCoord Zero => new(0, 0);
        public static GridCoord Up => new(0, 1);
        public static GridCoord Down => new(0, -1);
        public static GridCoord Left => new(-1, 0);
        public static GridCoord Right => new(1, 0);

        public int SqrMagnitude => x * x + y * y;

        public static GridCoord operator +(GridCoord a, GridCoord b) => new(a.x + b.x, a.y + b.y);
        public static GridCoord operator -(GridCoord a, GridCoord b) => new(a.x - b.x, a.y - b.y);
        public static GridCoord operator -(GridCoord a) => new(-a.x, -a.y);
        public static bool operator ==(GridCoord a, GridCoord b) => a.x == b.x && a.y == b.y;
        public static bool operator !=(GridCoord a, GridCoord b) => !(a == b);

        public bool Equals(GridCoord other) => this == other;
        public override bool Equals(object obj) => obj is GridCoord other && this == other;
        public override int GetHashCode() => unchecked(x * 397) ^ y;
        public override string ToString() => $"({x}, {y})";
    }
}
