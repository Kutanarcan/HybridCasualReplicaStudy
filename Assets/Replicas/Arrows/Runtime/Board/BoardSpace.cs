using UnityEngine;

namespace ReplicaProjects.Arrows
{
    /// <summary>Board-to-world mapping: cell (x, y) sits at world (x, y, 0); arrows point along their direction.</summary>
    public static class BoardSpace
    {
        public static Vector3 ToWorld(GridCoord coordinates) => new(coordinates.x, coordinates.y, 0f);

        public static Vector3 ToWorldStep(Direction direction)
        {
            var offset = direction.ToOffset();
            return new Vector3(offset.x, offset.y, 0f);
        }

        public static Quaternion ToRotation(Direction direction)
        {
            switch (direction)
            {
                case Direction.Left: return Quaternion.Euler(0, 0, 90);
                case Direction.Right: return Quaternion.Euler(0, 0, -90);
                case Direction.Down: return Quaternion.Euler(0, 0, 180);
                default: return Quaternion.identity;
            }
        }
    }
}
