using NUnit.Framework;

namespace ReplicaProjects.MagicSort.Tests
{
    public class MagicSortLogicTests
    {
        private const int E = MagicSortLevel.Empty; // -1
        private const int BarHeight = 4;

        private readonly MagicSortLogic _logic = new();

        // Builds a board input from bar rows. Each row is a bar, slot 0 = bottom.
        private static int[] Board(params int[][] bars)
        {
            var flat = new int[bars.Length * BarHeight];
            for (int b = 0; b < bars.Length; b++)
            {
                Assert.AreEqual(BarHeight, bars[b].Length, "each bar must have BarHeight slots");
                for (int s = 0; s < BarHeight; s++)
                    flat[b * BarHeight + s] = bars[b][s];
            }
            return flat;
        }

        private SequentialBarArrayInput Input(int[] flat)
            => new(flat, BarHeight);

        // ── IsBarEmpty ──

        [Test]
        public void IsBarEmpty_TrueForFullyEmptyBar()
        {
            var flat = Board(new[] { E, E, E, E });
            Assert.IsTrue(_logic.IsBarEmpty(Input(flat), 0));
        }

        [Test]
        public void IsBarEmpty_FalseWhenBottomSlotFilled()
        {
            var flat = Board(new[] { 0, E, E, E });
            Assert.IsFalse(_logic.IsBarEmpty(Input(flat), 0));
        }

        // ── IsBarFull ──

        [Test]
        public void IsBarFull_TrueWhenTopSlotFilled()
        {
            var flat = Board(new[] { 0, 0, 0, 0 });
            Assert.IsTrue(_logic.IsBarFull(Input(flat), 0));
        }

        [Test]
        public void IsBarFull_FalseWhenTopSlotEmpty()
        {
            var flat = Board(new[] { 0, 0, 0, E });
            Assert.IsFalse(_logic.IsBarFull(Input(flat), 0));
        }

        [Test]
        public void IsBarFull_FalseForEmptyBar()
        {
            var flat = Board(new[] { E, E, E, E });
            Assert.IsFalse(_logic.IsBarFull(Input(flat), 0));
        }

        // ── IsSameBar ──

        [Test]
        public void IsSameBar_ComparesIndices()
        {
            Assert.IsTrue(_logic.IsSameBar(2, 2));
            Assert.IsFalse(_logic.IsSameBar(1, 2));
        }

        // ── IsBarSolved ──

        [Test]
        public void IsBarSolved_TrueForFullSingleColorBar()
        {
            var flat = Board(new[] { 1, 1, 1, 1 });
            Assert.IsTrue(_logic.IsBarSolved(Input(flat), 0));
        }

        [Test]
        public void IsBarSolved_FalseForFullMixedBar()
        {
            var flat = Board(new[] { 1, 1, 2, 1 });
            Assert.IsFalse(_logic.IsBarSolved(Input(flat), 0));
        }

        [Test]
        public void IsBarSolved_FalseForPartiallyFilledUniformBar()
        {
            var flat = Board(new[] { 1, 1, 1, E });
            Assert.IsFalse(_logic.IsBarSolved(Input(flat), 0));
        }

        [Test]
        public void IsBarSolved_FalseForEmptyBar()
        {
            var flat = Board(new[] { E, E, E, E });
            Assert.IsFalse(_logic.IsBarSolved(Input(flat), 0));
        }

        // ── IsAllBarsSolved ──

        [Test]
        public void IsAllBarsSolved_TrueWhenEveryBarUniformIncludingEmpty()
        {
            var flat = Board(
                new[] { 0, 0, 0, 0 },
                new[] { 1, 1, 1, 1 },
                new[] { E, E, E, E });
            Assert.IsTrue(_logic.IsAllBarsSolved(Input(flat)));
        }

        [Test]
        public void IsAllBarsSolved_FalseWhenAnyBarMixed()
        {
            var flat = Board(
                new[] { 0, 0, 0, 0 },
                new[] { 1, 1, 0, 1 });
            Assert.IsFalse(_logic.IsAllBarsSolved(Input(flat)));
        }

