using System;
using System.Collections.Generic;
using ReplicaProjects.Common;

namespace ReplicaProjects.MagicSort
{
    /// <summary>
    /// Scrambles a solved level with random free moves (depth), then empties the spare bars back into
    /// the color bars (fragmentation), retrying until the solver confirms the result is solvable.
    /// </summary>
    public sealed class MagicSortGenerator
    {
        private static readonly int[] BaseMoves = { 15, 40, 80 };

        private readonly IRandomSource _random;
        private readonly List<(int from, int to)> _moves = new();
        private readonly List<int> _targets = new();
        private readonly List<int> _preferred = new();

        public MagicSortGenerator(IRandomSource random) => _random = random;

        public static List<Bar> CreateSolvedLevel(int colorCount, int barHeight, int emptyBarCount)
        {
            var bars = new List<Bar>(colorCount + emptyBarCount);
            for (int i = 0; i < colorCount; i++)
            {
                var bar = new Bar(barHeight);
                for (int j = 0; j < barHeight; j++)
                    bar.Push(i);
                bars.Add(bar);
            }
            for (int i = 0; i < emptyBarCount; i++)
                bars.Add(new Bar(barHeight));
            return bars;
        }

        public List<Bar> Generate(int colorCount, int barHeight, int emptyBarCount, int depthLevel, int fragLevel,
                                  out SolveResult result, bool ensureSolvable = true, int maxAttempts = 100)
        {
            if (emptyBarCount == 0)
            {
                result = new SolveResult { Status = SolveStatus.Solved, MinMoves = 0, Attempts = 1 };
                return CreateSolvedLevel(colorCount, barHeight, emptyBarCount);
            }

            List<Bar> bars;
            int attempts = 0;

            do
            {
                attempts++;
                bars = CreateSolvedLevel(colorCount, barHeight, emptyBarCount);

                Scramble(bars, colorCount, depthLevel);
                EmptyTheEmptyBars(bars, colorCount, fragLevel);

                result = MagicSortSolver.Solve(MagicSortFlat.Flatten(bars, barHeight), barHeight);
            }
            while (ensureSolvable && result.Status != SolveStatus.Solvable
                   && result.Status != SolveStatus.Solved && attempts < maxAttempts);

            result.Attempts = attempts;
            return bars;
        }

        private void Scramble(List<Bar> bars, int colorCount, int depthLevel)
        {
            int moveCount = BaseMoves[Math.Min(depthLevel, 2)] + _random.Next(0, 15) + colorCount * 4;
            int lastFrom = -1, lastTo = -1;

            for (int m = 0; m < moveCount; m++)
            {
                CollectFreeMoves(bars, lastFrom, lastTo);
                if (_moves.Count == 0) break;

                // Deeper levels keep digging into the same source bar.
                var move = depthLevel >= 2 && lastFrom >= 0 && _random.NextDouble() < depthLevel * 0.25
                    ? PickPreferringSource(lastFrom)
                    : _moves[_random.Next(0, _moves.Count)];

                bars[move.to].Push(bars[move.from].Pop());
                lastFrom = move.from;
                lastTo = move.to;
            }
        }

        // Every designer free-move except undoing the previous one.
        private void CollectFreeMoves(List<Bar> bars, int lastFrom, int lastTo)
        {
            _moves.Clear();
            for (int from = 0; from < bars.Count; from++)
            {
                if (bars[from].IsEmpty) continue;
                for (int to = 0; to < bars.Count; to++)
                {
                    if (from == to || bars[to].IsFull) continue;
                    if (from == lastTo && to == lastFrom) continue;
                    _moves.Add((from, to));
                }
            }
        }

        private (int from, int to) PickPreferringSource(int source)
        {
            _preferred.Clear();
            for (int i = 0; i < _moves.Count; i++)
                if (_moves[i].from == source)
                    _preferred.Add(i);

            return _preferred.Count > 0
                ? _moves[_preferred[_random.Next(0, _preferred.Count)]]
                : _moves[_random.Next(0, _moves.Count)];
        }

        private void EmptyTheEmptyBars(List<Bar> bars, int colorCount, int fragLevel)
        {
            for (int spare = colorCount; spare < bars.Count; spare++)
            {
                while (!bars[spare].IsEmpty)
                {
                    int item = bars[spare].Top;

                    _targets.Clear();
                    for (int t = 0; t < colorCount; t++)
                        if (!bars[t].IsFull)
                            _targets.Add(t);

                    if (_targets.Count == 0) break;

                    bars[PickTarget(bars, item, fragLevel)].Push(bars[spare].Pop());
                }
            }
        }

        // High fragmentation prefers a different top color; low prefers the same one.
        private int PickTarget(List<Bar> bars, int item, int fragLevel)
        {
            if (fragLevel == 1)
                return _targets[_random.Next(0, _targets.Count)];

            bool wantSame = fragLevel == 0;
            _preferred.Clear();
            foreach (var t in _targets)
                if (!bars[t].IsEmpty && (bars[t].Top == item) == wantSame)
                    _preferred.Add(t);

            double preferChance = wantSame ? 0.8 : 0.7 + fragLevel * 0.1;
            return _preferred.Count > 0 && _random.NextDouble() < preferChance
                ? _preferred[_random.Next(0, _preferred.Count)]
                : _targets[_random.Next(0, _targets.Count)];
        }
    }
}
