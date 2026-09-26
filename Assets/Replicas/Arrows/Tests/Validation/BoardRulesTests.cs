using NUnit.Framework;

namespace ReplicaProjects.Arrows.Tests
{
    public class BoardRulesTests
    {
        private readonly BoardRules _rules = new();

        // ---- Boundary rule -------------------------------------------------

        [Test]
        public void EvaluateCorner_BottomLeft_BansRightAndUp()
        {
            var result = _rules.EvaluateCorner(new BoardCornerEvaluateInput
            { coordinates = new GridCoord(0, 0), width = 5, height = 5 });

            CollectionAssert.AreEquivalent(new[] { Direction.Up, Direction.Right }, result.bannedDirections);
        }

        [Test]
        public void EvaluateCorner_BottomEdge_BansUpOnly()
        {
            var result = _rules.EvaluateCorner(new BoardCornerEvaluateInput
            { coordinates = new GridCoord(2, 0), width = 5, height = 5 });

            CollectionAssert.AreEquivalent(new[] { Direction.Up }, result.bannedDirections);
        }

        [Test]
        public void EvaluateCorner_Interior_BansNothing()
        {
            var result = _rules.EvaluateCorner(new BoardCornerEvaluateInput
            { coordinates = new GridCoord(2, 2), width = 5, height = 5 });

            Assert.IsEmpty(result.bannedDirections);
            Assert.IsFalse(result.IsAtCorner);
        }

        // ---- Intersection rules --------------------------------------------

        [Test]
        public void EvaluateIntersection_FacingTowardOnRow_BansApproach()
        {
            var result = _rules.EvaluateIntersection(new BoardIntersectionEvaluateInput
            {
                coordinateA = new GridCoord(0, 0),
                coordinateB = new GridCoord(3, 0),
                directionB = Direction.Left   // B points toward A
            });

            Assert.IsTrue(result.HasIntersection);
            CollectionAssert.Contains(result.bannedDirections, Direction.Right);
        }

        [Test]
        public void EvaluateIntersection_AdjacentAway_BansRetreat()
        {
            var result = _rules.EvaluateIntersection(new BoardIntersectionEvaluateInput
            {
                coordinateA = new GridCoord(0, 0),
                coordinateB = new GridCoord(1, 0),
                directionB = Direction.Right  // B points away from A
            });

            CollectionAssert.Contains(result.bannedDirections, Direction.Left);
        }

        [Test]
        public void EvaluateIntersection_DifferentRowAndColumn_NoIntersection()
        {
            var result = _rules.EvaluateIntersection(new BoardIntersectionEvaluateInput
            {
                coordinateA = new GridCoord(0, 0),
                coordinateB = new GridCoord(2, 3),
                directionB = Direction.Left
            });

            Assert.IsFalse(result.HasIntersection);
            Assert.IsEmpty(result.bannedDirections);
        }
    }
}
