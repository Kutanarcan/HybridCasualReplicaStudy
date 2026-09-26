using NUnit.Framework;
using ReplicaProjects.Common.TestSupport;

namespace ReplicaProjects.MagicSort.Tests
{
    public class MagicSortSessionTests
    {
        private const int E = ColorSlot.Empty;
        private const int BarHeight = 2;

        // Two moves solve it: 0->1, then 0->2.
        private static readonly int[] TwoMoves = { 0, 1, 1, E, 0, E };

        private FakeThemeCycle _themes;
        private MagicSortSession _session;
        private int _completed;

        [SetUp]
        public void SetUp()
        {
            _themes = new FakeThemeCycle();
            _session = new MagicSortSession(_themes);
            _completed = 0;
            _session.LevelCompleted += () => _completed++;

            _session.Load(TwoMoves, BarHeight);
            _themes.ActiveFake.FinishIntro();
        }

        private void Move(int from, int to)
        {
            _session.Tap(from);
            _session.Tap(to);
        }

        [Test]
        public void Load_BuildsActiveThemeAfterCameraReset()
        {
            Assert.AreEqual(3, _themes.ActiveFake.BuiltBarCount);
            Assert.AreEqual(1, _themes.CameraResets);
        }

        [Test]
        public void Tap_WhileIntroPlaying_Ignored()
        {
            _session.Load(TwoMoves, BarHeight); // new intro, not finished

            _session.Tap(0);

            CollectionAssert.DoesNotContain(_themes.ActiveFake.Calls, "Selected 0");
        }

        [Test]
        public void Tap_ValidMove_PlaysTransportAndSolvedAfterIt()
        {
            Move(0, 1);
            CollectionAssert.Contains(_themes.ActiveFake.Calls, "Transport 0->1 x1");
            CollectionAssert.DoesNotContain(_themes.ActiveFake.Calls, "Solved 1");

            _themes.ActiveFake.FinishTransport();
            CollectionAssert.Contains(_themes.ActiveFake.Calls, "Solved 1");
        }

        [Test]
        public void Tap_SolvingMove_RaisesLevelCompletedOnce()
        {
            Move(0, 1);
            Move(0, 2);
            _session.Tap(1);
            _session.Tap(2);

            Assert.AreEqual(1, _completed);
            Assert.IsTrue(_session.IsLevelSolved);
        }

        [Test]
        public void SwitchTheme_WhenSolved_DoesNothing()
        {
            Move(0, 1);
            Move(0, 2);

            _session.SwitchTheme();

            Assert.AreEqual(0, _themes.ActiveIndex);
            Assert.IsTrue(_themes.Themes[0].IsBuilt);
        }

        [Test]
        public void SwitchTheme_MidLevel_RebuildsNextThemeFromCurrentBoard()
        {
            Move(0, 1);

            _session.SwitchTheme();

            Assert.AreEqual(1, _themes.ActiveIndex);
            Assert.IsFalse(_themes.Themes[0].IsBuilt);
            Assert.IsTrue(_themes.Themes[1].IsBuilt);
            Assert.AreEqual(2, _themes.CameraResets);
        }

        [Test]
        public void SwitchTheme_ClearsSelection()
        {
            _themes.ActiveFake.FinishIntro();
            _session.Tap(0); // select

            _session.SwitchTheme();
            _themes.ActiveFake.FinishIntro();
            _session.Tap(1); // must be a fresh selection, not a pour from bar 0

            CollectionAssert.Contains(_themes.ActiveFake.Calls, "Selected 1");
        }

        [Test]
        public void Load_Again_TearsDownPreviousBuildAndResetsSolved()
        {
            Move(0, 1);
            Move(0, 2);

            _session.Load(TwoMoves, BarHeight);

            Assert.IsFalse(_session.IsLevelSolved);
            CollectionAssert.AreEqual(new[] { "Build", "Teardown", "Build" },
                _themes.ActiveFake.Calls.FindAll(c => c == "Build" || c == "Teardown"));
        }

        // Selecting a bar is the most frequent tap; it must not touch the GC.
        [Test]
        public void Tap_Select_DoesNotAllocate()
        {
            _themes.ActiveFake.Record = false; // the fake's own logging must not be measured

            AllocationAssert.NoSteadyStateAllocations(() =>
            {
                _session.Tap(0); // select
                _session.Tap(0); // deselect
            });
        }

        // A move allocates: PresentationCore captures the transport command in its completion closure.
        // Measured, not asserted to zero — see PresentationCore.PlayTransport.
        [Test]
        public void Tap_Move_AllocationIsMeasured()
        {
            // One fresh session per call (1 warm-up + 3 measured) so every call is a real move.
            var sessions = new MagicSortSession[4];
            for (int i = 0; i < sessions.Length; i++)
            {
                var themes = new FakeThemeCycle();
                sessions[i] = new MagicSortSession(themes);
                sessions[i].Load(TwoMoves, BarHeight);
                themes.ActiveFake.FinishIntro();
                themes.ActiveFake.Record = false; // the fake's own logging must not be measured
            }

            int next = 0;
            int allocations = GcAllocCounter.SteadyState(() =>
            {
                var session = sessions[next++];
                session.Tap(0);
                session.Tap(1);
            });

            TestContext.WriteLine($"[alloc] MagicSortSession move (select + pour) = {allocations} GC.Alloc sample(s)");
            Assert.That(allocations, Is.InRange(1, 2), "one completion closure (+ its delegate) per move");
        }
    }
}
