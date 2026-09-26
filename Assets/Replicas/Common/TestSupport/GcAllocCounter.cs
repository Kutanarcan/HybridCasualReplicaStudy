using System;
using NUnit.Framework;
using UnityEngine.Profiling;

namespace ReplicaProjects.Common.TestSupport
{
    /// <summary>
    /// Counts managed allocations made by a delegate on the calling thread via the "GC.Alloc" profiler
    /// marker (the one Is.Not.AllocatingGCMemory() uses). GC.GetAllocatedBytesForCurrentThread reads 0
    /// on Unity's Mono, so allocation *count* is the available metric.
    /// </summary>
    public static class GcAllocCounter
    {
        public static int Count(TestDelegate action)
        {
            var recorder = Recorder.Get("GC.Alloc");
            recorder.enabled = false;
            recorder.FilterToCurrentThread();
            recorder.enabled = true;
            try
            {
                action();
            }
            finally
            {
                recorder.enabled = false;
                recorder.CollectFromAllThreads();
            }

            return recorder.sampleBlockCount;
        }

        /// <summary>
        /// Steady-state count: one warm-up call (JIT, lazy init), then the minimum over <paramref name="runs"/>
        /// measured calls. The first run after a domain reload can pick up one-off runtime allocations; an
        /// allocation the code really makes per call shows up in every run, so the minimum still sees it.
        /// </summary>
        public static int SteadyState(TestDelegate action, int runs = 3)
        {
            action();

            int min = int.MaxValue;
            for (int i = 0; i < runs; i++)
                min = Math.Min(min, Count(action));
            return min;
        }
    }
}
