using System.Collections.Generic;

namespace ReplicaProjects.Arrows.Tests
{
    /// <summary>Hand-written fake: records what the session asked the view to show.</summary>
    public sealed class RecordingArrowsView : IArrowsView
    {
        public readonly List<GridCoord> Removed = new();
        public readonly List<(GridCoord head, GridCoord blocker, int health)> Blocked = new();

        public void ShowRemoved(GridCoord head) => Removed.Add(head);

        public void ShowBlocked(GridCoord head, GridCoord blocker, int remainingHealth) =>
            Blocked.Add((head, blocker, remainingHealth));
    }
}
