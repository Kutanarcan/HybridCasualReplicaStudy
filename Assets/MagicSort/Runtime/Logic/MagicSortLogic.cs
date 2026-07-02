using Codice.CM.Client.Differences;
using System;
using UnityEngine;

namespace ReplicaProjects.MagicSort
{
    public class MagicSortLogic
    {
        public bool IsBarEmpty(ReadOnlySpan<int> sequentialBarArray, int barIndex, int barHeight)
        {
            // We just look at the first item on the bar

            return false;
        }

        public bool IsBarFull(ReadOnlySpan<int> sequentialBarArray, int barIndex, int barHeight)
        {
            // We just look at the last item on the bar
            return false;
        }
        public bool IsSameBar(int sourceIndex, int targetIndex)
        {
            return sourceIndex == targetIndex;
        }

        public bool IsBarSolved(ReadOnlySpan<int> sequentialBarArray, int barIndex, int barHeight)
        {
            // TODO: Look for first color same with others
            // Note: Empty bars cannot solvable, we either check IsBarFull or IsBarEmpty First
            return false;
        }

        public bool IsAllBarsSolved(ReadOnlySpan<int> sequentialBarArray, int barHeight)
        {
            // TODO: Look for first color same with others, including Empty -> 0,0,0,0 - 1,1,1,1 - -1,-1,-1,-1 -> Solved because all same
            return false;
        }

        public int GetTopBarIndexColorValue(ReadOnlySpan<int> sequentialBarArray, int barHeight, int barIndex)
        {
            // TODO: Return last color index value from bar
            return 0;
        }

        public AvailablePlacementResult EvaluateAvailableBarPlacement(
            ReadOnlySpan<int> sequentialBarArray,
            int barHeight,
            int sourceBarIndex, int targetBarIndex)
        {
            Debug.Assert(!IsBarEmpty(sequentialBarArray, sourceBarIndex, barHeight));

            // Is Same bar -> No Move Count
            // Is Empty -> Calculate Move Count
            // Is Full -> No Move Count
            // Get Color Index Value
            // Has Available Bar Placement
            // Does not have Available -> No Move Count
            // If has Available calculate all values 

            // Why do we haved Move Count -> Source Bar can able to more then 1 same color stack on each other, if so we have to move all of them

            return new AvailablePlacementResult();
        }
    }
}
