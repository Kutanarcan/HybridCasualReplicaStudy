using System.Collections.Generic;
using NUnit.Framework;
using ReplicaProjects.Common.TestSupport;

namespace ReplicaProjects.Common.Tests
{
    public class TickGroupTests
    {
        private sealed class RecordingTickable : ITickable
        {
            private readonly List<string> _log;
            private readonly string _name;

            public RecordingTickable(List<string> log, string name)
            {
                _log = log;
                _name = name;
            }

            public void Tick(float deltaTime) => _log?.Add(_name);
        }

        [Test]
        public void Tick_RunsMembersInRegistrationOrder()
        {
            var log = new List<string>();
            var group = new TickGroup();
            group.Add(new RecordingTickable(log, "a"));
            group.Add(new RecordingTickable(log, "b"));

            group.Tick(0.016f);

            CollectionAssert.AreEqual(new[] { "a", "b" }, log);
        }

        // Called every frame from the composition root.
        [Test]
        public void Tick_DoesNotAllocate()
        {
            var group = new TickGroup();
            group.Add(new RecordingTickable(null, "a"));
            TestDelegate tick = () => { group.Tick(0.016f); };

            AllocationAssert.NoSteadyStateAllocations(tick);
        }
    }
}
