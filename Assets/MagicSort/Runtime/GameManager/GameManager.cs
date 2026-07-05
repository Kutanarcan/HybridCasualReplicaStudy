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
        private MagicSortPresentation _presentation;
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
            var presentationPrefab = MagicSortReplicaAssetDatabase.MagicSortPresentationPrefab;
            var endScreenPresentationPrefab = MagicSortReplicaAssetDatabase.EndScreenPresentationPrefab;
            var _inGameUIPrefab = MagicSortReplicaAssetDatabase.InGameUIPrefab;

            _presentation = Instantiate(presentationPrefab);
            _endScreenPresentation = Instantiate(endScreenPresentationPrefab);
            _inGameUI = Instantiate(_inGameUIPrefab);
        }

        private void Initialize()
        {
            _presentation.AnyBarViewClicked += OnAnyBarViewClicked;

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

            DOVirtual.DelayedCall(1.5f, () =>
            {
                _inGameUI.Hide();
                _endScreenPresentation.SetState(true);
            });
        }

        private void ReloadLevel(int index)
        {
            _endScreenPresentation.InteractionButtonPressed -= OnEndGameButtonPressed;
            _inGameUI.InteractionButtonPressed -= OnRestartLevelButtonClicked;

            _presentation.DeInitialize();
            LoadLevel(index);
        }

        private void OnAnyBarViewClicked(int barIndex)
        {
            TapResult result = _orchestrator.HandleTap(barIndex);

            _isSolvedLevel = result.LevelSolved;

            _presentation.HandleTapResponse(result);

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

                barDataList.Add(data);
            }

            _presentation.Initialize(barDataList);
        }
    }
}
