using System.Collections.Generic;
using UnityEngine;

namespace ReplicaProjects.Arrows
{
    [System.Serializable]
    public struct HeadData
    {
        public Vector2Int coordinates;
        public Direction direction;

        // Ordered head -> tail, EXCLUDES the head cell. Valid levels require >= 1 entry.
        // null/empty is tolerated for legacy assets.
        public List<Vector2Int> line;
    }

    [CreateAssetMenu(menuName = "Arrows/Level Data")]
    public class LevelData : ScriptableObject
    {
        public int width = 5;
        public int height = 5;

        // Sparse: only occupied cells are stored.
        public List<HeadData> heads = new();
    }
}
