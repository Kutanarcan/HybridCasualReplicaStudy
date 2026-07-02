using System.Collections.Generic;

namespace BallSortDesigner
{
    public sealed class Bar
    {
        private readonly List<int> _balls;

        public int Capacity { get; }

        public Bar(int capacity)
        {
            Capacity = capacity;
            _balls = new List<int>(capacity);
        }

        public Bar(int capacity, IEnumerable<int> balls)
        {
            Capacity = capacity;
            _balls = new List<int>(balls);
        }

        public int Count => _balls.Count;
        public bool IsEmpty => _balls.Count == 0;
        public bool IsFull => _balls.Count >= Capacity;

        public int this[int slot] => _balls[slot];

        public int Top => _balls.Count > 0 ? _balls[_balls.Count - 1] : -1;

        public bool IsUniform
        {
            get
            {
                for (int i = 1; i < _balls.Count; i++)
                    if (_balls[i] != _balls[0]) return false;
                return true;
            }
        }

        public bool IsComplete => _balls.Count == Capacity && IsUniform;

        public void Push(int color) => _balls.Add(color);

        public int Pop()
        {
            int top = _balls[_balls.Count - 1];
            _balls.RemoveAt(_balls.Count - 1);
            return top;
        }

        public Bar Clone() => new Bar(Capacity, _balls);
    }
}
