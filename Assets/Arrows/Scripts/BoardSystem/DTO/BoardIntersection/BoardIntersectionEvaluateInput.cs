using UnityEngine;

namespace ReplicaProjects.Arrows
{
    public struct BoardIntersectionEvaluateInput
    {
        public Vector2Int coordinateA;   // head being evaluated
        public Vector2Int coordinateB;   // already-placed head
        public Direction directionB;     // direction of B
    }
}
