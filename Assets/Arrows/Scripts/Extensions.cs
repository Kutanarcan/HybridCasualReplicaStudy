using UnityEngine;

namespace ReplicaProjects.Arrows
{
    public static class Extensions
    {
        public static Direction ToDirection(this Vector2Int vector)
        {
            if (vector == Vector2Int.left)
            {
                return Direction.Left;
            }
            if (vector == Vector2Int.right)
            {
                return Direction.Right;
            }
            if (vector == Vector2Int.up)
            {
                return Direction.Up;
            }
            if (vector == Vector2Int.down)
            {
                return Direction.Down;
            }

            return Direction.None;
        }

        public static Quaternion ToQuaternion(this Direction dir)
        {
            switch (dir)
            {
                case Direction.Left:
                    return Quaternion.Euler(0, 0, 90);
                case Direction.Right:
                    return Quaternion.Euler(0, 0, -90);
                case Direction.Up:
                    return Quaternion.Euler(0, 0, 0);
                case Direction.Down:
                    return Quaternion.Euler(0, 0, 180);
            }

            return Quaternion.identity;
        }
    }
}
