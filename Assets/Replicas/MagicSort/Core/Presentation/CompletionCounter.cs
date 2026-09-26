using System;

namespace ReplicaProjects.MagicSort
{
    /// <summary>Calls <c>onAllDone</c> once, after <c>Signal</c> has been called <c>count</c> times (immediately when count is 0).</summary>
    public sealed class CompletionCounter
    {
        private readonly Action _onAllDone;
        private int _remaining;

        public CompletionCounter(int count, Action onAllDone)
        {
            _onAllDone = onAllDone;
            _remaining = count;

            if (_remaining <= 0)
                _onAllDone();
        }

        public void Signal()
        {
            if (_remaining <= 0)
                return;

            _remaining--;
            if (_remaining == 0)
                _onAllDone();
        }
    }
}
