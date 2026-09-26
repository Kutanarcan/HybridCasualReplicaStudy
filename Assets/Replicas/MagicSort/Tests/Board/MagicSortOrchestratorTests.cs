using NUnit.Framework;
using ReplicaProjects.Common.TestSupport;

namespace ReplicaProjects.MagicSort.Tests
{
    public class MagicSortOrchestratorTests
    {
        private const int E = ColorSlot.Empty;
        private const int BarHeight = 2;

        // Bar 0: [0, 1]  Bar 1: [1, E]  Bar 2: [0, E] — two moves solve it: 0->1, then 0->2.
        private static readonly int[] OneMoveFromTwo = { 0, 1, 1, E, 0, E };

        private MagicSortOrchestrator _orchestrator;

        [SetUp]
        public void SetUp()
        {
            _orchestrator = new MagicSortOrchestrator();
            _orchestrator.Initialize(OneMoveFromTwo, BarHeight);
        }

        private TapResult Move(int from, int to)
        {
            _orchestrator.HandleTap(from);
            return _orchestrator.HandleTap(to);
        }

        [Test]
        public void Initialize_CopiesSlots()
        {
            var slots = (int[])OneMoveFromTwo.Clone();
            _orchestrator.Initialize(slots, BarHeight);

            Move(0, 1);

            CollectionAssert.AreEqual(OneMoveFromTwo, slots, "the level's array must not be mutated");
        }

        [Test]
        public void HandleTap_ValidMove_AppliesPour()
        {
            var result = Move(0, 1);

            Assert.AreEqual(TapKind.Consumed, result.Kind);
            Assert.IsTrue(result.TargetBarSolved);
            Assert.IsFalse(result.LevelSolved);
            Assert.AreEqual(E, _orchestrator.GetBoard().Placement(0, 1));
        }

        [Test]
        public void HandleTap_SolvingMove_ReportsLevelSolved()
        {
            Move(0, 1);
            var result = Move(0, 2);

            Assert.IsTrue(result.LevelSolved);
            Assert.IsTrue(_orchestrator.IsLevelSolved);
        }

        [Test]
        public void HandleTap_AfterLevelSolved_KeepsSolvedState()
        {
            Move(0, 1);
            Move(0, 2);

            var after = _orchestrator.HandleTap(1);

            Assert.AreEqual(TapKind.Ignored, after.Kind);
            Assert.IsTrue(_orchestrator.IsLevelSolved, "a later tap must not clear the solved state");
        }

        [Test]
        public void HandleTap_OnSolvedBar_Ignored()
        {
            Move(0, 1);

            Assert.AreEqual(TapKind.Ignored, _orchestrator.HandleTap(1).Kind);
        }

        [Test]
        public void Initialize_ResetsSolvedState()
        {
            Move(0, 1);
            Move(0, 2);

            _orchestrator.Initialize(OneMoveFromTwo, BarHeight);

            Assert.IsFalse(_orchestrator.IsLevelSolved);
        }

        // Per-tap hot path: the rules (span-based board) must not touch the GC.
        [Test]
        public void HandleTap_DoesNotAllocate()
        {
            // One fresh board per call (1 warm-up + 3 measured) so every call is a real pour.
            var boards = new MagicSortOrchestrator[4];
            for (int i = 0; i < boards.Length; i++)
            {
                boards[i] = new MagicSortOrchestrator();
                boards[i].Initialize(OneMoveFromTwo, BarHeight);
            }

            int next = 0;
            AllocationAssert.NoSteadyStateAllocations(() =>
            {
                var board = boards[next++];
                board.HandleTap(0); // select
                board.HandleTap(1); // pour
            });
        }
    }
}
