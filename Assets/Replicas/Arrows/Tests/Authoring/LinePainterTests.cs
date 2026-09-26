using NUnit.Framework;
using static ReplicaProjects.Arrows.Tests.TestHeads;

namespace ReplicaProjects.Arrows.Tests
{
    public class LinePainterTests
    {
        private LevelEditBuffer _buffer;
        private LinePainter _painter;

        [SetUp]
        public void SetUp()
        {
            _buffer = new LevelEditBuffer();
            _buffer.Load(5, 5, List(Head(2, 2, Direction.Up, (2, 1))));
            _painter = new LinePainter(_buffer);
        }

        [Test]
        public void Paint_Head_SelectsWithoutChanging()
        {
            Assert.IsFalse(_painter.Paint(new GridCoord(2, 2)));
            Assert.AreEqual(new GridCoord(2, 2), _painter.SelectedHead);
        }

        [Test]
        public void Paint_CellAdjacentToTail_ExtendsLineWithFlowDirection()
        {
            _painter.Paint(new GridCoord(2, 2));

            Assert.IsTrue(_painter.Paint(new GridCoord(3, 1)));

            var cell = _buffer.Heads[0].line[1];
            Assert.AreEqual(new GridCoord(3, 1), cell.coordinates);
            Assert.AreEqual(Direction.Right, cell.direction);
        }

        [Test]
        public void Paint_NotAdjacentToTail_Rejected()
        {
            _painter.Paint(new GridCoord(2, 2));

            Assert.IsFalse(_painter.Paint(new GridCoord(0, 0)));
        }

        [Test]
        public void Paint_IntoOwnLineOfSight_Rejected()
        {
            // Head (2,2) faces Up; a line bending round to (2,3) would sit in its own ray.
            _buffer.Load(5, 5, List(Head(2, 2, Direction.Up, (1, 2), (1, 3))));
            _painter.Paint(new GridCoord(2, 2));

            Assert.IsFalse(_painter.Paint(new GridCoord(2, 3)));
        }

        [Test]
        public void Paint_WithoutSelection_Rejected()
        {
            Assert.IsFalse(_painter.Paint(new GridCoord(2, 0)));
        }

        [Test]
        public void Erase_SelectedHead_DropsSelection()
        {
            _painter.Paint(new GridCoord(2, 2));

            Assert.IsTrue(_painter.Erase(new GridCoord(2, 2)));
            Assert.IsNull(_painter.SelectedHead);
        }
    }
}
