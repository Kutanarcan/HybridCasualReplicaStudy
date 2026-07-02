
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
        public readonly PlacementOutcome Outcome;
        public readonly int MovedCount;
        public readonly int SourceNewTop;
        public readonly int TargetNewTop;

        public bool Success => Outcome == PlacementOutcome.Moved;

        private AvailablePlacementResult(PlacementOutcome outcome, int moved, int srcTop, int tgtTop)
        { Outcome = outcome; MovedCount = moved; SourceNewTop = srcTop; TargetNewTop = tgtTop; }

        public static AvailablePlacementResult Moved(int moved, int srcTop, int tgtTop)
        => new(PlacementOutcome.Moved, moved, srcTop, tgtTop);
        public static AvailablePlacementResult Rejected(PlacementOutcome reason)
            => new(reason, 0, -1, -1);
    }
}
