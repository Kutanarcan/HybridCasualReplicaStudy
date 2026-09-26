namespace ReplicaProjects.MagicSort
{
    public readonly struct AvailablePlacementResult
    {
        public readonly PlacementOutcome outCome;
        public readonly int movedCount;
        public readonly int sourceNewTop;
        public readonly int targetNewTop;

        public bool Success => outCome == PlacementOutcome.Moved;
        // targetNewTop, taşımanın sonunda hedefin üstünde kalan rengi tutar; Moved durumunda bu, taşınan renktir.
        public int MovedColor => targetNewTop;

        private AvailablePlacementResult(PlacementOutcome outcome, int moved, int srcTop, int tgtTop)
        { outCome = outcome; movedCount = moved; sourceNewTop = srcTop; targetNewTop = tgtTop; }

        public static AvailablePlacementResult Moved(int moved, int srcTop, int tgtTop)
            => new(PlacementOutcome.Moved, moved, srcTop, tgtTop);
        public static AvailablePlacementResult Rejected(PlacementOutcome reason)
            => new(reason, 0, ColorSlot.Empty, ColorSlot.Empty);
    }
}
