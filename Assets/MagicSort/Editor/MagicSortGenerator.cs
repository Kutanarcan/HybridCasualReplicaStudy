using System;
using System.Collections.Generic;
using System.Linq;

namespace ReplicaProjects.MagicSort
{
    public struct Metrics
    {
        public float Depth;          // 0-1 normalized
        public float Fragmentation;  // 0-1 normalized
        public int RawDepth;
        public int RawFragmentation;
    }

    public static class MagicSortGenerator
    {
        private static readonly Random _rng = new Random();

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

        public static List<Bar> Generate(
            int colorCount, int barHeight, int emptyBarCount,
            int depthLevel, int fragLevel, out SolveResult result,
            bool ensureSolvable = true, int maxAttempts = 100)
        {
            if (emptyBarCount == 0)
            {
                result = new SolveResult { Status = SolveStatus.Solved, MinMoves = 0, Attempts = 1 };
                return CreateSolvedLevel(colorCount, barHeight, emptyBarCount);
            }

            List<Bar> bars;
            result = default;
            int attempts = 0;

            do
            {
                attempts++;
                bars = CreateSolvedLevel(colorCount, barHeight, emptyBarCount);

                ShuffleInternal(bars, colorCount, depthLevel);
                EmptyTheEmptyBars(bars, colorCount, fragLevel);

                result = MagicSortSolver.Solve(MagicSortFlat.Flatten(bars, barHeight), barHeight);
            }
            while (ensureSolvable && result.Status != SolveStatus.Solvable
                   && result.Status != SolveStatus.Solved && attempts < maxAttempts);

            result.Attempts = attempts;
            return bars;
        }

        private static void ShuffleInternal(List<Bar> bars, int colorCount, int depthLevel)
        {
            int[] baseMoves = { 15, 40, 80 };
            int moveCount = baseMoves[Math.Min(depthLevel, 2)]
                          + _rng.Next(15) + colorCount * 4;

            int lastFrom = -1, lastTo = -1, prevFrom = -1;

            for (int m = 0; m < moveCount; m++)
            {
                var valid = new List<(int from, int to)>();
                for (int from = 0; from < bars.Count; from++)
                {
                    if (bars[from].IsEmpty) continue;
                    for (int to = 0; to < bars.Count; to++)
                    {
                        if (from == to || bars[to].IsFull) continue;
                        if (from == lastTo && to == lastFrom) continue;
                        valid.Add((from, to));
                    }
                }
                if (valid.Count == 0) break;

                (int f, int t) mv;
                if (depthLevel >= 2 && prevFrom >= 0 && _rng.NextDouble() < depthLevel * 0.25)
                {
                    var sameSrc = valid.Where(v => v.from == prevFrom).ToList();
                    mv = sameSrc.Count > 0
                        ? sameSrc[_rng.Next(sameSrc.Count)]
                        : valid[_rng.Next(valid.Count)];
                }
                else
                {
                    mv = valid[_rng.Next(valid.Count)];
                }

                bars[mv.t].Push(bars[mv.f].Pop());
                lastFrom = mv.f;
                lastTo = mv.t;
                prevFrom = mv.f;
            }
        }

        private static void EmptyTheEmptyBars(List<Bar> bars, int colorCount, int fragLevel)
        {
            for (int eb = colorCount; eb < bars.Count; eb++)
            {
                while (!bars[eb].IsEmpty)
                {
                    int item = bars[eb].Top;
                    var targets = new List<int>();
                    for (int t = 0; t < colorCount; t++)
                        if (!bars[t].IsFull)
                            targets.Add(t);

                    if (targets.Count == 0) break;

                    int pick;
                    if (fragLevel >= 2)
                    {
                        var diff = targets.Where(t =>
                            !bars[t].IsEmpty && bars[t].Top != item).ToList();
                        pick = diff.Count > 0 && _rng.NextDouble() < 0.7 + fragLevel * 0.1
                            ? diff[_rng.Next(diff.Count)]
                            : targets[_rng.Next(targets.Count)];
                    }
                    else if (fragLevel == 0)
                    {
                        var same = targets.Where(t =>
                            !bars[t].IsEmpty && bars[t].Top == item).ToList();
                        pick = same.Count > 0 && _rng.NextDouble() < 0.8
                            ? same[_rng.Next(same.Count)]
                            : targets[_rng.Next(targets.Count)];
                    }
                    else
                    {
                        pick = targets[_rng.Next(targets.Count)];
                    }

                    bars[pick].Push(bars[eb].Pop());
                }
            }
        }

        public static Metrics CalculateMetrics(IReadOnlyList<Bar> bars)
        {
            int height = bars.Count > 0 ? bars[0].Capacity : 0;
            int totalTrans = 0, totalDepth = 0, filled = 0;

            foreach (var bar in bars)
            {
                if (bar.Count <= 1) continue;
                filled++;

                for (int i = 1; i < bar.Count; i++)
                    if (bar[i] != bar[i - 1])
                        totalTrans++;

                int sameFromBottom = 1;
                for (int i = 1; i < bar.Count; i++)
                {
                    if (bar[i] == bar[0]) sameFromBottom++;
                    else break;
                }
                totalDepth += bar.Count - sameFromBottom;
            }

            int maxTrans = filled * (height - 1);
            int maxDepth = filled * (height - 1);

            return new Metrics
            {
                Depth = maxDepth > 0 ? (float)totalDepth / maxDepth : 0f,
                Fragmentation = maxTrans > 0 ? (float)totalTrans / maxTrans : 0f,
                RawDepth = totalDepth,
                RawFragmentation = totalTrans
            };
        }
    }
}
