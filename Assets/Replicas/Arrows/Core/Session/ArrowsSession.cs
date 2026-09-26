using System;
using System.Collections.Generic;

namespace ReplicaProjects.Arrows
{
    /// <summary>
    /// One play-through of a level: tap rules, health and the win/lose outcome.
    /// Once finished, taps are ignored until the next Start.
    /// </summary>
    public sealed class ArrowsSession
    {
        public event Action Won;
        public event Action Lost;

        private readonly BoardController _board = new();
        private readonly HealthOrchestrator _health = new();

        public BoardController Board => _board;
        public int Health => _health.CurrentHealth;
        public SessionState State { get; private set; }

        public void Start(int width, int height, IReadOnlyList<HeadData> heads, int maxHealth)
        {
            _board.DeInitialize();
            _board.Initialize(width, height, heads);
            _health.Initialize(maxHealth);
            State = SessionState.Playing;
        }

        public ArrowTapResult Tap(GridCoord cell)
        {
            if (State != SessionState.Playing || !_board.IsInBounds(cell) || _board.IsEmpty(cell))
                return ArrowTapResult.Ignored();

            // A tapped cell may be a head OR any of its line cells; both resolve to the same head.
            var head = _board.GetHeadCoordinate(cell);

            return _board.IsPathClear(cell) ? Remove(cell, head) : Bump(cell, head);
        }

        private ArrowTapResult Remove(GridCoord cell, GridCoord head)
        {
            _board.RemoveAtCoordinate(cell);

            if (_board.RemainingArrows == 0)
                Finish(SessionState.Won, Won);

            return ArrowTapResult.Removed(head);
        }

        private ArrowTapResult Bump(GridCoord cell, GridCoord head)
        {
            var blocker = _board.GetForwardBlocker(cell);
            _health.DecreaseHealth();

            if (_health.IsDead)
                Finish(SessionState.Lost, Lost);

            return ArrowTapResult.Blocked(head, blocker, _health.CurrentHealth);
        }

        private void Finish(SessionState state, Action outcome)
        {
            State = state;
            outcome?.Invoke();
        }
    }
}
