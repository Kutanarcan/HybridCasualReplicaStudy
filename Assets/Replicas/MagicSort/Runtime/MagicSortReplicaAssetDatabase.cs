using ReplicaProjects.Common;
using UnityEngine;

namespace ReplicaProjects.MagicSort
{
    public static class MagicSortReplicaAssetDatabase
    {
        // Yeni tema eklemek = buraya bir satır + Resources/Themes/ altına prefab
        public static MagicSortThemeRoot[] ThemePrefabs =
        {
            Resources.Load<MagicSortThemeRoot>("Themes/BoltThemeRoot"),
        };

        public static EndScreenPresentation EndScreenPresentationPrefab = Resources.Load<EndScreenPresentation>("EndScreenPresentation");
        public static InGameUI InGameUIPrefab = Resources.Load<InGameUI>("InGameUI");
    }
}
