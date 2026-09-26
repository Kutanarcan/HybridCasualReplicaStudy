using System.Collections.Generic;

namespace ReplicaProjects.MagicSort
{
    public struct MoveRecord
    {
        public int From;
        public int To;
        public int Color; // color index
    }

    /// <summary>
    /// Editor-only working board for authoring levels. Uses the <b>designer's</b> rule set:
    /// the top ball may move onto ANY non-full bar, with no color restriction, so a designer
    /// can freely arrange any layout. This is deliberately different from the runtime game
    /// rules (empty / same-color only) — the two rule sets live in separate classes on purpose.
    /// </summary>
    public class MagicSortLevelEditorLogic
    {
        public int BarHeight { get; private set; }
        public int TotalBars => _bars.Count;

        private List<Bar> _bars;
        private List<Bar> _initial;   // snapshot of the loaded level, for Restart()
        private readonly List<MoveRecord> _historyList = new List<MoveRecord>();

        public MagicSortLevelEditorLogic(IReadOnlyList<Bar> bars)
        {
            LoadLevel(bars);
        }

        /// <summary>Replace the board with a level (deep-copied).</summary>
        public void LoadLevel(IReadOnlyList<Bar> bars)
        {
            _bars = Clone(bars);
            _initial = Clone(bars);
            BarHeight = _bars.Count > 0 ? _bars[0].Capacity : 0;
            _historyList.Clear();
        }

        /// <summary>Restore the board to the level exactly as it was loaded.</summary>
        public void Restart()
        {
            _bars = Clone(_initial);
            _historyList.Clear();
        }

        // ── State access ──

        public int GetBarCount() => _bars.Count;
        public int GetBarSize(int barIndex) => _bars[barIndex].Count;
        public int GetColor(int barIndex, int slotIndex) => _bars[barIndex][slotIndex];
        public bool IsBarEmpty(int barIndex) => _bars[barIndex].IsEmpty;
        public bool IsBarFull(int barIndex) => _bars[barIndex].IsFull;
        public int GetTopColor(int barIndex) => _bars[barIndex].Top;
        public int MoveCount => _historyList.Count;
        public bool CanUndo => _historyList.Count > 0;

        public bool IsPuzzleSolved()
        {
            foreach (var bar in _bars)
                if (!bar.IsEmpty && !bar.IsComplete)
                    return false;
            return true;
        }

        /// <summary>Deep copy of the current board (for saving / metrics / solving).</summary>
        public List<Bar> Snapshot() => Clone(_bars);

        // ── Moves (designer free-move — no color restriction) ──

        public bool CanMove(int from, int to)
        {
            if (from == to) return false;
            if (from < 0 || from >= _bars.Count) return false;
            if (to < 0 || to >= _bars.Count) return false;
            if (_bars[from].IsEmpty) return false;
            if (_bars[to].IsFull) return false;
            return true;
        }

        public bool TryMove(int from, int to)
        {
            if (!CanMove(from, to)) return false;
            int color = _bars[from].Pop();
            _bars[to].Push(color);
            _historyList.Add(new MoveRecord { From = from, To = to, Color = color });
            return true;
        }

        public bool Undo()
        {
            if (_historyList.Count == 0) return false;
            var last = _historyList[_historyList.Count - 1];
            _historyList.RemoveAt(_historyList.Count - 1);
            _bars[last.To].Pop();
            _bars[last.From].Push(last.Color);
            return true;
        }

        /// <summary>BFS solvability / min-move check of the current board (runtime rules).</summary>
        public SolveResult CheckCurrentState()
        {
            return MagicSortSolver.Solve(MagicSortFlat.Flatten(_bars, BarHeight), BarHeight);
        }

        private static List<Bar> Clone(IReadOnlyList<Bar> bars)
        {
            var copy = new List<Bar>(bars.Count);
            foreach (var b in bars)
                copy.Add(b.Clone());
            return copy;
        }
    }
}
