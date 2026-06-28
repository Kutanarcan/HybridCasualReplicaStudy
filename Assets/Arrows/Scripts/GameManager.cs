using System.Collections.Generic;
using UnityEngine;

namespace ReplicaProjects.Arrows
{
    public class GameManager : MonoBehaviour
    {
        [SerializeField] private LevelData _level;

        private int width, height;

        private readonly BoardController _boardController = new();

        private BoardPresentation _boardPresentation;
        private Selection _selection;
        private void Awake()
        {
            Create();
            Initialize();
            InitializePresentation();
            InitializeSelection();
            AdjustCamera(width, height);
        }

        private void Create()
        {
            var boardPresentationPrefab = ArrowsReplicaAssetDatabase.BoardPresentationPrefab;

            _boardPresentation = Instantiate(boardPresentationPrefab);
        }

        private void Initialize()
        {
            width = _level.width;
            height = _level.height;

            _boardController.Initialize(_level);
        }

        private void InitializePresentation()
        {
            var boardItemDataList = new List<BoardItemData>();
            var board = _boardController.Board;
            var grid = _boardController.Grid;

            foreach (var headIndex in board.dataArrays.headIndexArray)
            {
                boardItemDataList.Add(new BoardItemData()
                {
                    coordinates = grid.IndexToCoordinates(headIndex),
                    direction = board.dataArrays.directionArray[headIndex],
                    type = BoardItemType.Arrow
                });

                int[] lineChunk = board.GetLineChuck(headIndex);

                if (lineChunk == null)
                    continue;

                foreach (var chunkIndex in lineChunk)
                {
                    boardItemDataList.Add(new BoardItemData()
                    {
                        coordinates = grid.IndexToCoordinates(chunkIndex),
                        direction = board.dataArrays.directionArray[chunkIndex],
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

        private void InitializeSelection()
        {
            _selection = gameObject.AddComponent<Selection>();
            _selection.Initialize(Camera.main);
            _selection.OnNodeSelected += OnNodeSelected;
        }

        private void OnNodeSelected(Vector2Int coordinate)
        {
            if (coordinate.x < 0 || coordinate.x >= width ||
                coordinate.y < 0 || coordinate.y >= height)
                return;

            Debug.Log($"Selected {coordinate} full={!_boardController.IsEmpty(coordinate)}");
        }
    }
}
