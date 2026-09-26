using DG.Tweening;
using ReplicaProjects.Common;
using System.Collections.Generic;
using UnityEngine;

namespace ReplicaProjects.MagicSort
{
    /// <summary>Composition root: builds the themes and the Core session, wires UI buttons and level flow.</summary>
    public class MagicSortGameManager : MonoBehaviour
    {
        [SerializeField] private List<MagicSortLevel> _LevelList;

        private const float END_SCREEN_DELAY = 2f;

        private EndScreenPresentation _endScreenPresentation;
        private InGameUI _inGameUI;
        private SortThemeManager _themes;
        private MagicSortSession _session;
        private LevelCursor _levels;
        private Tween _endScreenDelay;

        private void Awake()
        {
            Application.targetFrameRate = 60;
            Create();
            Wire();
            LoadCurrentLevel();
        }

        private void Create()
        {
            _levels = new LevelCursor(_LevelList.Count);
            _themes = new SortThemeManager(MagicSortReplicaAssetDatabase.ThemePrefabs);
            _session = new MagicSortSession(_themes);
            _endScreenPresentation = Instantiate(MagicSortReplicaAssetDatabase.EndScreenPresentationPrefab);
            _inGameUI = Instantiate(MagicSortReplicaAssetDatabase.InGameUIPrefab);
        }

        private void Wire()
        {
            _themes.BarTapped += _session.Tap;
            _session.LevelCompleted += ScheduleEndScreen;
            _endScreenPresentation.InteractionButtonPressed += OnNextLevelPressed;
            _inGameUI.RestartLevelButtonPressed += LoadCurrentLevel;
            _inGameUI.SwitchThemePressed += _session.SwitchTheme;
        }

        private void OnDestroy()
        {
            _endScreenDelay?.Kill();
        }

        private void LoadCurrentLevel()
        {
            // A pending end screen belongs to the level being replaced.
            _endScreenDelay?.Kill();
            _endScreenDelay = null;

            _inGameUI.Show();
            _endScreenPresentation.Hide();

            var level = _LevelList[_levels.Current];
            _session.Load(level.slots, level.barHeight);
        }

        private void OnNextLevelPressed()
        {
            _levels.Advance();
            LoadCurrentLevel();
        }

        private void ScheduleEndScreen() =>
            _endScreenDelay = DOVirtual.DelayedCall(END_SCREEN_DELAY, ShowEndScreen);

        private void ShowEndScreen()
        {
            _inGameUI.Hide();
            _endScreenPresentation.SetState(true);
        }
    }
}
