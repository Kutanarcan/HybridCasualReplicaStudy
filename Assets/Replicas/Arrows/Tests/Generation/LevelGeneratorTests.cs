using System.Collections.Generic;
using NUnit.Framework;
using ReplicaProjects.Common;

namespace ReplicaProjects.Arrows.Tests
{
    public class LevelGeneratorTests
    {
        private const int Runs = 50;

        private LevelGenerator _generator;
        private BoardValidator _validator;

        [SetUp]
        public void SetUp()
        {
            _generator = new LevelGenerator(new SeededRandomSource(12345));
            _validator = new BoardValidator();
        }

        // ---- PickRandomArray -----------------------------------------------

        [Test]
        public void PickRandomArray_ResultLength_EqualsHeadCount()
        {
            var result = _generator.PickRandomArray(new PickRandomArrayInput { boardSize = 20, headCount = 5 });

            Assert.AreEqual(5, result.Length);
        }

        [Test]
        public void PickRandomArray_AllValues_InBoardSizeRange()
        {
            var input = new PickRandomArrayInput { boardSize = 10, headCount = 4 };

            for (int run = 0; run < 20; run++)
                foreach (var value in _generator.PickRandomArray(input))
                    Assert.IsTrue(value >= 0 && value < input.boardSize,
                        $"Value {value} out of [0, {input.boardSize})");
        }

        [TestCase(20, 6)]
        [TestCase(20, 20)]
        public void PickRandomArray_NoDuplicates(int boardSize, int headCount)
        {
            var input = new PickRandomArrayInput { boardSize = boardSize, headCount = headCount };

            for (int run = 0; run < 20; run++)
            {
                var seen = new HashSet<int>();
                foreach (var value in _generator.PickRandomArray(input))
                    Assert.IsTrue(seen.Add(value), $"Duplicate value {value} on run {run}");
            }
        }

        // ---- Determinism ---------------------------------------------------

        [Test]
        public void GenerateRandomLevel_SameSeed_SameLevel()
        {
            var a = new LevelGenerator(new SeededRandomSource(7)).GenerateRandomLevel(8, 8, 10);
            var b = new LevelGenerator(new SeededRandomSource(7)).GenerateRandomLevel(8, 8, 10);

            Assert.AreEqual(a.Count, b.Count);
            for (int i = 0; i < a.Count; i++)
            {
                Assert.AreEqual(a[i].coordinates, b[i].coordinates);
                Assert.AreEqual(a[i].direction, b[i].direction);
                CollectionAssert.AreEqual(a[i].line, b[i].line);
            }
        }

        // ---- Generated levels are valid ------------------------------------

        [Test]
        public void GenerateRandomLevel_AlwaysValid()
        {
            for (int run = 0; run < Runs; run++)
                AssertValid(_generator.GenerateRandomLevel(5, 5, 6), 5, 5, run);
        }

        [Test]
        public void GenerateRandomLevel_IsAlwaysSolvable_LargeBoard()
        {
            // Direct regression guard: a big, dense board must always be solvable. Validation includes
            // the deadlock/solvability check, so isValid implies a valid clearing order exists.
            for (int run = 0; run < 100; run++)
                AssertValid(_generator.GenerateRandomLevel(15, 15, 30), 15, 15, run);
        }

        // ---- Line properties -----------------------------------------------

        [Test]
        public void GenerateRandomLevel_EveryHeadHasLine()
        {
            ForEachGeneratedHead((head, run) =>
                Assert.IsTrue(head.line != null && head.line.Count > 0,
                    $"Head at {head.coordinates} has no line on run {run}"));
        }

        [Test]
        public void GenerateRandomLevel_FirstLineCell_IsBehindHead()
        {
            ForEachGeneratedHead((head, run) =>
                Assert.AreEqual(head.coordinates - head.direction.ToOffset(), head.line[0].coordinates,
                    $"Head at {head.coordinates} ({head.direction}) first line cell wrong on run {run}"));
        }

        [Test]
        public void GenerateRandomLevel_FirstLineCellDirection_IsOppositeOfHead()
        {
            ForEachGeneratedHead((head, run) =>
                Assert.AreEqual(head.direction.Opposite(), head.line[0].direction,
                    $"Head at {head.coordinates} ({head.direction}) first line direction wrong on run {run}"));
        }

        [Test]
        public void GenerateRandomLevel_LineNeverEntersOwnLineOfSight()
        {
            ForEachGeneratedHead((head, run) =>
            {
                var los = new HashSet<int>();
                BoardGeometry.CollectLineOfSight(head.coordinates, head.direction, 5, 5, los);
                foreach (var cell in head.line)
                    Assert.IsFalse(los.Contains(BoardGeometry.ToIndex(cell.coordinates, 5)),
                        $"Head at {head.coordinates} line cell {cell.coordinates} in its own line of sight, run {run}");
            });
        }

        [Test]
        public void GenerateRandomLevel_LinesAreContiguousInBoundsAndDirected()
        {
            ForEachGeneratedHead((head, run) =>
            {
                var prev = head.coordinates;
                foreach (var cell in head.line)
                {
                    Assert.IsTrue(BoardGeometry.InBounds(cell.coordinates, 5, 5), $"Line cell {cell.coordinates} out of bounds on run {run}");
                    Assert.AreEqual(1, (cell.coordinates - prev).SqrMagnitude, $"Line cell {cell.coordinates} not adjacent to {prev} on run {run}");
                    Assert.AreEqual((cell.coordinates - prev).ToDirection(), cell.direction, $"Cell {cell.coordinates} direction mismatch on run {run}");
                    prev = cell.coordinates;
                }
            });
        }

        [Test]
        public void GenerateRandomLevel_NoCellsOverlap()
        {
            for (int run = 0; run < Runs; run++)
            {
                var seen = new HashSet<GridCoord>();
                foreach (var head in _generator.GenerateRandomLevel(5, 5, 6))
                {
                    Assert.IsTrue(seen.Add(head.coordinates), $"Head cell {head.coordinates} overlaps on run {run}");
                    foreach (var cell in head.line)
                        Assert.IsTrue(seen.Add(cell.coordinates), $"Line cell {cell.coordinates} overlaps on run {run}");
                }
            }
        }

        // ---- Helpers -------------------------------------------------------

        private void ForEachGeneratedHead(System.Action<HeadData, int> assert)
        {
            for (int run = 0; run < Runs; run++)
                foreach (var head in _generator.GenerateRandomLevel(5, 5, 6))
                    assert(head, run);
        }

        private void AssertValid(List<HeadData> heads, int width, int height, int run)
        {
            var result = _validator.ValidateBoard(new BoardValidateInput { width = width, height = height, heads = heads });

            Assert.IsTrue(result.isValid,
                $"Generated {width}x{height} level invalid on run {run}: " +
                string.Join(", ", result.violations.ConvertAll(v => $"{v.coordinates} {v.reason}")));
        }
    }
}
