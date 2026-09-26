using NUnit.Framework;
using static ReplicaProjects.Arrows.Tests.TestHeads;

namespace ReplicaProjects.Arrows.Tests
{
    public class BoardControllerTests
    {
        private BoardController _controller;

        [SetUp]
        public void SetUp()
        {
            _controller = new BoardController();
        }

        [Test]
        public void Initialize_HeadCells_AreOccupied()
        {
            _controller.Initialize(5, 5, List(Head(1, 1, Direction.Up), Head(3, 3, Direction.Left)));

            Assert.IsFalse(_controller.IsEmpty(new GridCoord(1, 1)));
            Assert.IsFalse(_controller.IsEmpty(new GridCoord(3, 3)));
        }

        [Test]
        public void Initialize_EmptyCells_AreEmpty()
        {
            _controller.Initialize(5, 5, List(Head(1, 1, Direction.Up)));

            Assert.IsTrue(_controller.IsEmpty(new GridCoord(0, 0)));
            Assert.IsTrue(_controller.IsEmpty(new GridCoord(4, 4)));
            Assert.IsTrue(_controller.IsEmpty(new GridCoord(2, 2)));
        }

        [Test]
        public void IsHeadPathClear_NoObstacles_ReturnsTrue()
        {
            // Head at (1,2) facing Right — (2,2),(3,2),(4,2) all empty.
            _controller.Initialize(5, 5, List(Head(1, 2, Direction.Right)));

            Assert.IsTrue(_controller.IsPathClear(new GridCoord(1, 2)));
        }

        [Test]
        public void IsHeadPathClear_AnotherHeadBlocking_ReturnsFalse()
        {
            // Two heads on the same row; first faces Right into the second.
            _controller.Initialize(5, 5, List(Head(1, 2, Direction.Right), Head(3, 2, Direction.Left)));

            Assert.IsFalse(_controller.IsPathClear(new GridCoord(1, 2)));
        }

        [Test]
        public void GetHeadCoordinate_LineCell_ResolvesToOwningHead()
        {
            _controller.Initialize(5, 5, List(Head(2, 2, Direction.Up, (2, 1), (3, 1))));

            Assert.AreEqual(new GridCoord(2, 2), _controller.GetHeadCoordinate(new GridCoord(3, 1)));
        }

        [Test]
        public void GetForwardBlocker_EmptyCell_ReturnsInput()
        {
            _controller.Initialize(5, 5, List(Head(1, 1, Direction.Up)));

            var empty = new GridCoord(4, 4);
            Assert.AreEqual(empty, _controller.GetForwardBlocker(empty));
        }

        [Test]
        public void GetForwardBlocker_OutOfBounds_ReturnsInput()
        {
            _controller.Initialize(5, 5, List(Head(1, 1, Direction.Up)));

            // x == width used to wrap onto the next row's first cell.
            var outside = new GridCoord(5, 0);
            Assert.AreEqual(outside, _controller.GetForwardBlocker(outside));
        }

        [Test]
        public void RemoveAtCoordinate_FreesHeadAndLineCells()
        {
            _controller.Initialize(5, 5, List(Head(2, 2, Direction.Up, (2, 1), (3, 1))));

            _controller.RemoveAtCoordinate(new GridCoord(3, 1));

            Assert.IsTrue(_controller.IsEmpty(new GridCoord(2, 2)));
            Assert.IsTrue(_controller.IsEmpty(new GridCoord(2, 1)));
            Assert.IsTrue(_controller.IsEmpty(new GridCoord(3, 1)));
            Assert.AreEqual(0, _controller.RemainingArrows);
        }
    }
}
