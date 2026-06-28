using UnityEngine;

namespace ReplicaProjects.Arrows
{
    public class GridLogic
    {
        public int CoordinatesToIndex(int x, int y, int width) => y * width + x;

        public Vector2Int IndexToCoordinates(int index, int width) => new Vector2Int(index % width, index / width);

        public bool IsInBounds(int index, int length) => index >= 0 && index < length;

        public bool IsEmpty(GridCellInput input)
        {
            var index = CoordinatesToIndex(input.x, input.y, input.width);

            return IsInBounds(index, input.gridData.Length) && !input.gridData[index];
        }

        public int CellsAhead(Vector2Int from, Direction direction, int width, int height)
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

        public bool IsPathClear(bool[] gridData, Vector2Int from, Direction direction, int width, int height)
        {
            int steps = CellsAhead(from, direction, width, height);
            var step = direction.ToVector2Int();
            var c = from;
            for (int i = 0; i < steps; i++)
            {
                c += step;
                if (gridData[CoordinatesToIndex(c.x, c.y, width)])
                    return false;
            }
            return true;
        }
    }
}
