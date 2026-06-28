using UnityEngine;

namespace ReplicaProjects.Arrows
{
    public static class ArrowsReplicaAssetDatabase
    {
        public static BoardPresentation BoardPresentationPrefab = Resources.Load<BoardPresentation>("BoardPresentation");
        public static HealthPresentation HealthPresentationPrefab = Resources.Load<HealthPresentation>("HealthPresentation");

    }
}
