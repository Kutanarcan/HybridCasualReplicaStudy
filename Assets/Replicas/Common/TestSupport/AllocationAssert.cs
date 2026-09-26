using NUnit.Framework;

namespace ReplicaProjects.Common.TestSupport
{
    public static class AllocationAssert
    {
        /// <summary>Fails if the delegate allocates on every call. The delegate is invoked 1 + runs times.</summary>
        public static void NoSteadyStateAllocations(TestDelegate action, int runs = 3)
        {
            int allocations = GcAllocCounter.SteadyState(action, runs);
            Assert.AreEqual(0, allocations, $"expected no GC allocations per call, measured {allocations}");
        }
    }
}
