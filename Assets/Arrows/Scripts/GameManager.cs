using System.Collections.Generic;
using UnityEngine;

namespace ReplicaProjects.Arrows
{
    public class GameManager : MonoBehaviour
    {
        public int width, height;

        private readonly GridOrchestrator _gridOrchestrator = new();
        private readonly BoardOrchestrator _boardOrchestrator = new();

        private GridPresentation _gridPresentation;
        private BoardPresentation _boardPresentation;

        private void Awake()
        {
            Create();
            Initialize();
            InitializePresentation();
            AdjustCamera(width, height);
        }

        private void Create()
        {
            var gridPresentationPrefab = ArrowsReplicaAssetDatabase.GridPresentationPrefab;
            var boardPresentationPrefab = ArrowsReplicaAssetDatabase.BoardPresentationPrefab;

            _gridPresentation = Instantiate(gridPresentationPrefab);
            _boardPresentation = Instantiate(boardPresentationPrefab);
        }

        private void Initialize()
        {
            _gridOrchestrator.Initialize(new GridOrchestratorModel()
            {
                width = width,
                height = height,
            });

            _boardOrchestrator.Initialize(_gridOrchestrator, 5);
        }

        private void InitializePresentation()
        {
            _gridPresentation.Initialize(width, height);

            var boardItemDataList = new List<BoardItemData>();

            foreach (var headIndex in _boardOrchestrator.dataArrays.headIndexArray)
            {
                boardItemDataList.Add(new BoardItemData()
                {
                    coordinates = _gridOrchestrator.IndexToCoordinates(headIndex),
                    direction = _boardOrchestrator.dataArrays.directionArray[headIndex],
                    type = BoardItemType.Arrow
                });

                int[] lineChunk = _boardOrchestrator.GetLineChuck(headIndex);

                if (lineChunk == null)
                    continue;

                foreach (var chunkIndex in lineChunk)
                {
                    boardItemDataList.Add(new BoardItemData()
                    {
                        coordinates = _gridOrchestrator.IndexToCoordinates(chunkIndex),
                        direction = _boardOrchestrator.dataArrays.directionArray[chunkIndex],
                        type = BoardItemType.Line
                    });
                }
            }

            _boardPresentation.Initialize(boardItemDataList);
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
