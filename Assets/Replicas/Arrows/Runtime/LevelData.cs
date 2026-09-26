using System.Collections.Generic;
using UnityEngine;

namespace ReplicaProjects.Arrows
{
    [CreateAssetMenu(menuName = "Arrows/Level Data")]
    public class LevelData : ScriptableObject
    {
        public int width = 5;
        public int height = 5;

        // Sparse: only occupied cells are stored.
        public List<HeadData> heads = new();
    }
}
