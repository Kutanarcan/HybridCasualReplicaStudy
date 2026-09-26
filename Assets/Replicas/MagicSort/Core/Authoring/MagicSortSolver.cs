using System.Collections.Generic;

namespace ReplicaProjects.MagicSort
{
    /// <summary>
    /// Breadth-first search over packed boards under the runtime rules (move the top ball onto an
    /// empty bar or a same-color top). BFS depth at the first solved child is the minimum move count.
    /// </summary>
    public static class MagicSortSolver
    {
        public const int MaxBfsStates = 600_000;

        /// <param name="slots">Flat board, length barHeight*barCount, indexed bar*barHeight+slot;
        /// each cell is a color index or <see cref="ColorSlot.Empty"/>.</param>
        public static SolveResult Solve(int[] slots, int barHeight)
        {
            var initial = PackedBar.Pack(slots, barHeight);

            if (PackedBar.IsSolved(initial, barHeight))
                return new SolveResult { Status = SolveStatus.Solved, MinMoves = 0 };

            var visited = new HashSet<BoardStateKey> { new(initial) };
            var frontier = new List<ulong[]> { initial };
            int depth = 0;

            while (frontier.Count > 0)
            {
                depth++;
                var next = new List<ulong[]>();

                foreach (var state in frontier)
                {
                    var outcome = Expand(state, barHeight, visited, next);
                    if (outcome == SolveStatus.Solvable)
                        return new SolveResult { Status = SolveStatus.Solvable, MinMoves = depth, StatesExplored = visited.Count };
                    if (outcome == SolveStatus.Timeout)
                        return new SolveResult { Status = SolveStatus.Timeout, StatesExplored = visited.Count };
                }

                frontier = next;
            }

            return new SolveResult { Status = SolveStatus.Unsolvable, StatesExplored = visited.Count };
        }

        // Adds every unseen child of `state` to `next`. Returns Solvable if a child is solved,
        // Timeout if the state budget ran out, Unsolvable otherwise (= keep searching).
        private static SolveStatus Expand(ulong[] state, int height, HashSet<BoardStateKey> visited, List<ulong[]> next)
        {
            for (int from = 0; from < state.Length; from++)
            {
                if (state[from] == PackedBar.Empty || PackedBar.IsComplete(state[from], height))
                    continue;

                int topColor = PackedBar.Top(state[from]);
                bool usedEmpty = false;

                for (int to = 0; to < state.Length; to++)
                {
                    if (!CanPour(state, from, to, topColor, height, ref usedEmpty))
                        continue;

                    var child = (ulong[])state.Clone();
                    child[to] = PackedBar.Push(child[to], topColor);
                    child[from] = PackedBar.Pop(child[from]);

                    var key = new BoardStateKey(child);
                    if (visited.Contains(key))
                        continue;

                    if (PackedBar.IsSolved(child, height))
                        return SolveStatus.Solvable;

                    visited.Add(key);
                    if (visited.Count >= MaxBfsStates)
                        return SolveStatus.Timeout;

                    next.Add(child);
                }
            }

            return SolveStatus.Unsolvable;
        }

        private static bool CanPour(ulong[] state, int from, int to, int topColor, int height, ref bool usedEmpty)
        {
            if (from == to || PackedBar.Count(state[to]) >= height)
                return false;

            if (state[to] != PackedBar.Empty)
                return PackedBar.Top(state[to]) == topColor;

            // Only one empty destination is worth trying, and never shuffle an already-uniform bar
            // into a fresh empty one.
            if (usedEmpty)
                return false;

            usedEmpty = true;
            return !PackedBar.IsUniform(state[from]);
        }
    }
}
