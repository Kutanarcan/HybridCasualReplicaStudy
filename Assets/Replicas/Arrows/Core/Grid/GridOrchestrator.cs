namespace ReplicaProjects.Arrows
{
    /// <summary>Occupancy grid (true = occupied) with coordinate conversion and ray queries.</summary>
    public class GridOrchestrator
    {
        private readonly GridLogic _gridLogic = new();
        private bool[] _gridData;

        public int Width { get; private set; }
        public int Height { get; private set; }

        public void Initialize(int width, int height)
        {
            Width = width;
            Height = height;
            _gridData = new bool[width * height];
        }

        public void DeInitialize()
        {
            Width = 0;
            Height = 0;
            _gridData = null;
        }

        public void Set(GridCoord coordinates, bool value)
        {
            if (!IsInBounds(coordinates))
                return;

            _gridData[CoordinatesToIndex(coordinates)] = value;
        }

        public bool IsEmpty(GridCoord coordinates) => _gridLogic.IsEmpty(new GridCellInput
        {
            gridData = _gridData,
            width = Width,
            x = coordinates.x,
            y = coordinates.y
        });

        public bool IsInBounds(GridCoord coordinates) =>
            _gridLogic.IsInBounds(coordinates.x, coordinates.y, Width, Height);

        public GridCoord IndexToCoordinates(int index) => _gridLogic.IndexToCoordinates(index, Width);

        public int CoordinatesToIndex(GridCoord coordinates) =>
            _gridLogic.CoordinatesToIndex(coordinates.x, coordinates.y, Width);

        public bool IsPathClear(GridCoord from, Direction direction) =>
            _gridLogic.IsPathClear(_gridData, from, direction, Width, Height);

        public GridCoord FirstBlocked(GridCoord from, Direction direction) =>
            _gridLogic.FirstBlocked(_gridData, from, direction, Width, Height);
    }
}
