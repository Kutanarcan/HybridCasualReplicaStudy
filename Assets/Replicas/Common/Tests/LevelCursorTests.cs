using System;
using NUnit.Framework;

namespace ReplicaProjects.Common.Tests
{
    public class LevelCursorTests
    {
        [Test]
        public void Advance_WrapsToFirstLevel()
        {
            var cursor = new LevelCursor(3);

            cursor.Advance();
            cursor.Advance();
            Assert.AreEqual(2, cursor.Current);

            Assert.AreEqual(0, cursor.Advance());
        }

        [Test]
        public void EmptyLevelList_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new LevelCursor(0));
        }
    }
}
