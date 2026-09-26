using System;
using System.Collections.Generic;

namespace ReplicaProjects.MagicSort.Tests
{
    /// <summary>
    /// Hand-written fake theme. Records calls; intro and transport complete only when the test says
    /// so, which is how the real tweens behave (asynchronously).
    /// </summary>
    public sealed class FakeSortTheme : ISortTheme
    {
        public event Action<int> BarTapped;

        public readonly List<string> Calls = new();
        public int BuiltBarCount = -1;
        public bool IsBuilt;

        /// <summary>Turn off for allocation tests: building the call strings allocates.</summary>
        public bool Record = true;

        private Action _pendingIntro;
        private Action _pendingTransport;

        public void Build(IReadOnlyList<BarVisualData> bars)
        {
            BuiltBarCount = bars.Count;
            IsBuilt = true;
            if (Record) Calls.Add("Build");
        }

        public void Teardown()
        {
            IsBuilt = false;
            if (Record) Calls.Add("Teardown");
        }

        public void PlayIntro(Action onComplete) => _pendingIntro = onComplete;

        public void ShowSelected(int bar)
        {
            if (Record) Calls.Add($"Selected {bar}");
        }

        public void ShowDeselected(int bar)
        {
            if (Record) Calls.Add($"Deselected {bar}");
        }

        public void PlayTransport(in TransportData command, Action onComplete)
        {
            if (Record) Calls.Add($"Transport {command.SourceBar}->{command.TargetBar} x{command.MovedCount}");
            _pendingTransport = onComplete;
        }

        public void ShowSolved(int bar, bool animated)
        {
            if (Record) Calls.Add($"Solved {bar}");
        }

        public void FinishIntro() => _pendingIntro?.Invoke();
        public void FinishTransport() => _pendingTransport?.Invoke();
        public void Tap(int bar) => BarTapped?.Invoke(bar);
    }
}
