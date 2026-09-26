using NUnit.Framework;

namespace ReplicaProjects.MagicSort.Tests
{
    public class MagicSortSolverTests
    {
        private const int E = ColorSlot.Empty;

        [Test]
        public void Solve_AlreadySolved_ZeroMoves()
        {
            var result = MagicSortSolver.Solve(new[] { 0, 0, 1, 1, E, E }, 2);

            Assert.AreEqual(SolveStatus.Solved, result.Status);
            Assert.AreEqual(0, result.MinMoves);
        }

        [Test]
        public void Solve_TwoMovePuzzle_FindsMinimum()
        {
            // [0,1] [1,E] [0,E]: 0->1 then 0->2.
            var result = MagicSortSolver.Solve(new[] { 0, 1, 1, E, 0, E }, 2);

            Assert.AreEqual(SolveStatus.Solvable, result.Status);
            Assert.AreEqual(2, result.MinMoves);
        }

        [Test]
        public void Solve_NoFreeSpace_Unsolvable()
        {
            // Two full, interleaved bars and nowhere to move.
            var result = MagicSortSolver.Solve(new[] { 0, 1, 1, 0 }, 2);

            Assert.AreEqual(SolveStatus.Unsolvable, result.Status);
        }
    }
}
