using System.Collections.Generic;
using ReplicaProjects.Common;
using UnityEngine;

namespace ReplicaProjects.Arrows
{
    /// <summary>Composition root: builds the Core session and controls, wires them to the Unity views.</summary>
    public class ArrowsGameManager : MonoBehaviour, IArrowsView
    {
        [SerializeField] private List<LevelData> _levelDataList;

        private const int MAX_HEALTH = 3;
        private const float NODE_HIT_DIAMETER = 1.1f;
        private const float DRAG_THRESHOLD_PIXELS = 10f;

        private readonly ArrowsSession _session = new();
        private readonly TickGroup _ticks = new();
        private LevelCursor _levels;
        private CameraRig _cameraRig;

        private BoardPresentation _boardPresentation;
        private HealthPresentation _healthPresentation;
        private EndScreenPresentation _endScreenPresentation;
        private CameraController _cameraController;
        private AudioSource _audioSource;
        private AudioClip _selectionSFX;

        private void Awake()
        {
            Application.targetFrameRate = 60;

            Create();
            Wire();
            StartLevel();
        }

        private void Update() => _ticks.Tick(Time.deltaTime);

        private void Create()
        {
            _levels = new LevelCursor(_levelDataList.Count);
            _selectionSFX = ArrowsReplicaAssetDatabase.SFX_Selection;

            _boardPresentation = Instantiate(ArrowsReplicaAssetDatabase.BoardPresentationPrefab);
            _healthPresentation = Instantiate(ArrowsReplicaAssetDatabase.HealthPresentationPrefab);
            _endScreenPresentation = Instantiate(ArrowsReplicaAssetDatabase.EndScreenPresentationPrefab);
            _audioSource = gameObject.AddComponent<AudioSource>();

            var camera = Camera.main;
            var input = new UnityInputSource();
            var viewport = new CameraViewport(camera);

            var tapDetector = new TapDetector(input, viewport, new CellPicker(NODE_HIT_DIAMETER), DRAG_THRESHOLD_PIXELS);
            tapDetector.CellTapped += OnCellTapped;
            _cameraRig = new CameraRig(input, viewport, new CameraRigSettings());

            _ticks.Add(tapDetector);
            _ticks.Add(_cameraRig);

            _cameraController = gameObject.AddComponent<CameraController>();
            _cameraController.Initialize(camera, _cameraRig);
        }

        private void Wire()
        {
            _session.Won += OnWon;
            _session.Lost += OnLost;
            _endScreenPresentation.InteractionButtonPressed += OnEndGameButtonPressed;
        }

        private void StartLevel()
        {
            var level = _levelDataList[_levels.Current];

            _session.Start(level.width, level.height, level.heads, MAX_HEALTH);
            _cameraRig.Frame(level.width, level.height);
            _boardPresentation.Initialize(_session.Board.Heads);
            _healthPresentation.Initialize(_session.Health);
        }

        private void StopLevel()
        {
            _boardPresentation.AnimationFinished -= ShowVictory;
            _cameraController.DeInitialize();
            _boardPresentation.DeInitialize();
            _healthPresentation.DeInitialize();
        }

        private void OnCellTapped(GridCoord cell)
        {
            _audioSource.PlayOneShot(_selectionSFX);
            _session.Tap(cell).ApplyTo(this);
        }

        // Victory waits for the last arrow's exit animation.
        private void OnWon()
        {
            _levels.Advance();
            _boardPresentation.AnimationFinished += ShowVictory;
        }

        private void OnLost() => _endScreenPresentation.SetState(false);

        private void ShowVictory() => _endScreenPresentation.SetState(true);

        private void OnEndGameButtonPressed()
        {
            StopLevel();
            StartLevel();
        }

        void IArrowsView.ShowRemoved(GridCoord head) => _boardPresentation.EmptyArrow(head);

        void IArrowsView.ShowBlocked(GridCoord head, GridCoord blocker, int remainingHealth)
        {
            _boardPresentation.BumpArrow(head, blocker);
            _cameraController.Shake();
            _healthPresentation.SetHealthAmount(remainingHealth);
        }
    }
}
