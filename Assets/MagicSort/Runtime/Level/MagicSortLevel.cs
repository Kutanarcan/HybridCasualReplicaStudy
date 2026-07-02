using UnityEngine;

namespace ReplicaProjects.MagicSort
{
    [CreateAssetMenu(menuName = "MagicSort/Level", fileName = "MagicSortLevel")]
    public class MagicSortLevel : ScriptableObject
    {
        public const int Empty = -1;

        [Min(1)] public int colorCount;
        [Min(1)] public int barHeight = 4;
        [Min(1)] public int barCount;

        // length == barHeight * barCount; each cell is a color index (0..colorCount-1) or Empty.
        public int[] slots = new int[0];

        /// <summary>Flat index of a slot within a bar (slot 0 = bottom).</summary>
        public int SlotIndex(int bar, int slot) => bar * barHeight + slot;
    }
}
