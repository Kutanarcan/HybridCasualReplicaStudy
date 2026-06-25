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
    }
}
