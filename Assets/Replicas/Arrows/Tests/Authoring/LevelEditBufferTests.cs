using NUnit.Framework;
using static ReplicaProjects.Arrows.Tests.TestHeads;

namespace ReplicaProjects.Arrows.Tests
{
    public class LevelEditBufferTests
    {
        private LevelEditBuffer _buffer;

        [SetUp]
        public void SetUp()
        {
            _buffer = new LevelEditBuffer();
            _buffer.Load(5, 5, List(Head(2, 2, Direction.Up, (2, 1), (3, 1), (4, 1))));
        }

        [Test]
        public void Load_DeepCopiesLines()
        {
            var source = List(Head(1, 1, Direction.Up, (1, 0)));
            _buffer.Load(5, 5, source);

            _buffer.TrimLineAt(new GridCoord(1, 0));

            Assert.AreEqual(1, source[0].line.Count, "editing the buffer must not touch the asset's list");
        }

        [Test]
        public void StampHead_OnExistingHead_ChangesDirectionAndKeepsLine()
        {
            _buffer.StampHead(new GridCoord(2, 2), Direction.Left);

            Assert.AreEqual(Direction.Left, _buffer.Heads[0].direction);
            Assert.AreEqual(3, _buffer.Heads[0].line.Count);
        }

        [Test]
        public void TrimLineAt_RemovesCellAndEverythingPastIt()
        {
            Assert.IsTrue(_buffer.TrimLineAt(new GridCoord(3, 1)));

            Assert.AreEqual(1, _buffer.Heads[0].line.Count);
        }

        [Test]
        public void RemoveAt_Head_RemovesWholeArrow()
        {
            Assert.IsTrue(_buffer.RemoveAt(new GridCoord(2, 2)));

            Assert.IsEmpty(_buffer.Heads);
        }

        [Test]
        public void Resize_Shrink_TrimsLineFromFirstOutsideCell()
        {
            Assert.IsFalse(_buffer.HasHeadsOutside(4, 5));

            _buffer.Resize(4, 5);

            Assert.AreEqual(2, _buffer.Heads[0].line.Count); // (4,1) fell outside
        }

        [Test]
        public void Resize_Shrink_DropsHeadsOutside()
        {
            Assert.IsTrue(_buffer.HasHeadsOutside(2, 5));

            _buffer.Resize(2, 5);

            Assert.IsEmpty(_buffer.Heads);
        }
    }
}
