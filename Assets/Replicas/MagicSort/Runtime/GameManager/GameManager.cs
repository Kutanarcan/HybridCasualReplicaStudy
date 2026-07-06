
using DG.Tweening;
using ReplicaProjects.Common;
using System.Collections.Generic;
using UnityEngine;

namespace ReplicaProjects.MagicSort
{
    public class GameManager : MonoBehaviour
    {
        [SerializeField] private List<MagicSortLevel> _LevelList;

        private EndScreenPresentation _endScreenPresentation;
        private InGameUI _inGameUI;
        private MagicSortThemeManager _themes;
        private PresentationCore _presentation;
        private readonly MagicSortOrchestrator _orchestrator = new();

        private int _currentLevelIndex;
        private bool _isSolvedLevel = false;

        private void Awake()
        {
            Application.targetFrameRate = 60;
            Create();
            Initialize();
        }

        private void Create()
        {
            var endScreenPresentationPrefab = MagicSortReplicaAssetDatabase.EndScreenPresentationPrefab;
            var _inGameUIPrefab = MagicSortReplicaAssetDatabase.InGameUIPrefab;

            _themes = new MagicSortThemeManager(MagicSortReplicaAssetDatabase.ThemePrefabs);
            _presentation = new PresentationCore(_themes.Active);
            _endScreenPresentation = Instantiate(endScreenPresentationPrefab);
            _inGameUI = Instantiate(_inGameUIPrefab);
        }

        private void Initialize()
        {
            _themes.BarTapped += OnBarTapped;

            LoadLevel(_currentLevelIndex);
        }

        private void OnSwitchThemeButtonClicked()
        {
            SwitchTheme();
        }

        private void OnRestartLevelButtonClicked()
        {
            ReloadLevel(_currentLevelIndex);
        }

        private void OnEndGameButtonPressed()
        {
            ReloadLevel((_currentLevelIndex + 1) % _LevelList.Count);
        }

        private void LoadLevel(int index)
        {
            _inGameUI.Show();
            _endScreenPresentation.Hide();

            _isSolvedLevel = false;

            _currentLevelIndex = index;
            _orchestrator.Initialize(_LevelList[index]);
            Build(_orchestrator.GetBoard());

            _endScreenPresentation.InteractionButtonPressed += OnEndGameButtonPressed;
            _inGameUI.RestartLevelButtonPressed += OnRestartLevelButtonClicked;
            _inGameUI.RestartSwitchThemePressed += OnSwitchThemeButtonClicked;
        }

        private void TryToCallEndGame()
        {
            if (!_isSolvedLevel)
                return;

            DOVirtual.DelayedCall(2f, () =>
            {
                _inGameUI.Hide();
                _endScreenPresentation.SetState(true);
            });
        }

        private void ReloadLevel(int index)
        {
            _endScreenPresentation.InteractionButtonPressed -= OnEndGameButtonPressed;
            _inGameUI.RestartLevelButtonPressed -= OnRestartLevelButtonClicked;
            _inGameUI.RestartSwitchThemePressed -= OnSwitchThemeButtonClicked;

            _presentation.Teardown();
            LoadLevel(index);
        }

        private void SwitchTheme()
        {
            if (_isSolvedLevel)
                return; // end screen bekliyor; tema değiştirme

            _themes.ActivateNext();          // eski temayı Teardown edip havuzdan sıradakini açar
            _presentation = new PresentationCore(_themes.Active);
            _orchestrator.ClearSelection();  // board korunur, seçim temizlenir
            Build(_orchestrator.GetBoard()); // yeni tema mevcut board'dan kendini kurar
        }

        private void OnBarTapped(int barIndex)
        {
            if (_presentation.InputLocked)
                return;

            var result = _orchestrator.HandleTap(barIndex);

            _isSolvedLevel = result.LevelSolved;

            _presentation.Handle(in result);

            TryToCallEndGame();
        }

        public void Build(in SequentialBarArrayInput board)
        {
            _themes.ResetCamera();
            _presentation.Build(in board, _orchestrator.Logic);
        }
    }
}
