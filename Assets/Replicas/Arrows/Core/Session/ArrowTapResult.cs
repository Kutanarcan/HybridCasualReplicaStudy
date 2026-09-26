namespace ReplicaProjects.Arrows
{
    public readonly struct ArrowTapResult
    {
        public readonly ArrowTapKind Kind;
        public readonly GridCoord Head;           // Removed / Blocked: the tapped arrow's head
        public readonly GridCoord Blocker;        // Blocked: first occupied cell in front of the head
        public readonly int RemainingHealth;      // Blocked: health after the hit

        private ArrowTapResult(ArrowTapKind kind, GridCoord head, GridCoord blocker, int remainingHealth)
        {
            Kind = kind;
            Head = head;
            Blocker = blocker;
            RemainingHealth = remainingHealth;
        }

        public static ArrowTapResult Ignored() => new(ArrowTapKind.Ignored, default, default, 0);
        public static ArrowTapResult Removed(GridCoord head) => new(ArrowTapKind.Removed, head, default, 0);
        public static ArrowTapResult Blocked(GridCoord head, GridCoord blocker, int remainingHealth)
            => new(ArrowTapKind.Blocked, head, blocker, remainingHealth);

        public void ApplyTo(IArrowsView view)
        {
            switch (Kind)
            {
                case ArrowTapKind.Removed:
                    view.ShowRemoved(Head);
                    break;

                case ArrowTapKind.Blocked:
                    view.ShowBlocked(Head, Blocker, RemainingHealth);
                    break;
            }
        }
    }
}
