using NUnit.Framework;

namespace ReplicaProjects.Arrows.Tests
{
    public class GridOrchestratorTests
    {
        [Test]
        public void Set_XOutOfBounds_DoesNotWrapOntoNextRow()
        {
            var grid = new GridOrchestrator();
            grid.Initialize(5, 4);

            grid.Set(new GridCoord(5, 0), true);

            Assert.IsTrue(grid.IsEmpty(new GridCoord(0, 1)));
        }

        [Test]
        public void IsInBounds_XEqualToWidth_ReturnsFalse()
        {
            var grid = new GridOrchestrator();
            grid.Initialize(5, 4);

            Assert.IsFalse(grid.IsInBounds(new GridCoord(5, 0)));
        }
    }
}
