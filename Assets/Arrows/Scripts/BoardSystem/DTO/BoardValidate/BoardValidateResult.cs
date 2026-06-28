using System.Collections.Generic;
using UnityEngine;

namespace ReplicaProjects.Arrows
{
    public struct BoardViolation
    {
        public Vector2Int coordinates;
        public string reason;
    }

    public struct BoardValidateResult
    {
        public bool isValid;
        public List<BoardViolation> violations;
    }
}
