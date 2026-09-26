using System;

namespace ReplicaProjects.Common
{
    /// <summary>Index into a looping level list.</summary>
    public sealed class LevelCursor
    {
        private readonly int _count;

        public int Current { get; private set; }

        public LevelCursor(int count)
        {
            if (count <= 0)
                throw new ArgumentOutOfRangeException(nameof(count), "level list must not be empty");

            _count = count;
        }

        public int Advance() => Current = (Current + 1) % _count;
    }
}
