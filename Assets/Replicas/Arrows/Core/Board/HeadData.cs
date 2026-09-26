using System;
using System.Collections.Generic;

namespace ReplicaProjects.Arrows
{
    [Serializable]
    public struct HeadData
    {
        public GridCoord coordinates;
        public Direction direction;

        // Ordered head -> tail, EXCLUDES the head cell. Valid levels require >= 1 entry.
        // null/empty is tolerated for legacy assets.
        public List<LineCell> line;
    }
}
