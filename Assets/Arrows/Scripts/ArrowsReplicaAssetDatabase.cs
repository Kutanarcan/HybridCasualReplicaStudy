using UnityEngine;
using ReplicaProjects.Common;

namespace ReplicaProjects.Arrows
{
    public static class ArrowsReplicaAssetDatabase
    {
        public static BoardPresentation BoardPresentationPrefab = Resources.Load<BoardPresentation>("BoardPresentation");
        public static HealthPresentation HealthPresentationPrefab = Resources.Load<HealthPresentation>("HealthPresentation");
        public static EndScreenPresentation EndScreenPresentationPrefab = Resources.Load<EndScreenPresentation>("EndScreenPresentation");
        public static AudioClip SFX_Selection = Resources.Load<AudioClip>("SFX_Selection1");
    }
}
