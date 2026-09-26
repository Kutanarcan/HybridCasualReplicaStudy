using System;
using System.Collections.Generic;

namespace ReplicaProjects.MagicSort
{
    public enum SolveStatus
    {
        Solved,
        Solvable,
        Unsolvable,
        Timeout
    }

    public struct SolveResult
    {
        public SolveStatus Status;
        public int MinMoves;
        public int StatesExplored;
        public int Attempts;
    }

    public static class MagicSortSolver
    {
        public const int MaxBfsStates = 600_000;

        // A bar with no balls: just the leading sentinel nibble.
        private const ulong EmptyBar = 1UL;

        /// <param name="slots">Flat board, length barHeight*barCount, indexed bar*barHeight+slot;
        /// each cell is a color index or <see cref="MagicSortLevel.Empty"/>.</param>
        public static SolveResult Solve(int[] slots, int barHeight)
        {
            int height = barHeight;
            var initial = Pack(slots, barHeight);

            if (IsSolved(initial, height))
                return new SolveResult { Status = SolveStatus.Solved, MinMoves = 0 };

            var visited = new HashSet<StateKey> { new StateKey(initial) };
            var frontier = new List<ulong[]> { initial };
            int depth = 0;

            while (frontier.Count > 0)
            {
                depth++;
                var next = new List<ulong[]>();

                foreach (var state in frontier)
                {
                    for (int from = 0; from < state.Length; from++)
                    {
                        if (state[from] == EmptyBar) continue;
                        if (IsComplete(state[from], height)) continue;

                        int topColor = Top(state[from]);
                        bool usedEmpty = false;

                        for (int to = 0; to < state.Length; to++)
                        {
                            if (from == to) continue;
                            if (Count(state[to]) >= height) continue;

                            bool toEmpty = state[to] == EmptyBar;
                            if (!toEmpty && Top(state[to]) != topColor) continue;

                            if (toEmpty)
                            {
                                // Only one empty destination is worth trying, and never
                                // shuffle an already-uniform bar into a fresh empty one.
                                if (usedEmpty) continue;
                                usedEmpty = true;
                                if (IsUniform(state[from])) continue;
                            }

                            var child = (ulong[])state.Clone();
                            child[to] = Push(child[to], topColor);
                            child[from] = Pop(child[from]);

                            var key = new StateKey(child);
                            if (visited.Contains(key)) continue;

                            if (IsSolved(child, height))
                                return new SolveResult
                                {
                                    Status = SolveStatus.Solvable,
                                    MinMoves = depth,
                                    StatesExplored = visited.Count
                                };

                            visited.Add(key);
                            if (visited.Count >= MaxBfsStates)
                                return new SolveResult
                                {
                                    Status = SolveStatus.Timeout,
                                    StatesExplored = visited.Count
                                };

                            next.Add(child);
                        }
                    }
                }

                frontier = next;
            }

            return new SolveResult
            {
                Status = SolveStatus.Unsolvable,
                StatesExplored = visited.Count
            };
        }

        // ── Packed-bar primitives ──

        private static ulong[] Pack(int[] slots, int barHeight)
        {
            int barCount = barHeight > 0 ? slots.Length / barHeight : 0;
            var packed = new ulong[barCount];
            for (int b = 0; b < barCount; b++)
            {
                ulong v = EmptyBar; // leading sentinel
                for (int j = 0; j < barHeight; j++)
                {
                    int color = slots[b * barHeight + j];
                    if (color == MagicSortLevel.Empty) break; // balls fill bottom-up; rest is empty
                    v = (v << 4) | (uint)(color + 1);
                }
                packed[b] = v;
            }
            return packed;
        }

        private static int Count(ulong bar)
        {
            int nibbles = 0;
            while (bar > 1UL) { bar >>= 4; nibbles++; }
            return nibbles;
        }

        private static int Top(ulong bar) => (int)(bar & 0xF) - 1;

        private static ulong Push(ulong bar, int color) => (bar << 4) | (uint)(color + 1);

        private static ulong Pop(ulong bar) => bar >> 4;

        private static bool IsUniform(ulong bar)
        {
            if (bar <= 1UL) return true;
            ulong top = bar & 0xF;
            for (bar >>= 4; bar > 1UL; bar >>= 4)
                if ((bar & 0xF) != top) return false;
            return true;
        }

        private static bool IsComplete(ulong bar, int height) =>
            Count(bar) == height && IsUniform(bar);

        private static bool IsSolved(ulong[] state, int height)
        {
            foreach (var bar in state)
            {
                if (bar == EmptyBar) continue;
                if (!IsComplete(bar, height)) return false;
            }
            return true;
        }

        private readonly struct StateKey : IEquatable<StateKey>
        {
            private readonly ulong[] _bars;
            private readonly int _hash;

            public StateKey(ulong[] bars)
            {
                _bars = (ulong[])bars.Clone();
                Array.Sort(_bars);

                int h = 17;
                foreach (var b in _bars)
                    h = h * 31 + b.GetHashCode();
                _hash = h;
            }

            public bool Equals(StateKey other)
            {
                if (_bars.Length != other._bars.Length) return false;
                for (int i = 0; i < _bars.Length; i++)
                    if (_bars[i] != other._bars[i]) return false;
                return true;
            }

            public override bool Equals(object obj) => obj is StateKey k && Equals(k);
            public override int GetHashCode() => _hash;
        }
    }
}
