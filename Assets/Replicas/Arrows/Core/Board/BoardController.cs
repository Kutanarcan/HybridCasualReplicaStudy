using System.Collections.Generic;

namespace ReplicaProjects.Arrows
{
    /// <summary>Board queries and removal in grid coordinates, keeping occupancy and ownership in sync.</summary>
    public class BoardController
    {
        private readonly GridOrchestrator _grid = new();
        private readonly BoardOrchestrator _board = new();

        public IReadOnlyList<HeadData> Heads { get; private set; }
        public int RemainingArrows => _board.RemainingCount;

        public void Initialize(int width, int height, IReadOnlyList<HeadData> heads)
        {
            Heads = heads;
            _grid.Initialize(width, height);
            _board.Initialize(width, height, heads);

            PopulateOccupancy();
        }

        public void DeInitialize()
        {
            _board.DeInitialize();
            _grid.DeInitialize();
            Heads = null;
        }

        private void PopulateOccupancy()
        {
            foreach (var headIndex in _board.dataArrays.headIndexArray)
                SetChunk(headIndex, true);
        }

        public bool IsEmpty(GridCoord coordinate) => _grid.IsEmpty(coordinate);
        public bool IsInBounds(GridCoord coordinate) => _grid.IsInBounds(coordinate);

        // Resolves any clicked cell (head or line) to the coordinate of its owning head.
        // Returns the input when the cell belongs to no arrow.
        public GridCoord GetHeadCoordinate(GridCoord coordinates)
        {
            int headIndex = HeadIndexAt(coordinates);
            return headIndex < 0 ? coordinates : _grid.IndexToCoordinates(headIndex);
        }

        public void RemoveAtCoordinate(GridCoord coordinates)
        {
            int headIndex = HeadIndexAt(coordinates);
            if (headIndex < 0)
                return;

            SetChunk(headIndex, false);
            _board.RemoveChunk(headIndex);
        }

        public bool IsPathClear(GridCoord coordinates)
        {
            int headIndex = HeadIndexAt(coordinates);
            if (headIndex < 0)
                return false;

            // Check from the head, in the head's direction. The self line-of-sight rule guarantees
            // the arrow's own line is never in front of it, so no own-cell exclusion is needed.
            var headCoord = _grid.IndexToCoordinates(headIndex);
            return _grid.IsPathClear(headCoord, _board.dataArrays.directionArray[headIndex]);
        }

        // The first occupied cell in front of the tapped arrow's head (used by the wrong-answer bump).
        // Returns the input when the cell belongs to no arrow.
        public GridCoord GetForwardBlocker(GridCoord coordinates)
        {
            int headIndex = HeadIndexAt(coordinates);
            if (headIndex < 0)
                return coordinates;

            var headCoord = _grid.IndexToCoordinates(headIndex);
            return _grid.FirstBlocked(headCoord, _board.dataArrays.directionArray[headIndex]);
        }

        private int HeadIndexAt(GridCoord coordinates) =>
            _grid.IsInBounds(coordinates) ? _board.GetHeadIndex(_grid.CoordinatesToIndex(coordinates)) : -1;

        // Marks the head cell and every line cell of its chunk.
        private void SetChunk(int headIndex, bool occupied)
        {
            _grid.Set(_grid.IndexToCoordinates(headIndex), occupied);

            var chunk = _board.GetLineChunk(headIndex);
            if (chunk == null)
                return;

            for (int i = 0; i < chunk.Length; i++)
                _grid.Set(_grid.IndexToCoordinates(chunk[i]), occupied);
        }
    }
}
