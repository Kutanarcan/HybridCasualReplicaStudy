using System;

namespace ReplicaProjects.Arrows
{
    [Serializable]
    public struct LineCell
    {
        public GridCoord coordinates;
        public Direction direction; // flow from the previous cell into this one (head -> tail)
    }
}
