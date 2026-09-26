namespace ReplicaProjects.Arrows
{
    public static class DirectionExtensions
    {
        public static GridCoord ToOffset(this Direction direction)
        {
            switch (direction)
            {
                case Direction.Up: return GridCoord.Up;
                case Direction.Down: return GridCoord.Down;
                case Direction.Left: return GridCoord.Left;
                case Direction.Right: return GridCoord.Right;
                default: return GridCoord.Zero;
            }
        }

        public static Direction ToDirection(this GridCoord offset)
        {
            if (offset == GridCoord.Up) return Direction.Up;
            if (offset == GridCoord.Down) return Direction.Down;
            if (offset == GridCoord.Left) return Direction.Left;
            if (offset == GridCoord.Right) return Direction.Right;
            return Direction.None;
        }

        public static Direction Opposite(this Direction direction) => (-direction.ToOffset()).ToDirection();
    }
}
