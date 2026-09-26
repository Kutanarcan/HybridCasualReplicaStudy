namespace ReplicaProjects.Arrows
{
    public struct GridCellInput
    {
        public bool[] gridData; // row-major, length = width * height
        public int width;
        public int x;
        public int y;
    }
}
