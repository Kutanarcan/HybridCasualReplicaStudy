using System;
using System.Runtime.CompilerServices;

namespace ReplicaProjects.MagicSort
{
    /// <summary>Read-only view over the flat board: bar b, slot s lives at b * barHeight + s (slot 0 = bottom).</summary>
    public readonly ref struct SequentialBarArrayInput
    {
        public readonly ReadOnlySpan<int> placementSpanArray;
        public readonly int barHeight;

        public int BarCount => placementSpanArray.Length / barHeight;
        public int BarHeight => barHeight;

        public SequentialBarArrayInput(ReadOnlySpan<int> sequentialBarArray, int barHeight)
        {
            if (barHeight <= 0)
                throw new ArgumentOutOfRangeException(nameof(barHeight), "barHeight must be positive");
            if (sequentialBarArray.Length % barHeight != 0)
                throw new ArgumentException("slots length must be a multiple of barHeight", nameof(sequentialBarArray));

            placementSpanArray = sequentialBarArray;
            this.barHeight = barHeight;
        }

        /// <summary>Bir bar'ın slotları (index 0 = alt). Kopyasız dilim.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan<int> Bar(int barIndex)
            => placementSpanArray.Slice(barIndex * barHeight, barHeight);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Placement(int barIndex, int placementIndex)
            => placementSpanArray[barIndex * barHeight + placementIndex];
    }
}
