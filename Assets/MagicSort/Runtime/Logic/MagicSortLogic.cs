using UnityEngine;

namespace ReplicaProjects.MagicSort
{
    public class MagicSortLogic
    {
        private const int EMPTY_INDEX_VALUE = MagicSortLevel.Empty;

        public bool IsBarEmpty(in SequentialBarArrayInput input, int barIndex)
        {
            return input.Placement(barIndex, 0) == EMPTY_INDEX_VALUE;
        }

        public bool IsBarFull(in SequentialBarArrayInput input, int barIndex)
        {
            return input.Placement(barIndex, input.barHeight - 1) != EMPTY_INDEX_VALUE;
        }

        public bool IsSameBar(int sourceIndex, int targetIndex)
        {
            return sourceIndex == targetIndex;
        }

        public bool IsBarSolved(in SequentialBarArrayInput input, int barIndex)
        {
            if (IsBarEmpty(input, barIndex))
                return false;

            return IsBarUniform(input.Bar(barIndex));
        }

        public bool IsAllBarsSolved(in SequentialBarArrayInput boardInput)
        {
            for (int barIndex = 0; barIndex < boardInput.BarCount; barIndex++)
            {
                if (!IsBarUniform(boardInput.Bar(barIndex)))
                    return false;
            }

            return true;
        }

        public int GetTopBarIndexColorValue(in SequentialBarArrayInput input, int barIndex)
        {
            var bar = input.Bar(barIndex);
            var index = TopFilledSlotIndex(bar);

            return index > 0 ? bar[index] : EMPTY_INDEX_VALUE;
        }

        public int TopFilledSlotIndex(System.ReadOnlySpan<int> bar)
        {
            for (int slot = bar.Length - 1; slot >= 0; slot--)
            {
                if (bar[slot] != EMPTY_INDEX_VALUE)
                    return slot;
            }

            return EMPTY_INDEX_VALUE;
        }
        public bool IsBarUniform(System.ReadOnlySpan<int> bar)
        {
            int first = bar[0];
            for (int slot = 1; slot < bar.Length; slot++)
            {
                if (bar[slot] != first)
                    return false;
            }

            return true;
        }
        public int SameColorRunFromTop(System.ReadOnlySpan<int> bar, int topSlot, int color)
        {
            int run = 0;

            for (int slot = topSlot; slot >= 0 && bar[slot] == color; slot--)
            {
                run++;
            }

            return run;
        }

        public AvailablePlacementResult EvaluateAvailableBarPlacement(in SequentialBarArrayInput input, int sourceBarIndex, int targetBarIndex)
        {
            Debug.Assert(!IsBarEmpty(input, sourceBarIndex));

            // Is Same bar -> No Move Count
            if (IsSameBar(sourceBarIndex, targetBarIndex))
                return AvailablePlacementResult.Rejected(PlacementOutcome.SameBar);

            // Is Full -> No Move Count
            if (IsBarFull(input, targetBarIndex))
                return AvailablePlacementResult.Rejected(PlacementOutcome.TargetFull);

            var source = input.Bar(sourceBarIndex);
            var target = input.Bar(targetBarIndex);

            int sourceTopSlot = TopFilledSlotIndex(source);
            int sourceColor = source[sourceTopSlot];

            // Is Empty -> any color is allowed. Otherwise colors must match.
            bool targetEmpty = target[0] == EMPTY_INDEX_VALUE;
            if (!targetEmpty && GetTopBarIndexColorValue(input, targetBarIndex) != sourceColor)
                return AvailablePlacementResult.Rejected(PlacementOutcome.ColorMismatch);

            int sameColorRun = SameColorRunFromTop(source, sourceTopSlot, sourceColor);
            int targetFree = input.barHeight - TopFilledSlotIndex(target) - 1;
            int movedCount = Mathf.Min(sameColorRun, targetFree);

            int sourceRemaining = sourceTopSlot + 1 - movedCount;
            int sourceNewTop = sourceRemaining > 0 ? source[sourceRemaining - 1] : EMPTY_INDEX_VALUE;
            int targetNewTop = sourceColor;

            return AvailablePlacementResult.Moved(movedCount, sourceNewTop, targetNewTop);
        }
    }
}
