using NUnit.Framework;

namespace ReplicaProjects.MagicSort.Tests
{
    public class MagicSortFlatTests
    {
        private const int E = ColorSlot.Empty;

        [Test]
        public void RoundTrip_PreservesLayout()
        {
            int[] slots = { 0, 1, E, 2, E, E, 1, 1, 1 };

            var bars = MagicSortFlat.ToBars(slots, 3);

            Assert.AreEqual(3, bars.Count);
            Assert.AreEqual(2, bars[0].Count);
            Assert.IsTrue(bars[1].IsEmpty == false && bars[1].Count == 1);
            CollectionAssert.AreEqual(slots, MagicSortFlat.Flatten(bars, 3));
        }

        [Test]
        public void Metrics_SolvedLevel_IsZero()
        {
            var metrics = LevelMetrics.Calculate(MagicSortGenerator.CreateSolvedLevel(3, 4, 1));

            Assert.AreEqual(0f, metrics.Depth);
            Assert.AreEqual(0f, metrics.Fragmentation);
        }
    }
}
