using ReplicaProjects.Common;
using UnityEngine;

namespace ReplicaProjects.MagicSort
{
    public static class MagicSortReplicaAssetDatabase
    {
        public static MagicSortPresentation MagicSortPresentationPrefab = Resources.Load<MagicSortPresentation>("MagicSortPresentation");
        public static EndScreenPresentation EndScreenPresentationPrefab = Resources.Load<EndScreenPresentation>("EndScreenPresentation");
        public static InGameUI InGameUIPrefab = Resources.Load<InGameUI>("InGameUI");
    }
}
