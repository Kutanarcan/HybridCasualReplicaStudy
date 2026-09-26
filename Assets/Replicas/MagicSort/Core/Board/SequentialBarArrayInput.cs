using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace ReplicaProjects.MagicSort
{
    public readonly ref struct SequentialBarArrayInput
    {
        public readonly ReadOnlySpan<int> placementSpanArray;
        public readonly int barHeight;

        public int BarCount => placementSpanArray.Length / barHeight;
        public int BarHeight => barHeight;

        public SequentialBarArrayInput(ReadOnlySpan<int> sequentialBarArray, int barHeight)
        {
            Debug.Assert(barHeight > 0, "barHeight must be positive");
            Debug.Assert(sequentialBarArray.Length % barHeight == 0,
                "slots length must be a multiple of barHeight");
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