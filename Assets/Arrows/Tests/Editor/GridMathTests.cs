using NUnit.Framework;
using UnityEngine;

namespace ReplicaProjects.Arrows.Tests
{
    public class GridMathTests
    {
        [Test]
        public void CoordinatesToIndex_KnownCase()
        {
            Assert.AreEqual(13, GridMath.CoordinatesToIndex(3, 2, 5));
        }

        [Test]
        public void IndexToCoordinates_KnownCase()
        {
            Assert.AreEqual(new Vector2Int(2, 1), GridMath.IndexToCoordinates(7, 5));
        }

        [Test]
        public void RoundTrip_PreservesCoordinates()
        {
            const int width = 5;
            const int height = 4;

            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                var index = GridMath.CoordinatesToIndex(x, y, width);
                var coords = GridMath.IndexToCoordinates(index, width);
                Assert.AreEqual(new Vector2Int(x, y), coords, $"Failed at ({x},{y})");
            }
        }

        [Test]
        public void IsInBounds_RejectsOutOfRange()
        {
            const int length = 10;

            Assert.IsFalse(GridMath.IsInBounds(-1, length));
            Assert.IsFalse(GridMath.IsInBounds(length, length));
            Assert.IsTrue(GridMath.IsInBounds(0, length));
            Assert.IsTrue(GridMath.IsInBounds(length - 1, length));
        }
    }
}
