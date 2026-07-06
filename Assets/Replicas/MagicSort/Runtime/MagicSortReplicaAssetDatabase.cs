using ReplicaProjects.Common;
using UnityEngine;

namespace ReplicaProjects.MagicSort
{
    public static class MagicSortReplicaAssetDatabase
    {
        // Yeni tema eklemek = buraya bir satır + Resources/Themes/ altına, IMagicSortTheme
        // implemente eden bir bileşen taşıyan prefab
        public static GameObject[] ThemePrefabs =
        {
            Resources.Load<GameObject>("Themes/BoltThemeRoot"),
        };

        public static EndScreenPresentation EndScreenPresentationPrefab = Resources.Load<EndScreenPresentation>("EndScreenPresentation");
        public static InGameUI InGameUIPrefab = Resources.Load<InGameUI>("InGameUI");
    }
}
