using System.Collections.Generic;
using ReplicaProjects.Common;

namespace ReplicaProjects.Arrows
{
    /// <summary>Grows an arrow's line: the mandatory cell behind the head, then a random walk.</summary>
    public class LineGrower
    {
        // Number of extra (random-walk) line cells beyond the mandatory first cell.
        private const int MaxExtraLineCells = 15;

        private readonly IRandomSource _random;
        private readonly Direction[] _directionBuffer = new Direction[4];
        private readonly HashSet<int> _forbidden = new();

        public LineGrower(IRandomSource random) => _random = random;

        // occupied is mutated as the behind cell and extra cells are claimed; forbidden = this head's
        // own line of sight (the first cell is behind the head, never in its forward ray).
        public List<LineCell> Grow(GridCoord head, Direction direction, int width, int height, HashSet<int> occupied)
        {
            var line = new List<LineCell>();
            if (direction == Direction.None)
                return line;

            // Mandatory first cell: directly behind the head. The generator guarantees it is in-bounds
            // and free, and (being behind) it is never in the forward line of sight.
            var behind = head - direction.ToOffset();
            occupied.Add(BoardGeometry.ToIndex(behind, width)); // claim it before random-walking further
            line.Add(new LineCell { coordinates = behind, direction = direction.Opposite() });
            var cursor = behind;

            _forbidden.Clear();
            BoardGeometry.CollectLineOfSight(head, direction, width, height, _forbidden);

            int extra = _random.Next(0, MaxExtraLineCells + 1);
            for (int i = 0; i < extra; i++)
            {
                var prev = cursor;
                if (!TryStepRandom(ref cursor, width, height, occupied))
                    break;
                line.Add(new LineCell { coordinates = cursor, direction = (cursor - prev).ToDirection() });
            }

            return line;
        }

        // Shuffle the four directions, take the first allowed neighbour.
        private bool TryStepRandom(ref GridCoord cursor, int width, int height, HashSet<int> occupied)
        {
            DirectionShuffler.Shuffle(_random, _directionBuffer);

            foreach (var d in _directionBuffer)
            {
                var next = cursor + d.ToOffset();
                if (TryClaim(next, width, height, occupied))
                {
                    cursor = next;
                    return true;
                }
            }

            return false;
        }

        private bool TryClaim(GridCoord c, int width, int height, HashSet<int> occupied)
        {
            if (!BoardGeometry.InBounds(c, width, height))
                return false;

            int index = BoardGeometry.ToIndex(c, width);
            if (_forbidden.Contains(index)) // would cross own head's line of sight
                return false;

            return occupied.Add(index); // false if already taken
        }
    }
}
