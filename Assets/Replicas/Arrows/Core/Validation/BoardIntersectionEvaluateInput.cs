namespace ReplicaProjects.Arrows
{
    public struct BoardIntersectionEvaluateInput
    {
        public GridCoord coordinateA;   // head being evaluated
        public GridCoord coordinateB;   // already-placed head
        public Direction directionB;    // direction of B
    }
}
