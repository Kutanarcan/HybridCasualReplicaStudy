namespace ReplicaProjects.Arrows
{
    public class GridLogic
    {
        public int CoordinatesToIndex(int x, int y, int width) => y * width + x;

        public GridCoord IndexToCoordinates(int index, int width) => new(index % width, index / width);

        public bool IsInBounds(int index, int length) => index >= 0 && index < length;

        // Checked per axis: a flat-index check alone lets x == width wrap onto the next row.
        public bool IsInBounds(int x, int y, int width, int height) =>
            x >= 0 && x < width && y >= 0 && y < height;

        // Out-of-bounds cells are reported as not empty.
        public bool IsEmpty(GridCellInput input)
        {
            int height = input.width > 0 ? input.gridData.Length / input.width : 0;
            if (!IsInBounds(input.x, input.y, input.width, height))
                return false;

            return !input.gridData[CoordinatesToIndex(input.x, input.y, input.width)];
        }

        public int CellsAhead(GridCoord from, Direction direction, int width, int height)
        {
            switch (direction)
            {
                case Direction.Right: return width - 1 - from.x;
                case Direction.Left:  return from.x;
                case Direction.Up:    return height - 1 - from.y;
                case Direction.Down:  return from.y;
                default:              return 0;
            }
        }

        public bool IsPathClear(bool[] gridData, GridCoord from, Direction direction, int width, int height) =>
            FirstBlocked(gridData, from, direction, width, height) == from;

        // First occupied cell from `from` in `direction`, or `from` itself if the ray is fully clear.
        public GridCoord FirstBlocked(bool[] gridData, GridCoord from, Direction direction, int width, int height)
        {
            int steps = CellsAhead(from, direction, width, height);
            var step = direction.ToOffset();
            var c = from;
            for (int i = 0; i < steps; i++)
            {
                c += step;
                if (gridData[CoordinatesToIndex(c.x, c.y, width)])
                    return c;
            }
            return from;
        }
    }
}
