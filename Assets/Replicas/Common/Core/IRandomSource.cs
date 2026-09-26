namespace ReplicaProjects.Common
{
    public interface IRandomSource
    {
        /// <summary>Uniform integer in [minInclusive, maxExclusive).</summary>
        int Next(int minInclusive, int maxExclusive);

        /// <summary>Uniform double in [0, 1).</summary>
        double NextDouble();
    }
}
