
using UnityEngine;

namespace ReplicaProjects.MagicSort
{
    public struct MagisSortDataOfArrays
    {
        public int[] sequentialBarArray;
    }

    public class MagicSortOrchestrator
    {
        private MagisSortDataOfArrays magicSortData;
        public readonly MagicSortLogic _magicSortLogic = new();

        private int _selectedBar = -1;
        public int _barHeight;
        public int _barCount;
        public int _colorCount;

        public void Initialize(MagicSortLevel level)
        {
            _barHeight = level.barHeight;
            _colorCount = level.colorCount;
            _barCount = level.barCount;

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
        public SequentialBarArrayInput GetBoard() => new(magicSortData.sequentialBarArray, _barHeight);

        public void DeInitialize()
        {

        }

        public TapResult HandleTap(int barIndex)
        {
            var board = new SequentialBarArrayInput(magicSortData.sequentialBarArray, _barHeight);

            if (_magicSortLogic.IsBarSolved(in board, barIndex))
                return TapResult.Ignored();

            if (_magicSortLogic.IsAllBarsSolved(in board))
                return TapResult.Ignored();

            var result = _magicSortLogic.EvaluateTap(board, _selectedBar, barIndex);

            _selectedBar = result.NewSource;

            if (result.Kind == TapKind.Consumed)
            {
                _magicSortLogic.ApplyPour(magicSortData.sequentialBarArray, _barHeight,
                                          result.SourceBar, result.TargetBar, result.Pour.movedCount);

                var newBoard = GetBoard();
                result = result.WithSolveInfo(
                    _magicSortLogic.IsBarSolved(in newBoard, result.TargetBar),
                    _magicSortLogic.IsAllBarsSolved(in newBoard));
            }

            return result;
        }
    }
}
