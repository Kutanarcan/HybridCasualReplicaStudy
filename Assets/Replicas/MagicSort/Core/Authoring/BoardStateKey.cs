using System;

namespace ReplicaProjects.MagicSort
{
    /// <summary>Order-independent hash key of a packed board: bars are sorted, so permuted boards collide on purpose.</summary>
    public readonly struct BoardStateKey : IEquatable<BoardStateKey>
    {
        private readonly ulong[] _bars;
        private readonly int _hash;

        public BoardStateKey(ulong[] bars)
        {
            _bars = (ulong[])bars.Clone();
            Array.Sort(_bars);

            int h = 17;
            foreach (var b in _bars)
                h = h * 31 + b.GetHashCode();
            _hash = h;
        }

        public bool Equals(BoardStateKey other)
        {
            if (_bars.Length != other._bars.Length) return false;
            for (int i = 0; i < _bars.Length; i++)
                if (_bars[i] != other._bars[i]) return false;
            return true;
        }

        public override bool Equals(object obj) => obj is BoardStateKey k && Equals(k);
        public override int GetHashCode() => _hash;
    }
}
