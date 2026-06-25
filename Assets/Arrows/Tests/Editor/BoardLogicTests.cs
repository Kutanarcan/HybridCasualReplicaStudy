using System.Collections.Generic;
using NUnit.Framework;

namespace ReplicaProjects.Arrows.Tests
{
    public class BoardLogicTests
    {
        private BoardLogic _boardLogic;

        [SetUp]
        public void SetUp()
        {
            _boardLogic = new BoardLogic();
        }

        [Test]
        public void PickRandomArray_ResultLength_EqualsHeadCount()
        {
            var input = new PickRandomArrayInput { boardSize = 20, headCount = 5 };

            var result = _boardLogic.PickRandomArray(input);

            Assert.AreEqual(5, result.Length);
        }

        [Test]
        public void PickRandomArray_AllValues_InBoardSizeRange()
        {
            var input = new PickRandomArrayInput { boardSize = 10, headCount = 4 };

            for (int run = 0; run < 20; run++)
            {
                var result = _boardLogic.PickRandomArray(input);
                foreach (var value in result)
                    Assert.IsTrue(value >= 0 && value < input.boardSize,
                        $"Value {value} out of [0, {input.boardSize})");
            }
        }

        [Test]
        public void PickRandomArray_NoDuplicates()
        {
            var input = new PickRandomArrayInput { boardSize = 20, headCount = 6 };

            for (int run = 0; run < 20; run++)
            {
                var result = _boardLogic.PickRandomArray(input);
                var seen = new HashSet<int>();
                foreach (var value in result)
                    Assert.IsTrue(seen.Add(value), $"Duplicate value {value} on run {run}");
            }
        }

        [Test]
        public void PickRandomArray_NoDuplicates_HeadCount_Equal_BoardSize()
        {
            var input = new PickRandomArrayInput { boardSize = 20, headCount = 20 };

            for (int run = 0; run < 20; run++)
            {
                var result = _boardLogic.PickRandomArray(input);
                var seen = new HashSet<int>();
                foreach (var value in result)
                    Assert.IsTrue(seen.Add(value), $"Duplicate value {value} on run {run}");
            }
        }
    }
}
