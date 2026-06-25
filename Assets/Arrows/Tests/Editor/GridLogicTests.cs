using NUnit.Framework;
using UnityEngine;

namespace ReplicaProjects.Arrows.Tests
{
    public class GridLogicTests
    {
        private GridLogic _gridLogic;

        [SetUp]
        public void SetUp()
        {
            _gridLogic = new GridLogic();
        }

        [Test]
        public void CoordinatesToIndex_KnownCase()
        {
            Assert.AreEqual(13, _gridLogic.CoordinatesToIndex(3, 2, 5));
        }

        [Test]
        public void IndexToCoordinates_KnownCase()
        {
            Assert.AreEqual(new Vector2Int(2, 1), _gridLogic.IndexToCoordinates(7, 5));
        }

        [Test]
        public void RoundTrip_PreservesCoordinates()
        {
            const int width = 5;
            const int height = 4;

            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                var index = _gridLogic.CoordinatesToIndex(x, y, width);
                var coords = _gridLogic.IndexToCoordinates(index, width);
                Assert.AreEqual(new Vector2Int(x, y), coords, $"Failed at ({x},{y})");
            }
        }

        [Test]
        public void IsInBounds_RejectsOutOfRange()
        {
            const int length = 10;

            Assert.IsFalse(_gridLogic.IsInBounds(-1, length));
            Assert.IsFalse(_gridLogic.IsInBounds(length, length));
            Assert.IsTrue(_gridLogic.IsInBounds(0, length));
            Assert.IsTrue(_gridLogic.IsInBounds(length - 1, length));
        }

        [Test]
        public void IsEmpty_EmptyCell_ReturnsTrue()
        {
            var input = new GridCellInput { gridData = new bool[20], width = 5, x = 3, y = 2 };

            Assert.IsTrue(_gridLogic.IsEmpty(input));
        }

        [Test]
        public void IsEmpty_SetCell_ReturnsFalse()
        {
            var gridData = new bool[20];
            gridData[_gridLogic.CoordinatesToIndex(3, 2, 5)] = true;

            var input = new GridCellInput { gridData = gridData, width = 5, x = 3, y = 2 };

            Assert.IsFalse(_gridLogic.IsEmpty(input));
        }

        [Test]
        public void IsEmpty_OutOfBounds_ReturnsFalse()
        {
            var input = new GridCellInput { gridData = new bool[20], width = 5, x = 0, y = 10 };

            Assert.IsFalse(_gridLogic.IsEmpty(input));
        }
    }
}
