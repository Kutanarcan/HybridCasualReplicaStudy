using DG.Tweening;
using ReplicaProjects.Common;
using System;
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
        private readonly MagicSortOrchestrator _orchestrator = new();

        private int _currentLevelIndex;
        private bool _isSolvedLevel = false;

        private void Awake()
        {
            Application.targetFrameRate = 60;
            Create();
            Initialize();
        }

        private void Update()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (Input.GetKeyDown(KeyCode.T))
                SwitchTheme();
#endif
        }

        private void Create()
        {
            var endScreenPresentationPrefab = MagicSortReplicaAssetDatabase.EndScreenPresentationPrefab;
            var _inGameUIPrefab = MagicSortReplicaAssetDatabase.InGameUIPrefab;

            _themes = new MagicSortThemeManager(MagicSortReplicaAssetDatabase.ThemePrefabs);
            _endScreenPresentation = Instantiate(endScreenPresentationPrefab);
            _inGameUI = Instantiate(_inGameUIPrefab);
        }

        private void Initialize()
        {
            _themes.AnyBarViewClicked += OnAnyBarViewClicked;

            LoadLevel(_currentLevelIndex);
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
            _inGameUI.InteractionButtonPressed += OnRestartLevelButtonClicked;
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
            _inGameUI.InteractionButtonPressed -= OnRestartLevelButtonClicked;

            _themes.Active.DeInitialize();
            LoadLevel(index);
        }

        private void SwitchTheme()
        {
            if (_isSolvedLevel)
                return; // end screen bekliyor; tema değiştirme

            _themes.ActivateNext();          // eski temayı DeInitialize edip havuzdan sıradakini açar
            _orchestrator.ClearSelection();  // board korunur, seçim temizlenir
            Build(_orchestrator.GetBoard()); // yeni tema mevcut board'dan kendini kurar
        }

        private void OnAnyBarViewClicked(int barIndex)
        {
            TapResult result = _orchestrator.HandleTap(barIndex);

            _isSolvedLevel = result.LevelSolved;

            _themes.Active.HandleTapResponse(result);

            TryToCallEndGame();
        }

        public void Build(in SequentialBarArrayInput board)
        {
            var barDataList = new List<BarVisualData>(board.BarCount);

            for (int b = 0; b < board.BarCount; b++)
            {
                var slots = board.Bar(b);

                var data = new BarVisualData();
                data.colorList = new List<int>(board.barHeight);

                for (int i = 0; i < slots.Length; i++)
                {
                    data.colorList.Add(slots[i]);
                }

                data.isSolved = _orchestrator._magicSortLogic.IsBarSolved(in board, b);

                barDataList.Add(data);
            }

            _themes.ResetCamera();
            _themes.Active.Initialize(barDataList);
        }
    }
}
