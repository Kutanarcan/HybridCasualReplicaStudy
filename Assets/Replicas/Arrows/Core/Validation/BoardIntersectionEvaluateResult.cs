using System.Collections.Generic;

namespace ReplicaProjects.Arrows
{
    public struct BoardIntersectionEvaluateResult
    {
        public bool HasIntersection;
        public List<Direction> bannedDirections;
    }
}
