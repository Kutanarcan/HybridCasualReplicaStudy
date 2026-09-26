using NUnit.Framework;
using static ReplicaProjects.Arrows.Tests.TestHeads;
using ReplicaProjects.Common.TestSupport;

namespace ReplicaProjects.Arrows.Tests
{
    public class ArrowsSessionTests
    {
        private const int MaxHealth = 3;

        private ArrowsSession _session;
        private RecordingArrowsView _view;
        private int _won;
        private int _lost;

        // P at (1,2) faces Right into Q at (3,2); Q faces Down with a clear ray.
        private static readonly GridCoord P = new(1, 2);
        private static readonly GridCoord Q = new(3, 2);

        [SetUp]
        public void SetUp()
        {
            _session = new ArrowsSession();
            _view = new RecordingArrowsView();
            _won = _lost = 0;
            _session.Won += () => _won++;
            _session.Lost += () => _lost++;

            _session.Start(5, 5, List(Head(1, 2, Direction.Right, (0, 2)), Head(3, 2, Direction.Down, (3, 3))), MaxHealth);
        }

        [Test]
        public void Tap_ClearPath_RemovesArrow()
        {
            _session.Tap(Q).ApplyTo(_view);

            CollectionAssert.AreEqual(new[] { Q }, _view.Removed);
            Assert.IsTrue(_session.Board.IsEmpty(Q));
        }

        [Test]
        public void Tap_LineCell_RemovesOwningArrow()
        {
            _session.Tap(new GridCoord(3, 3)).ApplyTo(_view);

            CollectionAssert.AreEqual(new[] { Q }, _view.Removed);
        }

        [Test]
        public void Tap_BlockedPath_BumpsAndDecreasesHealth()
        {
            _session.Tap(P).ApplyTo(_view);

            Assert.AreEqual(1, _view.Blocked.Count);
            Assert.AreEqual((P, Q, MaxHealth - 1), _view.Blocked[0]);
            Assert.AreEqual(MaxHealth - 1, _session.Health);
        }

        [TestCase(-1, 0)]
        [TestCase(5, 0)]   // x == width must not wrap to (0,1)
        [TestCase(2, 2)]   // empty cell
        public void Tap_OffBoardOrEmpty_Ignored(int x, int y)
        {
            var result = _session.Tap(new GridCoord(x, y));

            Assert.AreEqual(ArrowTapKind.Ignored, result.Kind);
            Assert.AreEqual(MaxHealth, _session.Health);
        }

        [Test]
        public void Session_BlockedTap_DecreasesHealth_AndDiesAtZero()
        {
            for (int i = 0; i < MaxHealth; i++)
                _session.Tap(P);

            Assert.AreEqual(0, _session.Health);
            Assert.AreEqual(SessionState.Lost, _session.State);
            Assert.AreEqual(1, _lost);
        }

        [Test]
        public void Tap_LastArrowRemoved_Wins()
        {
            _session.Tap(Q);
            _session.Tap(P);

            Assert.AreEqual(SessionState.Won, _session.State);
            Assert.AreEqual(1, _won);
            Assert.AreEqual(0, _lost);
        }

        [Test]
        public void Tap_AfterLost_IgnoredAndNeverWins()
        {
            for (int i = 0; i < MaxHealth; i++)
                _session.Tap(P);

            var result = _session.Tap(Q);

            Assert.AreEqual(ArrowTapKind.Ignored, result.Kind);
            Assert.AreEqual(0, _won);
        }

        [Test]
        public void Start_AfterFinish_ResetsState()
        {
            _session.Tap(Q);
            _session.Tap(P);

            _session.Start(5, 5, List(Head(3, 2, Direction.Down, (3, 3))), MaxHealth);

            Assert.AreEqual(SessionState.Playing, _session.State);
            Assert.AreEqual(MaxHealth, _session.Health);
            Assert.IsFalse(_session.Board.IsEmpty(Q));
        }

        // Per-tap hot path: a tap that neither removes nor finishes must not touch the GC.
        [Test]
        public void Tap_Blocked_DoesNotAllocate()
        {
            _session.Start(5, 5, List(Head(1, 2, Direction.Right, (0, 2)), Head(3, 2, Direction.Down, (3, 3))), 100);
            AllocationAssert.NoSteadyStateAllocations(() => { _session.Tap(P); });
        }
    }
}
