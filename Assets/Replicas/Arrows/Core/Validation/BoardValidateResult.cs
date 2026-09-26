using System.Collections.Generic;

namespace ReplicaProjects.Arrows
{
    public struct BoardValidateResult
    {
        public bool isValid;
        public List<BoardViolation> violations;
    }
}
