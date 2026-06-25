
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

        private GridOrchestratorModel _model;

        public void Initialize(GridOrchestratorModel model)
        {
            _model = model;
            _gridData = new bool[model.height * model.height];
        }

        public void DeInitialize()
        {
            _model = null;
            _gridData = null;
        }

        public void Set(int x, int y, bool value)
        {
            var index = y * _model.width + x;

            if (!IsInBounds(index))
                return;

            _gridData[index] = value;
        }

        public bool IsEmpty(int x, int y)
        {
            var index = y * _model.width + x;
         
            return IsInBounds(index) && _gridData[index];
        }

        public bool IsInBounds(int index)
        {
            return _gridData.Length > 0 && _gridData.Length < index;
        }
    }
}
