using NUnit.Framework;

namespace ReplicaProjects.MagicSort.Tests
{
    public class CompletionCounterTests
    {
        [Test]
        public void ZeroCount_CompletesImmediately()
        {
            int done = 0;
            new CompletionCounter(0, () => done++);

            Assert.AreEqual(1, done);
        }

        [Test]
        public void CompletesOnce_AfterAllSignals()
        {
            int done = 0;
            var counter = new CompletionCounter(3, () => done++);

            counter.Signal();
            counter.Signal();
            Assert.AreEqual(0, done);

            counter.Signal();
            counter.Signal(); // extra signals are ignored
            Assert.AreEqual(1, done);
        }
    }
}
