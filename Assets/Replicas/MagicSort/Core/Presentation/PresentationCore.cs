using System.Collections.Generic;

namespace ReplicaProjects.MagicSort
{
    /// <summary>
    /// MonoBehaviour değildir. TapResult -> tema komutu çevirisi, input kilidi
    /// politikası (tek karar noktası) ve domain görünümünden BarVisualData üretimi.
    /// </summary>
    public sealed class PresentationCore
    {
        private readonly ISortTheme _theme;
        private bool _inputLocked;
        private int _barHeight;

        public PresentationCore(ISortTheme theme) => _theme = theme;

        public bool InputLocked => _inputLocked;

        public void Build(in SequentialBarArrayInput board, MagicSortLogic logic)
        {
            _barHeight = board.BarHeight;

            var bars = new List<BarVisualData>(board.BarCount);
            for (int b = 0; b < board.BarCount; b++)
            {
                var colors = new int[board.BarHeight];
                board.Bar(b).CopyTo(colors);
                bars.Add(new BarVisualData(colors, logic.IsBarSolved(in board, b)));
            }

            _theme.Build(bars);
            Lock();
            _theme.PlayIntro(Unlock);
        }

        public void Teardown() => _theme.Teardown();

        public void Handle(in TapResult result)
        {
            switch (result.Kind)
            {
                case TapKind.SourceSelected:
                    _theme.ShowSelected(result.SourceBar);
                    break;

                case TapKind.Deselected:
                    _theme.ShowDeselected(result.SourceBar);
                    break;

                case TapKind.Retargeted:
                    _theme.ShowDeselected(result.SourceBar);
                    _theme.ShowSelected(result.NewSource);
                    break;

                case TapKind.Consumed:
                    PlayTransport(in result);
                    break;
            }
        }

        // Allocates one closure per move (captures the command). Measured in MagicSortSessionTests;
        // per-tap, not per-frame, and dwarfed by the theme's own tween allocations.
        private void PlayTransport(in TapResult result)
        {
            var cmd = new TransportData(
                result.SourceBar, result.TargetBar,
                result.TransportResult.MovedColor, result.TransportResult.movedCount,
                _barHeight, result.TargetBarSolved);

            _theme.PlayTransport(in cmd, () =>
            {
                if (cmd.TargetSolved)
                    _theme.ShowSolved(cmd.TargetBar, animated: true);
            });
        }

        private void Lock() => _inputLocked = true;
        private void Unlock() => _inputLocked = false;
    }
}
