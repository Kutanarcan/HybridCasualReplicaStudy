using NUnit.Framework;
using ReplicaProjects.Common.TestSupport;

namespace ReplicaProjects.Arrows.Tests
{
    public class CameraRigTests
    {
        private const float Tolerance = 1e-4f;

        private FakeInputSource _input;
        private FakeViewport _viewport;
        private CameraRigSettings _settings;
        private CameraRig _rig;

        [SetUp]
        public void SetUp()
        {
            _input = new FakeInputSource();
            _viewport = new FakeViewport { Aspect = 0.5f, ScreenHeight = 1000f };
            _settings = new CameraRigSettings();
            _rig = new CameraRig(_input, _viewport, _settings);
            _rig.Frame(10, 10);
        }

        [Test]
        public void Frame_CentresOnBoard()
        {
            Assert.AreEqual(4.5f, _rig.X, Tolerance);
            Assert.AreEqual(4.5f, _rig.Y, Tolerance);
        }

        [Test]
        public void CameraFit_TallScreen_WidthDrivesZoomOut()
        {
            // Padded half-extents: x = 5 + 0.5, y = 5 + 2.5. Aspect 0.5 => width needs 5.5 / 0.5 = 11.
            Assert.AreEqual(11f, _rig.OrthographicSize, Tolerance);
        }

        [Test]
        public void Scroll_ZoomsInButNotBelowMinimum()
        {
            _input.Frame = new PointerFrame { Scroll = 1000f };
            _rig.Tick(0.016f);

            Assert.AreEqual(_settings.MinOrthographicSize, _rig.OrthographicSize, Tolerance);
        }

        [Test]
        public void Scroll_ZoomsOutButNotBeyondFit()
        {
            _input.Frame = new PointerFrame { Scroll = -1000f };
            _rig.Tick(0.016f);

            Assert.AreEqual(11f, _rig.OrthographicSize, Tolerance);
        }

        [Test]
        public void Pan_WhenFullyZoomedOut_StaysLockedToCentre()
        {
            _input.Press(500f, 500f);
            _rig.Tick(0.016f);
            _input.Drag(900f, 100f);
            _rig.Tick(0.016f);

            Assert.AreEqual(4.5f, _rig.X, Tolerance);
            Assert.AreEqual(4.5f, _rig.Y, Tolerance);
        }

        [Test]
        public void Pan_WhenZoomedIn_MovesOppositeToPointerAndClampsToBounds()
        {
            _input.Frame = new PointerFrame { Scroll = 1000f }; // zoom to min size 2
            _rig.Tick(0.016f);

            _input.Press(500f, 500f);
            _rig.Tick(0.016f);
            _input.Drag(400f, 500f); // 100 px left => camera moves right by 100 * (2*2/1000) = 0.4
            _rig.Tick(0.016f);
            Assert.AreEqual(4.9f, _rig.X, Tolerance);

            _input.Drag(-100000f, 500f); // far drag => clamped to the right edge: 4.5 + 5.5 - 2*0.5
            _rig.Tick(0.016f);
            Assert.AreEqual(9f, _rig.X, Tolerance);
        }

        [Test]
        public void Pinch_FirstFrameSeeds_ThenZooms()
        {
            _input.Frame = new PointerFrame { TouchCount = 2, PinchDistance = 100f, PinchBegan = true };
            _rig.Tick(0.016f);
            Assert.AreEqual(11f, _rig.OrthographicSize, Tolerance);

            _input.Frame = new PointerFrame { TouchCount = 2, PinchDistance = 300f };
            _rig.Tick(0.016f);
            Assert.AreEqual(11f - 200f * _settings.PinchZoomSpeed, _rig.OrthographicSize, Tolerance);
        }

        // Per-frame hot path.
        [Test]
        public void Tick_DoesNotAllocate()
        {
            _input.Drag(500f, 500f);
            TestDelegate tick = () => { _rig.Tick(0.016f); };

            AllocationAssert.NoSteadyStateAllocations(tick);
        }
    }
}
