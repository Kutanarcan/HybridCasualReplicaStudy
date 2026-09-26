using System.Collections.Generic;

namespace ReplicaProjects.Arrows
{
    public struct BoardCornerEvaluateResult
    {
        public bool IsAtCorner;
        public List<Direction> bannedDirections;
    }
}
