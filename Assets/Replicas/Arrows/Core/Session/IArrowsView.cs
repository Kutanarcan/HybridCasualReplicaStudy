namespace ReplicaProjects.Arrows
{
    /// <summary>What the presentation must show for a tap. Implemented by the Unity shell.</summary>
    public interface IArrowsView
    {
        void ShowRemoved(GridCoord head);
        void ShowBlocked(GridCoord head, GridCoord blocker, int remainingHealth);
    }
}
