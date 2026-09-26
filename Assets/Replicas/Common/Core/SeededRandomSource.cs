using System;

namespace ReplicaProjects.Common
{
    public sealed class SeededRandomSource : IRandomSource
    {
        private readonly Random _random;

        public SeededRandomSource() => _random = new Random();
        public SeededRandomSource(int seed) => _random = new Random(seed);

        public int Next(int minInclusive, int maxExclusive) => _random.Next(minInclusive, maxExclusive);
        public double NextDouble() => _random.NextDouble();
    }
}
