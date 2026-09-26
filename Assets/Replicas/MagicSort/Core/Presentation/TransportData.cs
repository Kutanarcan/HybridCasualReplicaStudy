namespace ReplicaProjects.MagicSort
{
    public readonly struct TransportData
    {
        public readonly int SourceBar;
        public readonly int TargetBar;
        public readonly int ColorIndex;     // taşınan renk
        public readonly int MovedCount;     // kaç birim taşındı
        public readonly int BarHeight;      // oran hesabı için (sıvı: MovedCount/BarHeight)
        public readonly bool TargetSolved;  // taşıma sonunda hedef tamamlandı mı

        public TransportData(int source, int target, int colorIndex,
                           int movedCount, int barHeight, bool targetSolved)
        {
            SourceBar = source; TargetBar = target; ColorIndex = colorIndex;
            MovedCount = movedCount; BarHeight = barHeight; TargetSolved = targetSolved;
        }
    }
}
