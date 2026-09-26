using System;

namespace ReplicaProjects.MagicSort
{
    /// <summary>
    /// One loaded level: routes taps through the rules into the active theme, and owns the
    /// "level completed" transition. Theme switching is refused once the level is solved.
    /// </summary>
    public sealed class MagicSortSession
    {
        /// <summary>Raised once, by the tap that solves the level.</summary>
        public event Action LevelCompleted;

        private readonly MagicSortOrchestrator _orchestrator = new();
        private readonly IThemeCycle _themes;
        private PresentationCore _presentation;

        public MagicSortSession(IThemeCycle themes) => _themes = themes;

        public bool IsLevelSolved => _orchestrator.IsLevelSolved;
        public bool InputLocked => _presentation == null || _presentation.InputLocked;

        public void Load(int[] slots, int barHeight)
        {
            _presentation?.Teardown();
            _orchestrator.Initialize(slots, barHeight);
            Build();
        }

        public void SwitchTheme()
        {
            if (IsLevelSolved)
                return; // end screen is pending; keep the solved board on screen

            _themes.ActivateNext();          // tears the old theme down
            _orchestrator.ClearSelection();  // board is kept, selection is not
            Build();                         // new theme rebuilds itself from the current board
        }

        public void Tap(int barIndex)
        {
            if (InputLocked)
                return;

            var result = _orchestrator.HandleTap(barIndex);
            _presentation.Handle(in result);

            if (result.LevelSolved)
                LevelCompleted?.Invoke();
        }

        private void Build()
        {
            _presentation = new PresentationCore(_themes.Active);
            _themes.ResetCamera();
            _presentation.Build(_orchestrator.GetBoard(), _orchestrator.Logic);
        }
    }
}
