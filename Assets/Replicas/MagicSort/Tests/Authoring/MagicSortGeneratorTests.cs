using System.Collections.Generic;
using NUnit.Framework;
using ReplicaProjects.Common;

namespace ReplicaProjects.MagicSort.Tests
{
    public class MagicSortGeneratorTests
    {
        private const int BarHeight = 4;

        [Test]
        public void CreateSolvedLevel_OneColorPerBarPlusEmptySpares()
        {
            var bars = MagicSortGenerator.CreateSolvedLevel(colorCount: 3, BarHeight, emptyBarCount: 2);

            Assert.AreEqual(5, bars.Count);
            for (int i = 0; i < 3; i++)
                Assert.IsTrue(bars[i].IsComplete && bars[i].Top == i);
            Assert.IsTrue(bars[3].IsEmpty && bars[4].IsEmpty);
        }

        [Test]
        public void Generate_SameSeed_SameLevel()
        {
            var a = Flatten(new MagicSortGenerator(new SeededRandomSource(99)).Generate(4, BarHeight, 2, 1, 1, out _));
            var b = Flatten(new MagicSortGenerator(new SeededRandomSource(99)).Generate(4, BarHeight, 2, 1, 1, out _));

            CollectionAssert.AreEqual(a, b);
        }

        [TestCase(0, 0)]
        [TestCase(1, 1)]
        [TestCase(2, 2)]
        public void Generate_EnsureSolvable_ReturnsSolvableLayout(int depth, int frag)
        {
            var generator = new MagicSortGenerator(new SeededRandomSource(1234 + depth));

            var bars = generator.Generate(4, BarHeight, 2, depth, frag, out var result);

            Assert.That(result.Status, Is.EqualTo(SolveStatus.Solvable).Or.EqualTo(SolveStatus.Solved));
            Assert.AreEqual(result.Status, MagicSortSolver.Solve(Flatten(bars), BarHeight).Status);
        }

        [Test]
        public void Generate_KeepsEveryBallAndEmptiesSpares()
        {
            var bars = new MagicSortGenerator(new SeededRandomSource(5)).Generate(4, BarHeight, 2, 2, 2, out _);

            var counts = new int[4];
            foreach (var bar in bars)
                for (int s = 0; s < bar.Count; s++)
                    counts[bar[s]]++;

            CollectionAssert.AreEqual(new[] { BarHeight, BarHeight, BarHeight, BarHeight }, counts);
            Assert.IsTrue(bars[4].IsEmpty && bars[5].IsEmpty);
        }

        private static int[] Flatten(IReadOnlyList<Bar> bars) => MagicSortFlat.Flatten(bars, BarHeight);
    }
}
