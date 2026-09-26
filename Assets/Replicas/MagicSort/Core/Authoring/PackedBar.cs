namespace ReplicaProjects.MagicSort
{
    /// <summary>
    /// A bar packed into a ulong for the solver: a leading sentinel nibble (1), then one nibble per
    /// ball bottom -> top holding color + 1. Supports up to 15 colors and 15 balls per bar.
    /// </summary>
    public static class PackedBar
    {
        /// <summary>A bar with no balls: just the leading sentinel nibble.</summary>
        public const ulong Empty = 1UL;

        public static ulong[] Pack(int[] slots, int barHeight)
        {
            int barCount = barHeight > 0 ? slots.Length / barHeight : 0;
            var packed = new ulong[barCount];
            for (int b = 0; b < barCount; b++)
            {
                ulong v = Empty; // leading sentinel
                for (int j = 0; j < barHeight; j++)
                {
                    int color = slots[b * barHeight + j];
                    if (color == ColorSlot.Empty) break; // balls fill bottom-up; rest is empty
                    v = (v << 4) | (uint)(color + 1);
                }
                packed[b] = v;
            }
            return packed;
        }

        public static int Count(ulong bar)
        {
            int nibbles = 0;
            while (bar > 1UL) { bar >>= 4; nibbles++; }
            return nibbles;
        }

        public static int Top(ulong bar) => (int)(bar & 0xF) - 1;

        public static ulong Push(ulong bar, int color) => (bar << 4) | (uint)(color + 1);

        public static ulong Pop(ulong bar) => bar >> 4;

        public static bool IsUniform(ulong bar)
        {
            if (bar <= 1UL) return true;
            ulong top = bar & 0xF;
            for (bar >>= 4; bar > 1UL; bar >>= 4)
                if ((bar & 0xF) != top) return false;
            return true;
        }

        public static bool IsComplete(ulong bar, int height) => Count(bar) == height && IsUniform(bar);

        public static bool IsSolved(ulong[] state, int height)
        {
            foreach (var bar in state)
            {
                if (bar == Empty) continue;
                if (!IsComplete(bar, height)) return false;
            }
            return true;
        }
    }
}