        [Test]
        public void IsAllBarsSolved_FalseWhenBarPartiallyFilled()
        {
            // A half-filled bar has trailing Empty slots that differ from its color.
            var flat = Board(
                new[] { 0, 0, 0, 0 },
                new[] { 1, 1, E, E });
            Assert.IsFalse(_logic.IsAllBarsSolved(Input(flat)));
        }

        // ── GetTopBarIndexColorValue ──

        [Test]
        public void GetTopBarIndexColorValue_ReturnsTopMostFilledColor()
        {
            var flat = Board(new[] { 2, 3, E, E });
            Assert.AreEqual(3, _logic.GetTopBarIndexColorValue(Input(flat), 0));
        }

        [Test]
        public void GetTopBarIndexColorValue_ReturnsEmptyForEmptyBar()
        {
            var flat = Board(new[] { E, E, E, E });
            Assert.AreEqual(E, _logic.GetTopBarIndexColorValue(Input(flat), 0));
        }

        // ── TopFilledSlotIndex ──

        [Test]
        public void TopFilledSlotIndex_ReturnsIndexOfTopMostFilledSlot()
        {
            int[] bar = { 2, 3, E, E };
            Assert.AreEqual(1, _logic.TopFilledSlotIndex(bar));
        }

        [Test]
        public void TopFilledSlotIndex_ReturnsZeroWhenOnlyBottomSlotFilled()
        {
            int[] bar = { 2, E, E, E };
            Assert.AreEqual(0, _logic.TopFilledSlotIndex(bar));
        }

        [Test]
        public void TopFilledSlotIndex_ReturnsTopIndexForFullBar()
        {
            int[] bar = { 1, 1, 1, 1 };
            Assert.AreEqual(3, _logic.TopFilledSlotIndex(bar));
        }

        [Test]
        public void TopFilledSlotIndex_ReturnsEmptyForEmptyBar()
        {
            int[] bar = { E, E, E, E };
            Assert.AreEqual(E, _logic.TopFilledSlotIndex(bar));
        }

        // ── IsBarUniform ──

        [Test]
        public void IsBarUniform_TrueForSingleColorBar()
        {
            int[] bar = { 1, 1, 1, 1 };
            Assert.IsTrue(_logic.IsBarUniform(bar));
        }

        [Test]
        public void IsBarUniform_TrueForFullyEmptyBar()
        {
            int[] bar = { E, E, E, E };
            Assert.IsTrue(_logic.IsBarUniform(bar));
        }

        [Test]
        public void IsBarUniform_FalseForMixedColors()
        {
            int[] bar = { 1, 1, 2, 1 };
            Assert.IsFalse(_logic.IsBarUniform(bar));
        }

        [Test]
        public void IsBarUniform_FalseForColorWithTrailingEmpties()
        {
            int[] bar = { 1, 1, E, E };
            Assert.IsFalse(_logic.IsBarUniform(bar));
        }

        // ── SameColorRunFromTop ──

        [Test]
        public void SameColorRunFromTop_CountsContiguousTopRun()
        {
            int[] bar = { 0, 0, E, E };
            Assert.AreEqual(2, _logic.SameColorRunFromTop(bar, topSlot: 1, color: 0));
        }

        [Test]
        public void SameColorRunFromTop_StopsAtDifferentColorBelow()
        {
            int[] bar = { 2, 0, 0, E };
            Assert.AreEqual(2, _logic.SameColorRunFromTop(bar, topSlot: 2, color: 0));
        }

        [Test]
        public void SameColorRunFromTop_CountsWholeBarWhenUniform()
        {
            int[] bar = { 5, 5, 5, 5 };
            Assert.AreEqual(4, _logic.SameColorRunFromTop(bar, topSlot: 3, color: 5));
        }

        [Test]
        public void SameColorRunFromTop_CountsSingleTopBall()
        {
            int[] bar = { 1, E, E, E };
            Assert.AreEqual(1, _logic.SameColorRunFromTop(bar, topSlot: 0, color: 1));
        }

        // ── EvaluateAvailableBarPlacement ──

