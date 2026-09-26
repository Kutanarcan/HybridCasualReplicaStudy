using NUnit.Framework;

namespace ReplicaProjects.MagicSort.Tests
{
    public class BarGridLayoutTests
    {
        private static readonly BarGridLayout Layout = new(columns: 3, columnSpacing: 1.75f, rowDrop: 3f, rowDepth: 2f);

        [TestCase(0, 0f, 0f, 0f)]
        [TestCase(2, 3.5f, 0f, 0f)]
        [TestCase(3, 0f, -3f, -2f)]
        [TestCase(7, 1.75f, -6f, -4f)]
        public void ThemeLayout_BarPositions(int index, float x, float y, float z)
        {
            Layout.Position(index, out float px, out float py, out float pz);

            Assert.AreEqual(x, px, 1e-5f);
            Assert.AreEqual(y, py, 1e-5f);
            Assert.AreEqual(z, pz, 1e-5f);
        }
    }
}
