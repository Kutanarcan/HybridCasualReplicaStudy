
namespace ReplicaProjects.MagicSort
{
    public struct MagisSortDataOfArrays
    {
        public int[] sequentialBarArray;
    }

    public class MagicSortOrchestrator
    {
        public MagisSortDataOfArrays magicSortData;
        private readonly MagicSortLogic _magicSortLogic = new();

        private int _selectedBar = -1;
        private int _barHeight;

        public void Initialize(MagicSortLevel level)
        {
            _barHeight = level.barHeight;

            magicSortData = new MagisSortDataOfArrays()
            {
                sequentialBarArray = new int[level.slots.Length]
            };

            for (int i = 0; i < level.slots.Length; i++)
            {
                int barColorIndex = level.slots[i];
                magicSortData.sequentialBarArray[i] = barColorIndex;
            }
        }

        public void DeInitialize()
        {

        }


        public TapResult HandleTap(int barIndex)
        {
            var board = new SequentialBarArrayInput(magicSortData.sequentialBarArray, _barHeight);
            
            var result = _magicSortLogic.EvaluateTap(board, _selectedBar, barIndex);
            
            _selectedBar = result.NewSource;

            if (result.Kind == TapKind.Consumed)
            {
                _magicSortLogic.ApplyPour(magicSortData.sequentialBarArray, _barHeight,
                                          result.SourceBar, result.TargetBar, result.Pour.movedCount);
            }

            return result;
        }
    }
}
