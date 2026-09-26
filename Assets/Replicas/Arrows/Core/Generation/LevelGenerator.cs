using System;
using System.Collections.Generic;
using ReplicaProjects.Common;

namespace ReplicaProjects.Arrows
{
    /// <summary>
    /// Solvable-by-construction level generation: arrows are placed one at a time and each new arrow's
    /// forward ray is kept clear of every already-placed arrow. The reverse of placement order is then
    /// always a valid clearing order — when an arrow is removed, the arrows still on the board are
    /// exactly the ones placed before it, which its ray was built to avoid.
    /// </summary>
    public class LevelGenerator
    {
        // BuildLevel passes validation on the first attempt; the retry loop is a cheap safety net only.
        private const int MaxAttempts = 8;

        private readonly IRandomSource _random;
        private readonly LineGrower _lineGrower;
        private readonly BoardValidator _validator = new();
        private readonly Direction[] _directionBuffer = new Direction[4];

        public LevelGenerator(IRandomSource random)
        {
            _random = random;
            _lineGrower = new LineGrower(random);
        }

        public int[] PickRandomArray(PickRandomArrayInput input)
        {
            var pool = new int[input.boardSize];
            var result = new int[input.headCount];

            for (int i = 0; i < input.boardSize; i++)
                pool[i] = i;

            for (int i = 0; i < input.headCount; i++)
            {
                int j = _random.Next(i, input.boardSize);
                (pool[i], pool[j]) = (pool[j], pool[i]);
                result[i] = pool[i];
            }

            return result;
        }

        public List<HeadData> GenerateRandomLevel(int width, int height, int headCount)
        {
            headCount = Math.Clamp(headCount, 0, width * height);

            List<HeadData> heads = null;
            for (int attempt = 0; attempt < MaxAttempts; attempt++)
            {
                heads = BuildLevel(width, height, headCount);

                var result = _validator.ValidateBoard(new BoardValidateInput
                {
                    width = width,
                    height = height,
                    heads = heads
                });

                if (result.isValid)
                    break;
            }

            return heads;
        }

        private List<HeadData> BuildLevel(int width, int height, int headCount)
        {
            // Full shuffle of every cell: each is tried in turn as a candidate head.
            var indices = PickRandomArray(new PickRandomArrayInput
            {
                boardSize = width * height,
                headCount = width * height
            });

            var heads = new List<HeadData>(headCount);
            var occupied = new HashSet<int>();

            foreach (var index in indices)
            {
                if (heads.Count >= headCount)
                    break;

                if (occupied.Contains(index))
                    continue;

                var coord = new GridCoord(index % width, index / width);

                var direction = PickConstructiveDirection(coord, width, height, occupied);
                if (direction == Direction.None)
                    continue;

                occupied.Add(index); // reserve the head cell
                var line = _lineGrower.Grow(coord, direction, width, height, occupied);
                heads.Add(new HeadData { coordinates = coord, direction = direction, line = line });
            }

            return heads;
        }

        // Picks a random direction whose behind cell is free (room for the mandatory first line cell)
        // and whose forward ray to the edge is clear of already-placed arrows. None if no direction
        // qualifies, so the candidate cell is skipped.
        private Direction PickConstructiveDirection(GridCoord coord, int width, int height, HashSet<int> occupied)
        {
            DirectionShuffler.Shuffle(_random, _directionBuffer);

            foreach (var dir in _directionBuffer)
            {
                var behind = coord - dir.ToOffset();
                if (!BoardGeometry.InBounds(behind, width, height) ||
                    occupied.Contains(BoardGeometry.ToIndex(behind, width)))
                    continue;

                if (ForwardRayClear(coord, dir, width, height, occupied))
                    return dir;
            }

            return Direction.None;
        }

        // True when no already-placed arrow occupies the ray from the head to the board edge.
        private static bool ForwardRayClear(GridCoord head, Direction direction,
                                            int width, int height, HashSet<int> occupied)
        {
            var step = direction.ToOffset();
            var c = head + step;
            while (BoardGeometry.InBounds(c, width, height))
            {
                if (occupied.Contains(BoardGeometry.ToIndex(c, width)))
                    return false;
                c += step;
            }

            return true;
        }
    }
}
