using UnityEngine;

namespace ReplicaProjects.Arrows
{
    public class GameManager : MonoBehaviour
    {
        public int width, height;

        private GridPresentation _gridPresentation;

        private void Awake()
        {
            Create();
            Initialize();
        }

        private void Create()
        {
            var gridPresentationPrefab = ArrowsReplicaAssetDatabase.GridPresentationPrefab;

            _gridPresentation = Instantiate(gridPresentationPrefab);
        }

        private void Initialize()
        {
            _gridPresentation.Initialize(width, height);
        }
    }
}
