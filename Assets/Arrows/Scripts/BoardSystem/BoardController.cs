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

        public void RemoveAtCoordinate(Vector2Int coordinates)
        {
            _grid.Set(coordinates.x, coordinates.y, false);

            _board.RemoveChuck(_grid.CoordinatesToIndex(coordinates));
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
            var index = _grid.CoordinatesToIndex(coordinates);
            var direction = _board.dataArrays.directionArray[index];
            return _grid.IsPathClear(coordinates, direction);
        }
    }
}
