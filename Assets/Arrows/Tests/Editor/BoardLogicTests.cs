using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

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

        // ---- Boundary rule -------------------------------------------------

        [Test]
        public void EvaluateCorner_BottomLeft_BansRightAndUp()
        {
            var result = _boardLogic.EvaluateCorner(new BoardCornerEvaluateInput
            { coordinates = new Vector2Int(0, 0), width = 5, height = 5 });

            CollectionAssert.AreEquivalent(
                new[] { Direction.Up, Direction.Right }, result.bannedDirections);
        }

        [Test]
        public void EvaluateCorner_BottomEdge_BansUpOnly()
        {
            var result = _boardLogic.EvaluateCorner(new BoardCornerEvaluateInput
            { coordinates = new Vector2Int(2, 0), width = 5, height = 5 });

            CollectionAssert.AreEquivalent(new[] { Direction.Up }, result.bannedDirections);
        }

        [Test]
        public void EvaluateCorner_Interior_BansNothing()
        {
            var result = _boardLogic.EvaluateCorner(new BoardCornerEvaluateInput
            { coordinates = new Vector2Int(2, 2), width = 5, height = 5 });

            Assert.IsEmpty(result.bannedDirections);
            Assert.IsFalse(result.IsAtCorner);
        }

        // ---- Intersection rules --------------------------------------------

        [Test]
        public void EvaluateIntersection_FacingTowardOnRow_BansApproach()
        {
            var result = _boardLogic.EvaluateIntersection(new BoardIntersectionEvaluateInput
            {
                coordinateA = new Vector2Int(0, 0),
                coordinateB = new Vector2Int(3, 0),
                directionB = Direction.Left   // B points toward A
            });

            Assert.IsTrue(result.HasIntersection);
            CollectionAssert.Contains(result.bannedDirections, Direction.Right);
        }

        [Test]
        public void EvaluateIntersection_AdjacentAway_BansRetreat()
        {
            var result = _boardLogic.EvaluateIntersection(new BoardIntersectionEvaluateInput
            {
                coordinateA = new Vector2Int(0, 0),
                coordinateB = new Vector2Int(1, 0),
                directionB = Direction.Right  // B points away from A
            });

            CollectionAssert.Contains(result.bannedDirections, Direction.Left);
        }

        [Test]
        public void EvaluateIntersection_DifferentRowAndColumn_NoIntersection()
        {
            var result = _boardLogic.EvaluateIntersection(new BoardIntersectionEvaluateInput
            {
                coordinateA = new Vector2Int(0, 0),
                coordinateB = new Vector2Int(2, 3),
                directionB = Direction.Left
            });

            Assert.IsFalse(result.HasIntersection);
            Assert.IsEmpty(result.bannedDirections);
        }

        // ---- Generation + validation ---------------------------------------

        [Test]
        public void GenerateRandomLevel_AlwaysValid()
        {
            for (int run = 0; run < 50; run++)
            {
                var heads = _boardLogic.GenerateRandomLevel(5, 5, 6);
                var result = _boardLogic.ValidateBoard(new BoardValidateInput
                { width = 5, height = 5, heads = heads });

                Assert.IsTrue(result.isValid,
                    $"Generated level invalid on run {run}: " +
                    string.Join(", ", result.violations.ConvertAll(v => $"{v.coordinates} {v.reason}")));
            }
        }

        [Test]
        public void ValidateBoard_HeadFacingOffBoard_Fails()
        {
            var heads = new List<HeadData>
            {
                new HeadData { coordinates = new Vector2Int(0, 0), direction = Direction.Up }
            };

            var result = _boardLogic.ValidateBoard(new BoardValidateInput
            { width = 5, height = 5, heads = heads });

            Assert.IsFalse(result.isValid);
            Assert.AreEqual(new Vector2Int(0, 0), result.violations[0].coordinates);
        }

        [Test]
        public void ValidateBoard_HeadsFacingEachOther_Fails()
        {
            var heads = new List<HeadData>
            {
                new HeadData { coordinates = new Vector2Int(0, 2), direction = Direction.Right },
                new HeadData { coordinates = new Vector2Int(4, 2), direction = Direction.Left }
            };

            var result = _boardLogic.ValidateBoard(new BoardValidateInput
            { width = 5, height = 5, heads = heads });

            Assert.IsFalse(result.isValid);
        }
    }
}