        [Test]
        public void Evaluate_RejectsSameBar()
        {
            var flat = Board(new[] { 0, E, E, E });
            var result = _logic.EvaluateAvailableBarPlacement(Input(flat), 0, 0);

            Assert.IsFalse(result.Success);
            Assert.AreEqual(PlacementOutcome.SameBar, result.outCome);
            Assert.AreEqual(0, result.movedCount);
        }

        [Test]
        public void Evaluate_RejectsFullTarget()
        {
            var flat = Board(
                new[] { 0, E, E, E },
                new[] { 1, 1, 1, 1 });
            var result = _logic.EvaluateAvailableBarPlacement(Input(flat), 0, 1);

            Assert.IsFalse(result.Success);
            Assert.AreEqual(PlacementOutcome.TargetFull, result.outCome);
        }

        [Test]
        public void Evaluate_RejectsColorMismatch()
        {
            var flat = Board(
                new[] { 0, E, E, E },
                new[] { 1, E, E, E });
            var result = _logic.EvaluateAvailableBarPlacement(Input(flat), 0, 1);

            Assert.IsFalse(result.Success);
            Assert.AreEqual(PlacementOutcome.ColorMismatch, result.outCome);
        }

        [Test]
        public void Evaluate_MovesWholeRunIntoEmptyTarget()
        {
            // Source has a run of two 0's; empty target has room for all of them.
            var flat = Board(
                new[] { 0, 0, E, E },
                new[] { E, E, E, E });
            var result = _logic.EvaluateAvailableBarPlacement(Input(flat), 0, 1);

            Assert.IsTrue(result.Success);
            Assert.AreEqual(PlacementOutcome.Moved, result.outCome);
            Assert.AreEqual(2, result.movedCount);
            Assert.AreEqual(E, result.sourceNewTop); // source emptied
            Assert.AreEqual(0, result.targetNewTop);
        }

        [Test]
        public void Evaluate_MoveCountLimitedByTargetFreeSpace()
        {
            // Source run of three 0's, but target (bottom 1, then 0) has only two free slots.
            var flat = Board(
                new[] { 0, 0, 0, E },
                new[] { 1, 0, E, E });
            var result = _logic.EvaluateAvailableBarPlacement(Input(flat), 0, 1);

            Assert.IsTrue(result.Success);
            Assert.AreEqual(2, result.movedCount);
            Assert.AreEqual(0, result.sourceNewTop);  // one 0 left on source
            Assert.AreEqual(0, result.targetNewTop);
        }

        [Test]
        public void Evaluate_MovesOnlyTopColorRunNotColorBelow()
        {
            // Source is [2,0,0]; only the two 0's on top form the run, the 2 stays.
            var flat = Board(
                new[] { 2, 0, 0, E },
                new[] { E, E, E, E });
            var result = _logic.EvaluateAvailableBarPlacement(Input(flat), 0, 1);

            Assert.IsTrue(result.Success);
            Assert.AreEqual(2, result.movedCount);
            Assert.AreEqual(2, result.sourceNewTop); // the 2 is now on top of source
            Assert.AreEqual(0, result.targetNewTop);
        }

        [Test]
        public void Evaluate_MovesOntoMatchingColorTarget()
        {
            var flat = Board(
                new[] { 5, E, E, E },
                new[] { 5, 5, E, E });
            var result = _logic.EvaluateAvailableBarPlacement(Input(flat), 0, 1);

            Assert.IsTrue(result.Success);
            Assert.AreEqual(1, result.movedCount);
            Assert.AreEqual(E, result.sourceNewTop);
            Assert.AreEqual(5, result.targetNewTop);
        }

        // ── EvaluateTap ──

        private const int NoSelection = -1;

        [Test]
        public void EvaluateTap_IgnoresTapOnEmptyBarWhenNothingSelected()
        {
            var flat = Board(new[] { E, E, E, E });
            var result = _logic.EvaluateTap(Input(flat), NoSelection, 0);

            Assert.AreEqual(TapKind.Ignored, result.Kind);
            Assert.AreEqual(NoSelection, result.NewSource);
        }

