using UnityEngine;

namespace ReplicaProjects.MagicSort
{
    public struct MagisSortDataOfArrays
    {
        public int[] sequentialBarArray;
    }

    public class MagicSortOrchestrator
    {
        public MagisSortDataOfArrays _magicSortData;
        private readonly MagicSortLogic _magicSortLogic = new();

        public void Initialize(MagicSortLevel level)
        {
            _magicSortData = new MagisSortDataOfArrays()
            {
                sequentialBarArray = new int[level.slots.Length]
            };

            for (int i = 0; i < level.slots.Length; i++)
            {
                int barColorIndex = level.slots[i];
                _magicSortData.sequentialBarArray[i] = barColorIndex;
            }
        }

        public void DeInitialize()
        {

        }
    }
}
