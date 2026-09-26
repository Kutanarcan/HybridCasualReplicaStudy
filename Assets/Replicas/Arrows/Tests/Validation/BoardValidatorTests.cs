using System.Collections.Generic;
using NUnit.Framework;
using static ReplicaProjects.Arrows.Tests.TestHeads;

namespace ReplicaProjects.Arrows.Tests
{
    public class BoardValidatorTests
    {
        private readonly BoardValidator _validator = new();

        private BoardValidateResult Validate(List<HeadData> heads) =>
            _validator.ValidateBoard(new BoardValidateInput { width = 5, height = 5, heads = heads });

        private static bool HasReason(BoardValidateResult result, string reason) =>
            result.violations.Exists(v => v.reason == reason);

        private static string Describe(BoardValidateResult result) =>
            string.Join(", ", result.violations.ConvertAll(v => $"{v.coordinates} {v.reason}"));

        // ---- Heads ---------------------------------------------------------

        [Test]
        public void ValidateBoard_HeadFacingOffBoard_Fails()
        {
            var result = Validate(List(Head(0, 0, Direction.Up)));

            Assert.IsFalse(result.isValid);
            Assert.AreEqual(new GridCoord(0, 0), result.violations[0].coordinates);
        }

        [Test]
        public void ValidateBoard_HeadsFacingEachOther_Fails()
        {
            var result = Validate(List(Head(0, 2, Direction.Right), Head(4, 2, Direction.Left)));

            Assert.IsFalse(result.isValid);
        }

        // ---- Lines ---------------------------------------------------------

        [Test]
        public void ValidateBoard_ValidHeadWithLine_Passes()
        {
            // Head at (2,2) facing Up; line cell (2,1) is directly behind — direction is Down.
            var result = Validate(List(Head(2, 2, Direction.Up, (2, 1))));

            Assert.IsTrue(result.isValid, Describe(result));
        }

        [Test]
        public void ValidateBoard_HeadWithNoLine_Fails()
        {
            var result = Validate(List(Head(2, 2, Direction.Up)));

            Assert.IsFalse(result.isValid);
            Assert.IsTrue(HasReason(result, "has no line"));
        }

        [Test]
        public void ValidateBoard_LineInOwnLineOfSight_Fails()
        {
            // (2,3) is straight ahead of head at (2,2) facing Up — in the LOS.
            var result = Validate(List(Head(2, 2, Direction.Up, (2, 3))));

            Assert.IsFalse(result.isValid);
            Assert.IsTrue(HasReason(result, "line crosses its own head's line of sight"));
        }

        [Test]
        public void ValidateBoard_NonContiguousLine_Fails()
        {
            var result = Validate(List(Head(2, 2, Direction.Up, (0, 0)))); // not adjacent to head

            Assert.IsFalse(result.isValid);
            Assert.IsTrue(HasReason(result, "line is not contiguous"));
        }

        [Test]
        public void ValidateBoard_OffBoardLineCell_Fails()
        {
            // Head at (0,2) facing Up; (0,1) is behind (valid), (-1,1) is off-board.
            var result = Validate(List(Head(0, 2, Direction.Up, (0, 1), (-1, 1))));

            Assert.IsFalse(result.isValid);
            Assert.IsTrue(HasReason(result, "line cell off-board"));
        }

        [Test]
        public void ValidateBoard_OverlappingLines_Fails()
        {
            // Both heads claim (2,3) as their first line cell.
            var result = Validate(List(Head(2, 2, Direction.Down, (2, 3)), Head(2, 4, Direction.Up, (2, 3))));

            Assert.IsFalse(result.isValid);
            Assert.IsTrue(HasReason(result, "lines overlap"));
        }

        [Test]
        public void ValidateBoard_LineCellDirectionMismatch_Fails()
        {
            // (2,1) is behind (2,2) Up — correct direction is Down; supplying Up should fail.
            var head = Head(2, 2, Direction.Up, (2, 1));
            head.line[0] = new LineCell { coordinates = new GridCoord(2, 1), direction = Direction.Up };

            var result = Validate(List(head));

            Assert.IsFalse(result.isValid);
            Assert.IsTrue(HasReason(result, "line direction mismatch"));
        }

        // ---- Solvability (deadlock) ----------------------------------------

        [Test]
        public void ValidateBoard_CircularRayDependency_Fails()
        {
            // X is blocked by Y's line cell (4,1) and Y is blocked by X's line cell (0,3); the heads
            // never face each other, so the structure is valid but the board can never be cleared.
            var result = Validate(List(
                Head(1, 1, Direction.Right, (0, 1), (0, 2), (0, 3)),
                Head(3, 3, Direction.Left, (4, 3), (4, 2), (4, 1))));

            Assert.IsFalse(result.isValid);
            Assert.IsTrue(HasReason(result, "deadlocked: path can never clear"));
        }

        [Test]
        public void ValidateBoard_SolvableForcedOrder_Passes()
        {
            // Q has a clear ray (removable now); P's ray is blocked by Q's head until Q is gone, so
            // the forced order Q -> P clears the board.
            var result = Validate(List(Head(2, 2, Direction.Right, (1, 2)), Head(3, 2, Direction.Down, (3, 3))));

            Assert.IsTrue(result.isValid, Describe(result));
        }
    }
}
