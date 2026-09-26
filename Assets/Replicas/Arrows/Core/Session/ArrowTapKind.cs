namespace ReplicaProjects.Arrows
{
    public enum ArrowTapKind
    {
        Ignored,  // finished session, off-board or empty cell
        Removed,  // path was clear; the arrow left the board
        Blocked   // path was blocked; the arrow bumped and a life was lost
    }
}
