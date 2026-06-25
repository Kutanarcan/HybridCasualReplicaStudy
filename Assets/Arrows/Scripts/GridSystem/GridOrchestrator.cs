using UnityEngine;

namespace ReplicaProjects.Arrows
{
    public class GridOrchestratorModel
    {
        public int width;
        public int height;
    }

    public class GridOrchestrator
    {
        private bool[] _gridData;
        public GridOrchestratorModel model;

        public void Initialize(GridOrchestratorModel model)
        {
            this.model = model;
            _gridData = new bool[model.width * model.height];
        }

        public void DeInitialize()
        {
            model = null;
            _gridData = null;
        }

        public int Lenght() => _gridData.Length;

        public void Set(int x, int y, bool value)
        {
            var index = GridMath.CoordinatesToIndex(x, y, model.width);

            if (!IsInBounds(index))
                return;

            _gridData[index] = value;
        }

        public bool IsEmpty(int x, int y)
        {
            var index = GridMath.CoordinatesToIndex(x, y, model.width);

            return IsInBounds(index) && !_gridData[index];
        }

        public bool IsInBounds(int index)
        {
            return GridMath.IsInBounds(index, _gridData.Length);
        }

        public Vector2Int IndexToCoordinates(int index) => GridMath.IndexToCoordinates(index, model.width);

        public int CoordinatesToIndex(Vector2Int coordinates) => GridMath.CoordinatesToIndex(coordinates.x, coordinates.y, model.width);
    }
}
