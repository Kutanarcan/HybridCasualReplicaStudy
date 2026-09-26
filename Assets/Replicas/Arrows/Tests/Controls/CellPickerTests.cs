using NUnit.Framework;

namespace ReplicaProjects.Arrows.Tests
{
    public class CellPickerTests
    {
        private readonly CellPicker _picker = new(1.1f);

        [Test]
        public void TryPick_NearCentre_ReturnsRoundedCell()
        {
            Assert.IsTrue(_picker.TryPick(3.2f, 1.9f, out var cell));
            Assert.AreEqual(new GridCoord(3, 2), cell);
        }

        [Test]
        public void TryPick_CellCorner_Rejected()
        {
            Assert.IsFalse(_picker.TryPick(3.45f, 2.45f, out _));
        }

        [Test]
        public void TryPick_NegativeWorld_ReturnsNegativeCell()
        {
            Assert.IsTrue(_picker.TryPick(-1.1f, 0f, out var cell));
            Assert.AreEqual(new GridCoord(-1, 0), cell);
        }
    }
}
