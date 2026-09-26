namespace ReplicaProjects.MagicSort
{
    public sealed class BarVisualData
    {
        public readonly int[] Colors;    // index 0 = alt slot; ColorSlot.Empty = boş
        public readonly bool IsSolved;   // tema geçişinde anlık "çözülmüş" pozu için

        public BarVisualData(int[] colors, bool isSolved)
        {
            Colors = colors;
            IsSolved = isSolved;
        }
    }
}
