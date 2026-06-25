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
            AdjustCamera(width, height);
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

        private void AdjustCamera(int width, int height)
        {
            var camera = Camera.main;

            var size = width <= height ? (width + height) * 0.5f : width;
            var cameraPosition = new Vector3(width, height, camera.transform.position.z) * 0.5f - new Vector3(0.5f, 0.5f, 0);

            camera.transform.position = cameraPosition;
            camera.orthographicSize = size;
        }
    }
}
