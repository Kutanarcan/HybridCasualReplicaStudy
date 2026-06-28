using System.Collections.Generic;
using UnityEngine;

namespace ReplicaProjects.Arrows
{
    public class GameManager : MonoBehaviour
    {
        [SerializeField] private LevelData _level;

        private int width, height;

        private readonly BoardController _boardController = new();
        private readonly HealthOrchestrator _healthOrchestrator = new();

        private BoardPresentation _boardPresentation;
        private HealthPresentation _healthPresentation;
        private EndScreenPresentation _endScreenPresentation;

        private Selection _selection;

        private const int MAX_HEALTH = 3;
        private bool isFinished = false;

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
            var healthPresentationPrefab = ArrowsReplicaAssetDatabase.HealthPresentationPrefab;
            var endScreenPresentationPrefab = ArrowsReplicaAssetDatabase.EndScreenPresentationPrefab;

            _boardPresentation = Instantiate(boardPresentationPrefab);
            _healthPresentation = Instantiate(healthPresentationPrefab);
            _endScreenPresentation = Instantiate(endScreenPresentationPrefab);
        }

        private void Initialize()
        {
            width = _level.width;
            height = _level.height;

            _boardController.Initialize(_level);
            _healthOrchestrator.Initialize(MAX_HEALTH);
            _healthOrchestrator.Died += OnFinishedWithDefeat;
            _boardController.Board.NoHeadLeft += OnFinishedWithVictory;
            _endScreenPresentation.InteractionButtonPressed += OnEndGameButtonPressed;
        }


        private void DeInitialize()
        {
            _healthOrchestrator.Died -= OnFinishedWithDefeat;
            _boardController.Board.NoHeadLeft -= OnFinishedWithVictory;
            _endScreenPresentation.InteractionButtonPressed -= OnEndGameButtonPressed;
        }

        private void OnEndGameButtonPressed()
        {
            // TODO Restart Level (Victory-Defeat)
        }

        private void OnFinishedWithDefeat()
        {
            isFinished =true;
            _endScreenPresentation.SetState(false);
        }
        private void OnFinishedWithVictory()
        {
            isFinished =true;
            _endScreenPresentation.SetState(true);
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
            _healthPresentation.Initialize(_healthOrchestrator.currentHealth);

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

        private void OnNodeSelected(Vector2Int coordinates)
        {
            if (isFinished)
                return;

            if (coordinates.x < 0 || coordinates.x >= width ||
                coordinates.y < 0 || coordinates.y >= height)
                return;

            if (_boardController.IsEmpty(coordinates))
                return;

            if (_boardController.IsPathClear(coordinates))
            {
                _boardController.RemoveAtCoordinate(coordinates);
                _boardPresentation.EmptyAtCoordinate(coordinates);
            }
            else
            {
                _healthOrchestrator.DecreaseHealth();
                _healthPresentation.SetHealthAmount(_healthOrchestrator.currentHealth);
            }
        }
    }
}
