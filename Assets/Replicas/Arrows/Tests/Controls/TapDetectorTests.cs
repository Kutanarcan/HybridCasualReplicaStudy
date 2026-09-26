using System.Collections.Generic;
using NUnit.Framework;
using ReplicaProjects.Common.TestSupport;

namespace ReplicaProjects.Arrows.Tests
{
    public class TapDetectorTests
    {
        private const float DragThreshold = 10f;

        private FakeInputSource _input;
        private TapDetector _detector;
        private List<GridCoord> _taps;

        [SetUp]
        public void SetUp()
        {
            _input = new FakeInputSource();
            _detector = new TapDetector(_input, new FakeViewport(), new CellPicker(1.1f), DragThreshold);
            _taps = new List<GridCoord>();
            _detector.CellTapped += _taps.Add;
        }

        private void Step() => _detector.Tick(0.016f);

        [Test]
        public void PressAndRelease_OnCellCentre_TapsCell()
        {
            _input.Press(200f, 300f);
            Step();
            _input.Release(200f, 300f);
            Step();

            CollectionAssert.AreEqual(new[] { new GridCoord(2, 3) }, _taps);
        }

        [Test]
        public void TapDetector_DragBeyondThreshold_NoSelection()
        {
            _input.Press(200f, 300f);
            Step();
            _input.Release(200f + DragThreshold + 1f, 300f);
            Step();

            Assert.IsEmpty(_taps);
        }

        [Test]
        public void Release_OutsideHitCircle_NoSelection()
        {
            // World (2.5, 3.5) is ~0.71 from the nearest centre; the hit radius is 0.55.
            _input.Press(250f, 350f);
            Step();
            _input.Release(250f, 350f);
            Step();

            Assert.IsEmpty(_taps);
        }

        [Test]
        public void Tick_WithoutRelease_NoSelection()
        {
            _input.Press(200f, 300f);
            Step();
            _input.Drag(200f, 300f);
            Step();

            Assert.IsEmpty(_taps);
        }

        // Per-frame hot path.
        [Test]
        public void Tick_Idle_DoesNotAllocate()
        {
            _input.Idle();
            TestDelegate tick = () => { _detector.Tick(0.016f); };

            AllocationAssert.NoSteadyStateAllocations(tick);
        }
    }
}
