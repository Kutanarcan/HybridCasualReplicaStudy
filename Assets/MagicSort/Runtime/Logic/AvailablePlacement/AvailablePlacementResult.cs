
namespace ReplicaProjects.MagicSort
{
    // This enum will use for Presentation Later
    public enum PlacementOutcome
    {
        Moved,
        SameBar,
        TargetFull,
        ColorMismatch
    }
    public readonly struct AvailablePlacementResult
    {
        public readonly PlacementOutcome putcome;
        public readonly int movedCount;
        public readonly int sourceNewTop;
        public readonly int targetNewTop;

        public bool Success => putcome == PlacementOutcome.Moved;

        private AvailablePlacementResult(PlacementOutcome outcome, int moved, int srcTop, int tgtTop)
        { putcome = outcome; movedCount = moved; sourceNewTop = srcTop; targetNewTop = tgtTop; }

        public static AvailablePlacementResult Moved(int moved, int srcTop, int tgtTop)
        => new(PlacementOutcome.Moved, moved, srcTop, tgtTop);
        public static AvailablePlacementResult Rejected(PlacementOutcome reason)
            => new(reason, 0, -1, -1);
    }
}
