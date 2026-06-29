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

        // ---- Lines: generation properties ----------------------------------

        [Test]
        public void GenerateRandomLevel_EveryHeadHasLine()
        {
            for (int run = 0; run < 50; run++)
            {
                var heads = _boardLogic.GenerateRandomLevel(5, 5, 6);
                foreach (var head in heads)
                    Assert.IsTrue(head.line != null && head.line.Count > 0,
                        $"Head at {head.coordinates} has no line on run {run}");
            }
        }

        [Test]
        public void GenerateRandomLevel_FirstLineCell_IsBehindHead()
        {
            for (int run = 0; run < 50; run++)
            {
                var heads = _boardLogic.GenerateRandomLevel(5, 5, 6);
                foreach (var head in heads)
                {
                    var expected = head.coordinates - DirVec(head.direction);
                    Assert.AreEqual(expected, head.line[0].coordinates,
                        $"Head at {head.coordinates} ({head.direction}) first line cell wrong on run {run}");
                }
            }
        }

        [Test]
        public void GenerateRandomLevel_LineNeverEntersOwnLineOfSight()
        {
            for (int run = 0; run < 50; run++)
            {
                var heads = _boardLogic.GenerateRandomLevel(5, 5, 6);
                foreach (var head in heads)
                {
                    var los = LineOfSight(head.coordinates, head.direction, 5, 5);
                    foreach (var cell in head.line)
                        Assert.IsFalse(los.Contains(cell.coordinates),
                            $"Head at {head.coordinates} ({head.direction}) line cell {cell.coordinates} in its own line of sight, run {run}");
                }
            }
        }

        [Test]
        public void GenerateRandomLevel_LinesAreContiguousAndInBounds()
        {
            for (int run = 0; run < 50; run++)
            {
                var heads = _boardLogic.GenerateRandomLevel(5, 5, 6);
                foreach (var head in heads)
                {
                    var prev = head.coordinates;
                    foreach (var cell in head.line)
                    {
                        var coord = cell.coordinates;
                        Assert.IsTrue(coord.x >= 0 && coord.x < 5 && coord.y >= 0 && coord.y < 5,
                            $"Line cell {coord} out of bounds on run {run}");
                        Assert.AreEqual(1, (coord - prev).sqrMagnitude,
                            $"Line cell {coord} not adjacent to {prev} on run {run}");
                        prev = coord;
                    }
                }
            }
        }

        [Test]
        public void GenerateRandomLevel_NoCellsOverlap()
        {
            for (int run = 0; run < 50; run++)
            {
                var heads = _boardLogic.GenerateRandomLevel(5, 5, 6);
                var seen = new HashSet<Vector2Int>();

                foreach (var head in heads)
                {
                    Assert.IsTrue(seen.Add(head.coordinates),
                        $"Head cell {head.coordinates} overlaps on run {run}");
                    foreach (var cell in head.line)
                        Assert.IsTrue(seen.Add(cell.coordinates),
                            $"Line cell {cell.coordinates} overlaps on run {run}");
                }
            }
        }

        // ---- Lines: validation ---------------------------------------------

        [Test]
        public void ValidateBoard_ValidHeadWithLine_Passes()
        {
            // Head at (2,2) facing Up; line cell (2,1) is directly behind — direction is Down.
            var heads = new List<HeadData>
            {
                new HeadData
                {
                    coordinates = new Vector2Int(2, 2),
                    direction = Direction.Up,
                    line = new List<LineCell>
                    {
                        new LineCell { coordinates = new Vector2Int(2, 1), direction = Direction.Down }
                    }
                }
            };

            var result = _boardLogic.ValidateBoard(new BoardValidateInput
            { width = 5, height = 5, heads = heads });

            Assert.IsTrue(result.isValid,
                string.Join(", ", result.violations.ConvertAll(v => $"{v.coordinates} {v.reason}")));
        }

        [Test]
        public void ValidateBoard_HeadWithNoLine_Fails()
        {
            var heads = new List<HeadData>
            {
                new HeadData { coordinates = new Vector2Int(2, 2), direction = Direction.Up }
            };

            var result = _boardLogic.ValidateBoard(new BoardValidateInput
            { width = 5, height = 5, heads = heads });

            Assert.IsFalse(result.isValid);
            Assert.IsTrue(HasReason(result, "has no line"));
        }

        [Test]
        public void ValidateBoard_LineInOwnLineOfSight_Fails()
        {
            // (2,3) is straight ahead of head at (2,2) facing Up — in the LOS.
            var heads = new List<HeadData>
            {
                new HeadData
                {
                    coordinates = new Vector2Int(2, 2),
                    direction = Direction.Up,
                    line = new List<LineCell>
                    {
                        new LineCell { coordinates = new Vector2Int(2, 3), direction = Direction.Up }
                    }
                }
            };

            var result = _boardLogic.ValidateBoard(new BoardValidateInput
            { width = 5, height = 5, heads = heads });

            Assert.IsFalse(result.isValid);
            Assert.IsTrue(HasReason(result, "line crosses its own head's line of sight"));
        }

        [Test]
        public void ValidateBoard_NonContiguousLine_Fails()
        {
            var heads = new List<HeadData>
            {
                new HeadData
                {
                    coordinates = new Vector2Int(2, 2),
                    direction = Direction.Up,
                    line = new List<LineCell>
                    {
                        new LineCell { coordinates = new Vector2Int(0, 0), direction = Direction.Left } // not adjacent to head
                    }
                }
            };

            var result = _boardLogic.ValidateBoard(new BoardValidateInput
            { width = 5, height = 5, heads = heads });

            Assert.IsFalse(result.isValid);
            Assert.IsTrue(HasReason(result, "line is not contiguous"));
        }

        [Test]
        public void ValidateBoard_OffBoardLineCell_Fails()
        {
            // Head at (0,2) facing Up; (0,1) is behind (valid), (-1,1) is off-board.
            var heads = new List<HeadData>
            {
                new HeadData
                {
                    coordinates = new Vector2Int(0, 2),
                    direction = Direction.Up,
                    line = new List<LineCell>
                    {
                        new LineCell { coordinates = new Vector2Int(0, 1), direction = Direction.Down },
                        new LineCell { coordinates = new Vector2Int(-1, 1), direction = Direction.Left }
                    }
                }
            };

            var result = _boardLogic.ValidateBoard(new BoardValidateInput
            { width = 5, height = 5, heads = heads });

            Assert.IsFalse(result.isValid);
            Assert.IsTrue(HasReason(result, "line cell off-board"));
        }

        [Test]
        public void ValidateBoard_OverlappingLines_Fails()
        {
            // Both heads claim (2,3) as their first line cell.
            var heads = new List<HeadData>
            {
                new HeadData
                {
                    coordinates = new Vector2Int(2, 2),
                    direction = Direction.Down,
                    line = new List<LineCell>
                    {
                        new LineCell { coordinates = new Vector2Int(2, 3), direction = Direction.Up }
                    }
                },
                new HeadData
                {
                    coordinates = new Vector2Int(2, 4),
                    direction = Direction.Up,
                    line = new List<LineCell>
                    {
                        new LineCell { coordinates = new Vector2Int(2, 3), direction = Direction.Down }
                    }
                }
            };

            var result = _boardLogic.ValidateBoard(new BoardValidateInput
            { width = 5, height = 5, heads = heads });

            Assert.IsFalse(result.isValid);
            Assert.IsTrue(HasReason(result, "lines overlap"));
        }

        // ---- Lines: direction correctness ----------------------------------

        [Test]
        public void GenerateRandomLevel_FirstLineCellDirection_IsOppositeOfHead()
        {
            for (int run = 0; run < 50; run++)
            {
                var heads = _boardLogic.GenerateRandomLevel(5, 5, 6);
                foreach (var head in heads)
                {
                    var first = head.line[0];
                    Assert.AreEqual(head.direction.Opposite(), first.direction,
                        $"Head at {head.coordinates} ({head.direction}) first line direction wrong on run {run}");
                }
            }
        }

        [Test]
        public void GenerateRandomLevel_LineCellDirections_MatchSteps()
        {
            for (int run = 0; run < 50; run++)
            {
                var heads = _boardLogic.GenerateRandomLevel(5, 5, 6);
                foreach (var head in heads)
                {
                    var prev = head.coordinates;
                    foreach (var cell in head.line)
                    {
                        var expected = (cell.coordinates - prev).ToDirection();
                        Assert.AreEqual(expected, cell.direction,
                            $"Head at {head.coordinates} cell {cell.coordinates} direction mismatch on run {run}");
                        prev = cell.coordinates;
                    }
                }
            }
        }

        [Test]
        public void ValidateBoard_LineCellDirectionMismatch_Fails()
        {
            // (2,1) is behind (2,2) Up — correct direction is Down; supplying Up should fail.
            var heads = new List<HeadData>
            {
                new HeadData
                {
                    coordinates = new Vector2Int(2, 2),
                    direction = Direction.Up,
                    line = new List<LineCell>
                    {
                        new LineCell { coordinates = new Vector2Int(2, 1), direction = Direction.Up }
                    }
                }
            };

            var result = _boardLogic.ValidateBoard(new BoardValidateInput
            { width = 5, height = 5, heads = heads });

            Assert.IsFalse(result.isValid);
            Assert.IsTrue(HasReason(result, "line direction mismatch"));
        }

        // ---- Solvability (deadlock) ----------------------------------------

        [Test]
        public void ValidateBoard_CircularRayDependency_Fails()
        {
            // X is blocked by Y's line cell (4,1) and Y is blocked by X's line cell (0,3); the heads
            // never face each other, so the structure is valid but the board can never be cleared.
            var heads = new List<HeadData>
            {
                new HeadData
                {
                    coordinates = new Vector2Int(1, 1),
                    direction = Direction.Right,
                    line = new List<LineCell>
                    {
                        new LineCell { coordinates = new Vector2Int(0, 1), direction = Direction.Left },
                        new LineCell { coordinates = new Vector2Int(0, 2), direction = Direction.Up },
                        new LineCell { coordinates = new Vector2Int(0, 3), direction = Direction.Up }
                    }
                },
                new HeadData
                {
                    coordinates = new Vector2Int(3, 3),
                    direction = Direction.Left,
                    line = new List<LineCell>
                    {
                        new LineCell { coordinates = new Vector2Int(4, 3), direction = Direction.Right },
                        new LineCell { coordinates = new Vector2Int(4, 2), direction = Direction.Down },
                        new LineCell { coordinates = new Vector2Int(4, 1), direction = Direction.Down }
                    }
                }
            };

            var result = _boardLogic.ValidateBoard(new BoardValidateInput
            { width = 5, height = 5, heads = heads });

            Assert.IsFalse(result.isValid);
            Assert.IsTrue(HasReason(result, "deadlocked: path can never clear"));
        }

        [Test]
        public void GenerateRandomLevel_IsAlwaysSolvable_LargeBoard()
        {
            // Direct regression guard: a big, dense board must always be solvable. ValidateBoard now
            // includes the deadlock/solvability check, so isValid implies a valid clearing order exists.
            for (int run = 0; run < 100; run++)
            {
                var heads = _boardLogic.GenerateRandomLevel(15, 15, 30);
                var result = _boardLogic.ValidateBoard(new BoardValidateInput
                { width = 15, height = 15, heads = heads });

                Assert.IsTrue(result.isValid,
                    $"Generated 15x15 level not solvable on run {run}: " +
                    string.Join(", ", result.violations.ConvertAll(v => $"{v.coordinates} {v.reason}")));
            }
        }

        [Test]
        public void ValidateBoard_SolvableForcedOrder_Passes()
        {
            // Q has a clear ray (removable now); P's ray is blocked by Q's head until Q is gone, so
            // the forced order Q -> P clears the board.
            var heads = new List<HeadData>
            {
                new HeadData
                {
                    coordinates = new Vector2Int(2, 2),
                    direction = Direction.Right,
                    line = new List<LineCell>
                    {
                        new LineCell { coordinates = new Vector2Int(1, 2), direction = Direction.Left }
                    }
                },
                new HeadData
                {
                    coordinates = new Vector2Int(3, 2),
                    direction = Direction.Down,
                    line = new List<LineCell>
                    {
                        new LineCell { coordinates = new Vector2Int(3, 3), direction = Direction.Up }
                    }
                }
            };

            var result = _boardLogic.ValidateBoard(new BoardValidateInput
            { width = 5, height = 5, heads = heads });

            Assert.IsTrue(result.isValid,
                string.Join(", ", result.violations.ConvertAll(v => $"{v.coordinates} {v.reason}")));
        }

        // ---- Helpers -------------------------------------------------------

        private static bool HasReason(BoardValidateResult result, string reason) =>
            result.violations.Exists(v => v.reason == reason);

        private static Vector2Int DirVec(Direction dir)
        {
            switch (dir)
            {
                case Direction.Up: return new Vector2Int(0, 1);
                case Direction.Down: return new Vector2Int(0, -1);
                case Direction.Left: return new Vector2Int(-1, 0);
                case Direction.Right: return new Vector2Int(1, 0);
                default: return Vector2Int.zero;
            }
        }

        private static HashSet<Vector2Int> LineOfSight(Vector2Int from, Direction dir, int width, int height)
        {
            var cells = new HashSet<Vector2Int>();
            var step = DirVec(dir);
            if (step == Vector2Int.zero)
                return cells;

            var c = from + step;
            while (c.x >= 0 && c.x < width && c.y >= 0 && c.y < height)
            {
                cells.Add(c);
                c += step;
            }

            return cells;
        }
    }
}
