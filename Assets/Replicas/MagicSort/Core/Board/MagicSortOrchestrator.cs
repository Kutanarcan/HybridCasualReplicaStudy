using System;

namespace ReplicaProjects.MagicSort
{
    /// <summary>
    /// Owns the mutable board and the current selection; turns taps into validated moves.
    /// Once the level is solved it stays solved — later taps are ignored until the next Initialize.
    /// </summary>
    public class MagicSortOrchestrator
    {
        private const int NoSelection = -1;

        private readonly MagicSortLogic _magicSortLogic = new();
        private int[] _slots = Array.Empty<int>();
        private int _barHeight = 1;
        private int _selectedBar = NoSelection;

        public MagicSortLogic Logic => _magicSortLogic;
        public bool IsLevelSolved { get; private set; }

        public void Initialize(ReadOnlySpan<int> slots, int barHeight)
        {
            _barHeight = barHeight;
            _slots = slots.ToArray();
            _selectedBar = NoSelection;
            IsLevelSolved = _magicSortLogic.IsAllBarsSolved(GetBoard());
        }

        public SequentialBarArrayInput GetBoard() => new(_slots, _barHeight);

        public void ClearSelection() => _selectedBar = NoSelection;

        public TapResult HandleTap(int barIndex)
        {
            var board = GetBoard();

            if (IsLevelSolved || _magicSortLogic.IsBarSolved(in board, barIndex))
                return TapResult.Ignored();

            var result = _magicSortLogic.EvaluateTap(board, _selectedBar, barIndex);
            _selectedBar = result.NewSource;

            if (result.Kind != TapKind.Consumed)
                return result;

            _magicSortLogic.ApplyPour(_slots, _barHeight, result.SourceBar, result.TargetBar,
                                      result.TransportResult.movedCount);

            var newBoard = GetBoard();
            IsLevelSolved = _magicSortLogic.IsAllBarsSolved(in newBoard);

            return result.WithSolveInfo(_magicSortLogic.IsBarSolved(in newBoard, result.TargetBar), IsLevelSolved);
        }
    }
}
