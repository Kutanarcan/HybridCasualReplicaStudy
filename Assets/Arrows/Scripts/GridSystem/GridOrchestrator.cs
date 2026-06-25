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
        private GridLogic _gridLogic;
        public GridOrchestratorModel model;

        public void Initialize(GridOrchestratorModel model)
        {
            this.model = model;
            _gridLogic = new GridLogic();
            _gridData = new bool[model.width * model.height];
        }

        public void DeInitialize()
        {
            model = null;
            _gridLogic = null;
            _gridData = null;
        }

        public int Lenght() => _gridData.Length;

        public void Set(int x, int y, bool value)
        {
            var index = _gridLogic.CoordinatesToIndex(x, y, model.width);

            if (!IsInBounds(index))
                return;

            _gridData[index] = value;
        }

        public bool IsEmpty(int x, int y)
        {
            return _gridLogic.IsEmpty(new GridCellInput
            {
                gridData = _gridData,
                width = model.width,
                x = x,
                y = y
            });
        }

        public bool IsInBounds(int index) => _gridLogic.IsInBounds(index, _gridData.Length);

        public Vector2Int IndexToCoordinates(int index) => _gridLogic.IndexToCoordinates(index, model.width);

        public int CoordinatesToIndex(Vector2Int coordinates) => _gridLogic.CoordinatesToIndex(coordinates.x, coordinates.y, model.width);
    }
}
