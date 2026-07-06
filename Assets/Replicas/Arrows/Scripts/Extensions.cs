using System.Collections.Generic;
using UnityEngine;

namespace ReplicaProjects.Arrows
{
    public static class Extensions
    {
        public static void RemoveRange(this HashSet<Direction> hashSet, List<Direction> directions)
        {
            if (directions == null)
                return;

            foreach (var direction in directions)
            {
                hashSet.Remove(direction);
            }
        }


        public static void AddRange(this HashSet<Direction> hashSet, List<Direction> directions)
        {
            if (directions == null)
                return;

            foreach (var direction in directions)
            {
                hashSet.Add(direction);
            }
        }

        public static void AddRange(this HashSet<Direction> hashSet, Direction[] directions)
        {
            if (directions == null)
                return;

            foreach (var direction in directions)
            {
                hashSet.Add(direction);
            }
        }

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

        public static Vector2Int ToVector2Int(this Direction dir)
        {
            switch (dir)
            {
                case Direction.Up:
                    return Vector2Int.up;
                case Direction.Down:
                    return Vector2Int.down;
                case Direction.Left:
                    return Vector2Int.left;
                case Direction.Right:
                    return Vector2Int.right;
            }

            return Vector2Int.zero;
        }

        public static Direction Opposite(this Direction dir) => (-dir.ToVector2Int()).ToDirection();

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
