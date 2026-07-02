using System.Collections.Generic;

namespace ReplicaProjects.MagicSort
{
    /// <summary>
    /// The single bridge between the editor's OO <see cref="Bar"/> authoring model and the
    /// runtime Data-Oriented flat layout (see <see cref="MagicSortLevel"/>). Kept pure (no
    /// UnityEngine) so it stays unit-testable.
    ///
    /// Flat layout: length <c>barHeight * barCount</c>, indexed <c>bar * barHeight + slot</c>
    /// (slot 0 = bottom); balls fill each bar bottom-up, remaining slots are
    /// <see cref="MagicSortLevel.Empty"/>.
    /// </summary>
    public static class MagicSortFlat
    {
        public static int[] Flatten(IReadOnlyList<Bar> bars, int barHeight)
        {
            var slots = new int[bars.Count * barHeight];
            for (int b = 0; b < bars.Count; b++)
            {
                var bar = bars[b];
                for (int j = 0; j < barHeight; j++)
                    slots[b * barHeight + j] = j < bar.Count ? bar[j] : MagicSortLevel.Empty;
            }
            return slots;
        }

        public static List<Bar> ToBars(int[] slots, int barHeight)
        {
            int barCount = barHeight > 0 ? slots.Length / barHeight : 0;
            var bars = new List<Bar>(barCount);
            for (int b = 0; b < barCount; b++)
            {
                var bar = new Bar(barHeight);
                for (int j = 0; j < barHeight; j++)
                {
                    int color = slots[b * barHeight + j];
                    if (color == MagicSortLevel.Empty) break; // rest of the bar is empty
                    bar.Push(color);
                }
                bars.Add(bar);
            }
            return bars;
        }
    }
}
