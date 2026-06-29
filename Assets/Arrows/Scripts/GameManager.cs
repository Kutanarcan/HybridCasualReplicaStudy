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
        private CameraController _cameraController;

        private const int MAX_HEALTH = 3;
        private bool isFinished = false;

        private void Awake()
        {
            Create();
            Initialize();
            InitializePresentation();
        }

        private void Create()
        {
            var boardPresentationPrefab = ArrowsReplicaAssetDatabase.BoardPresentationPrefab;
            var healthPresentationPrefab = ArrowsReplicaAssetDatabase.HealthPresentationPrefab;
            var endScreenPresentationPrefab = ArrowsReplicaAssetDatabase.EndScreenPresentationPrefab;

            _boardPresentation = Instantiate(boardPresentationPrefab);
            _healthPresentation = Instantiate(healthPresentationPrefab);
            _endScreenPresentation = Instantiate(endScreenPresentationPrefab);

            _cameraController = gameObject.AddComponent<CameraController>();

            _selection = gameObject.AddComponent<Selection>();
            _selection.Initialize(Camera.main);
        }

        private void Initialize()
        {
            width = _level.width;
            height = _level.height;

            _boardController.Initialize(_level);
            _healthOrchestrator.Initialize(MAX_HEALTH);
            _cameraController.Initialize(Camera.main, width, height);
            _healthOrchestrator.Died += OnFinishedWithDefeat;
            _boardPresentation.AnimationFinished += OnAnyArrowHeadAnimationFinished;
            _boardController.Board.NoHeadLeft += OnFinishedWithVictory;
            _endScreenPresentation.InteractionButtonPressed += OnEndGameButtonPressed;

            _selection.OnNodeSelected += OnNodeSelected;

        }


        private void DeInitialize()
        {
            _selection.OnNodeSelected -= OnNodeSelected;

            _healthOrchestrator.Died -= OnFinishedWithDefeat;
            _boardController.Board.NoHeadLeft -= OnFinishedWithVictory;
            _endScreenPresentation.InteractionButtonPressed -= OnEndGameButtonPressed;
            _boardPresentation.AnimationFinished -= OnAnyArrowHeadAnimationFinished;

            _boardController.DeInitialize();
            _healthOrchestrator.DeInitialize();
            _cameraController.DeInitialize();
            _boardPresentation.DeInitialize();
            _healthPresentation.DeInitialize();
        }

        private void OnEndGameButtonPressed()
        {
            DeInitialize();
            Initialize();
            InitializePresentation();
            isFinished = false;
        }

        private void OnFinishedWithDefeat()
        {
            isFinished = true;
            _endScreenPresentation.SetState(false);
        }

        private void OnFinishedWithVictory()
        {
            isFinished = true;
        }

        private void OnAnyArrowHeadAnimationFinished()
        {
            if (!isFinished)
                return;

            _endScreenPresentation.SetState(true);
        }

        private void InitializePresentation()
        {
            var arrowDataList = new List<ArrowPresentationData>();
            var board = _boardController.Board;
            var grid = _boardController.Grid;

            foreach (var headIndex in board.dataArrays.headIndexArray)
            {
                var headCoord = grid.IndexToCoordinates(headIndex);
                var line = new List<LineCell>();

                int[] lineChunk = board.GetLineChuck(headIndex);
                if (lineChunk != null)
                {
                    foreach (var chunkIndex in lineChunk)
                    {
                        line.Add(new LineCell
                        {
                            coordinates = grid.IndexToCoordinates(chunkIndex),
                            direction = board.dataArrays.directionArray[chunkIndex]
                        });
                    }
                }

                arrowDataList.Add(new ArrowPresentationData()
                {
                    headCoordinates = headCoord,
                    headDirection = board.dataArrays.directionArray[headIndex],
                    line = line
                });
            }

            _boardPresentation.Initialize(arrowDataList);
            _healthPresentation.Initialize(_healthOrchestrator.currentHealth);

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

            // A tapped cell may be a head OR any of its line cells; both resolve to the same head.
            var headCoord = _boardController.GetHeadCoordinate(coordinates);

            if (_boardController.IsPathClear(coordinates))
            {
                _boardController.RemoveAtCoordinate(coordinates);
                _boardPresentation.EmptyArrow(headCoord);
            }
            else
            {
                _healthOrchestrator.DecreaseHealth();
                _healthPresentation.SetHealthAmount(_healthOrchestrator.currentHealth);
            }
        }
    }
}
