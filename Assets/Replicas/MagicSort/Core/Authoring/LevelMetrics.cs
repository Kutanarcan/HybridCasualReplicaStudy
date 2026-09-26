using System.Collections.Generic;

namespace ReplicaProjects.MagicSort
{
    /// <summary>
    /// Difficulty indicators of a layout. Depth: how many balls sit above a bar's bottom run.
    /// Fragmentation: how often adjacent balls change color. Both also normalized to 0-1.
    /// </summary>
    public struct LevelMetrics
    {
        public float Depth;
        public float Fragmentation;
        public int RawDepth;
        public int RawFragmentation;

        public static LevelMetrics Calculate(IReadOnlyList<Bar> bars)
        {
            int height = bars.Count > 0 ? bars[0].Capacity : 0;
            int totalTransitions = 0, totalDepth = 0, filled = 0;

            foreach (var bar in bars)
            {
                if (bar.Count <= 1) continue;
                filled++;

                for (int i = 1; i < bar.Count; i++)
                    if (bar[i] != bar[i - 1])
                        totalTransitions++;

                int sameFromBottom = 1;
                while (sameFromBottom < bar.Count && bar[sameFromBottom] == bar[0])
                    sameFromBottom++;
                totalDepth += bar.Count - sameFromBottom;
            }

            int max = filled * (height - 1);

            return new LevelMetrics
            {
                Depth = max > 0 ? (float)totalDepth / max : 0f,
                Fragmentation = max > 0 ? (float)totalTransitions / max : 0f,
                RawDepth = totalDepth,
                RawFragmentation = totalTransitions
            };
        }
    }
}
