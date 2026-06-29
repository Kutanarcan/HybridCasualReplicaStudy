using UnityEngine;

namespace ReplicaProjects.Arrows
{
    public class BoardController
    {
        private readonly GridOrchestrator _grid = new();
        private readonly BoardOrchestrator _board = new();

        public GridOrchestrator Grid => _grid;
        public BoardOrchestrator Board => _board;

        public void Initialize(LevelData level)
        {
            _grid.Initialize(new GridOrchestratorModel
            {
                width = level.width,
                height = level.height
            });
            _board.Initialize(level);

            PopulateOccupancy();
        }

        public void DeInitialize()
        {
            _board.DeInitialize();
            _grid.DeInitialize();
        }

        private void PopulateOccupancy()
        {
            foreach (var headIndex in _board.dataArrays.headIndexArray)
            {
                SetOccupied(headIndex);

                var chunk = _board.GetLineChuck(headIndex);
                if (chunk == null)
                    continue;

                foreach (var chunkIndex in chunk)
                    SetOccupied(chunkIndex);
            }
        }

        // Resolves any clicked cell (head or line) to the coordinate of its owning head.
        public Vector2Int GetHeadCoordinate(Vector2Int coordinates)
        {
            int headIndex = _board.GetHeadIndex(_grid.CoordinatesToIndex(coordinates));
            return _grid.IndexToCoordinates(headIndex);
        }

        public void RemoveAtCoordinate(Vector2Int coordinates)
        {
            int headIndex = _board.GetHeadIndex(_grid.CoordinatesToIndex(coordinates));
            if (headIndex < 0)
                return;

            // Free the head cell.
            var headCoord = _grid.IndexToCoordinates(headIndex);
            _grid.Set(headCoord.x, headCoord.y, false);

            // Free every line cell of the chunk.
            var chunk = _board.GetLineChuck(headIndex);
            if (chunk != null)
                foreach (var lineIndex in chunk)
                {
                    var lineCoord = _grid.IndexToCoordinates(lineIndex);
                    _grid.Set(lineCoord.x, lineCoord.y, false);
                }

            _board.RemoveChuck(headIndex);
        }

        private void SetOccupied(int index)
        {
            var coord = _grid.IndexToCoordinates(index);
            _grid.Set(coord.x, coord.y, true);
        }

        public bool IsEmpty(Vector2Int coordinate) => _grid.IsEmpty(coordinate.x, coordinate.y);
        public bool IsInBounds(Vector2Int coordinate) => _grid.IsInBounds(coordinate);

        public bool IsPathClear(Vector2Int coordinates)
        {
            int headIndex = _board.GetHeadIndex(_grid.CoordinatesToIndex(coordinates));
            if (headIndex < 0)
                return false;

            // Check from the head, in the head's direction. The self line-of-sight rule guarantees
            // the arrow's own line is never in front of it, so no own-cell exclusion is needed.
            var headCoord = _grid.IndexToCoordinates(headIndex);
            var direction = _board.dataArrays.directionArray[headIndex];
            return _grid.IsPathClear(headCoord, direction);
        }

        // The first occupied cell in front of the tapped arrow's head (used by the wrong-answer bump).
        public Vector2Int GetForwardBlocker(Vector2Int coordinates)
        {
            int headIndex = _board.GetHeadIndex(_grid.CoordinatesToIndex(coordinates));
            var headCoord = _grid.IndexToCoordinates(headIndex);
            var direction = _board.dataArrays.directionArray[headIndex];
            return _grid.FirstBlocked(headCoord, direction);
        }
    }
}