        [Test]
        public void EvaluateTap_SelectsSourceWhenTappingFilledBarWithNothingSelected()
        {
            var flat = Board(new[] { 0, E, E, E });
            var result = _logic.EvaluateTap(Input(flat), NoSelection, 0);

            Assert.AreEqual(TapKind.SourceSelected, result.Kind);
            Assert.AreEqual(0, result.NewSource);
        }

        [Test]
        public void EvaluateTap_DeselectsWhenTappingTheSelectedBarAgain()
        {
            var flat = Board(new[] { 0, E, E, E });
            var result = _logic.EvaluateTap(Input(flat), currentSource: 0, tappedBar: 0);

            Assert.AreEqual(TapKind.Deselected, result.Kind);
            Assert.AreEqual(NoSelection, result.NewSource);
            Assert.AreEqual(0, result.SourceBar);
        }

        [Test]
        public void EvaluateTap_ConsumesWhenTappingAValidTarget()
        {
            var flat = Board(
                new[] { 0, E, E, E },
                new[] { E, E, E, E });
            var result = _logic.EvaluateTap(Input(flat), currentSource: 0, tappedBar: 1);

            Assert.AreEqual(TapKind.Consumed, result.Kind);
            Assert.AreEqual(NoSelection, result.NewSource);
            Assert.AreEqual(0, result.SourceBar);
            Assert.AreEqual(1, result.TargetBar);
            Assert.IsTrue(result.TransportResult.Success);
            Assert.AreEqual(1, result.TransportResult.movedCount);
        }

        [Test]
        public void EvaluateTap_RetargetsSelectionWhenTappingAnInvalidTarget()
        {
            // Selected bar 0 (a 0), tapped bar 1 is full -> selection moves to bar 1.
            var flat = Board(
                new[] { 0, E, E, E },
                new[] { 1, 1, 1, 1 });
            var result = _logic.EvaluateTap(Input(flat), currentSource: 0, tappedBar: 1);

            Assert.AreEqual(TapKind.Retargeted, result.Kind);
            Assert.AreEqual(1, result.NewSource);      // selection moved to the tapped bar
            Assert.AreEqual(0, result.SourceBar);      // previous source
            Assert.AreEqual(PlacementOutcome.TargetFull, result.RejectReason);
        }

        // ── ApplyPour ──

        [Test]
        public void ApplyPour_MovesWholeRunIntoEmptyTarget()
        {
            var flat = Board(
                new[] { 0, 0, E, E },
                new[] { E, E, E, E });

            _logic.ApplyPour(flat, BarHeight, sourceBarIndex: 0, targetBarIndex: 1, movedCount: 2);

            CollectionAssert.AreEqual(new[] { E, E, E, E }, BarSlice(flat, 0));
            CollectionAssert.AreEqual(new[] { 0, 0, E, E }, BarSlice(flat, 1));
        }

        [Test]
        public void ApplyPour_MovesPartialRunOntoMatchingColor()
        {
            var flat = Board(
                new[] { 0, 0, 0, E },
                new[] { 1, 0, E, E });

            _logic.ApplyPour(flat, BarHeight, sourceBarIndex: 0, targetBarIndex: 1, movedCount: 2);

            CollectionAssert.AreEqual(new[] { 0, E, E, E }, BarSlice(flat, 0));
            CollectionAssert.AreEqual(new[] { 1, 0, 0, 0 }, BarSlice(flat, 1));
        }

        [Test]
        public void ApplyPour_LeavesColorBelowTheMovedRunUntouched()
        {
            // Only the two 0's on top move; the 2 at the bottom of the source stays.
            var flat = Board(
                new[] { 2, 0, 0, E },
                new[] { E, E, E, E });

            _logic.ApplyPour(flat, BarHeight, sourceBarIndex: 0, targetBarIndex: 1, movedCount: 2);

            CollectionAssert.AreEqual(new[] { 2, E, E, E }, BarSlice(flat, 0));
            CollectionAssert.AreEqual(new[] { 0, 0, E, E }, BarSlice(flat, 1));
        }

        private static int[] BarSlice(int[] flat, int barIndex)
        {
            var bar = new int[BarHeight];
            System.Array.Copy(flat, barIndex * BarHeight, bar, 0, BarHeight);
            return bar;
        }
    }
}
