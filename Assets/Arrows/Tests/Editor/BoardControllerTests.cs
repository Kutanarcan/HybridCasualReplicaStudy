using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

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

        private static LevelData MakeLevel(int width, int height, params (int x, int y, Direction dir)[] heads)
        {
            var level = ScriptableObject.CreateInstance<LevelData>();
            level.width = width;
            level.height = height;
            level.heads = new List<HeadData>();
            foreach (var (x, y, dir) in heads)
                level.heads.Add(new HeadData { coordinates = new Vector2Int(x, y), direction = dir });
            return level;
        }

        [Test]
        public void Initialize_HeadCells_AreOccupied()
        {
            var level = MakeLevel(5, 5, (1, 1, Direction.Up), (3, 3, Direction.Left));
            _controller.Initialize(level);

            Assert.IsFalse(_controller.IsEmpty(new Vector2Int(1, 1)));
            Assert.IsFalse(_controller.IsEmpty(new Vector2Int(3, 3)));
        }

        [Test]
        public void Initialize_EmptyCells_AreEmpty()
        {
            var level = MakeLevel(5, 5, (1, 1, Direction.Up));
            _controller.Initialize(level);

            Assert.IsTrue(_controller.IsEmpty(new Vector2Int(0, 0)));
            Assert.IsTrue(_controller.IsEmpty(new Vector2Int(4, 4)));
            Assert.IsTrue(_controller.IsEmpty(new Vector2Int(2, 2)));
        }

        [Test]
        public void IsHeadPathClear_NoObstacles_ReturnsTrue()
        {
            // Head at (1,2) facing Right — (2,2),(3,2),(4,2) all empty.
            var level = MakeLevel(5, 5, (1, 2, Direction.Right));
            _controller.Initialize(level);

            int headIndex = _controller.Grid.CoordinatesToIndex(new Vector2Int(1, 2));
            Assert.IsTrue(_controller.IsHeadPathClear(headIndex));
        }

        [Test]
        public void IsHeadPathClear_AnotherHeadBlocking_ReturnsFalse()
        {
            // Two heads on the same row; first faces Right into the second.
            var level = MakeLevel(5, 5, (1, 2, Direction.Right), (3, 2, Direction.Left));
            _controller.Initialize(level);

            int headIndex = _controller.Grid.CoordinatesToIndex(new Vector2Int(1, 2));
            Assert.IsFalse(_controller.IsHeadPathClear(headIndex));
        }
    }
}
