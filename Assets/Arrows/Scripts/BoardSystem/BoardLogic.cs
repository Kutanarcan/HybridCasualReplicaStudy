using System.Collections.Generic;
using UnityEngine;

namespace ReplicaProjects.Arrows
{
    public class BoardLogic
    {
        private static readonly Direction[] AllDirections =
        {
            Direction.Up,
            Direction.Down,
            Direction.Left,
            Direction.Right
        };

        public BoardLogic()
        {
        }

        public int[] PickRandomArray(PickRandomArrayInput input)
        {
            var pool = new int[input.boardSize];
            var result = new int[input.headCount];

            for (int i = 0; i < input.boardSize; i++)
                pool[i] = i;

            for (int i = 0; i < input.headCount; i++)
            {
                int j = Random.Range(i, input.boardSize);
                (pool[i], pool[j]) = (pool[j], pool[i]);
                result[i] = pool[i];
            }

            return result;
        }

        public BoardCornerEvaluateResult EvaluateCorner(BoardCornerEvaluateInput input)
        {
            var banned = new List<Direction>();

            foreach (var dir in AllDirections)
            {
                var behind = input.coordinates - dir.ToVector2Int();
                if (!InBounds(behind, input.width, input.height))
                    banned.Add(dir);
            }

            return new BoardCornerEvaluateResult
            {
                IsAtCorner = banned.Count > 0,
                bannedDirections = banned
            };
        }

        public BoardIntersectionEvaluateResult EvaluateIntersection(BoardIntersectionEvaluateInput input)
        {
            var banned = new List<Direction>();
            var delta = input.coordinateB - input.coordinateA;

            if (delta.x != 0 && delta.y != 0)
            {
                return new BoardIntersectionEvaluateResult
                {
                    HasIntersection = false,
                    bannedDirections = banned
                };
            }

            var step = new Vector2Int(
                delta.x == 0 ? 0 : (delta.x > 0 ? 1 : -1),
                delta.y == 0 ? 0 : (delta.y > 0 ? 1 : -1));

            var dirAtoB = step.ToDirection();
            var dirBtoA = (-step).ToDirection();
            bool adjacent = Mathf.Abs(delta.x) + Mathf.Abs(delta.y) == 1;

            if (input.directionB == dirBtoA)
                banned.Add(dirAtoB);

            if (adjacent && input.directionB == dirAtoB)
                banned.Add(dirBtoA);

            return new BoardIntersectionEvaluateResult
            {
                HasIntersection = true,
                bannedDirections = banned
            };
        }

        public List<HeadData> GenerateRandomLevel(int width, int height, int headCount)
        {
            headCount = Mathf.Clamp(headCount, 0, width * height);

            // Most layouts succeed on the first try; the retry guards the rare case where a head
            // can't be given a valid direction with a free cell behind it for its first line.
            const int maxAttempts = 30;
            List<HeadData> heads = null;
            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                heads = BuildLevel(width, height, headCount);

                var result = ValidateBoard(new BoardValidateInput
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
            var indices = PickRandomArray(new PickRandomArrayInput
            {
                boardSize = width * height,
                headCount = headCount
            });

            var heads = new List<HeadData>(indices.Length);
            var allowed = new HashSet<Direction>();

            // Every head and every cell reserved as a "behind" first-line cell. Seeded with all head
            // positions up front so a head is never placed onto another head's required first cell.
            var occupied = new HashSet<int>();
            foreach (var index in indices)
                occupied.Add(index);

            for (int i = 0; i < indices.Length; i++)
            {
                var coord = new Vector2Int(indices[i] % width, indices[i] / width);

                allowed.Clear();
                allowed.AddRange(AllDirections);

                var corner = EvaluateCorner(new BoardCornerEvaluateInput
                {
                    coordinates = coord,
                    width = width,
                    height = height
                });
                allowed.RemoveRange(corner.bannedDirections);

                for (int j = 0; j < heads.Count; j++)
                {
                    var result = EvaluateIntersection(new BoardIntersectionEvaluateInput
                    {
                        coordinateA = coord,
                        coordinateB = heads[j].coordinates,
                        directionB = heads[j].direction
                    });

                    if (result.HasIntersection)
                        allowed.RemoveRange(result.bannedDirections);
                }

                // A head needs a free cell directly behind it for its mandatory first line cell.
                RemoveBlockedBehindDirections(allowed, coord, width, height, occupied);

                var direction = PickDirection(allowed, corner.bannedDirections, width, height);
                heads.Add(new HeadData { coordinates = coord, direction = direction });

                // Reserve the behind cell so later heads and lines can't take it.
                if (direction != Direction.None)
                {
                    var behind = coord - direction.ToVector2Int();
                    occupied.Add(behind.y * width + behind.x);
                }
            }

            GrowLines(heads, width, height, occupied);

            return heads;
        }

        // Number of extra (random-walk) line cells beyond the mandatory first cell.
        private const int MaxExtraLineCells = 15;

        // Bans directions whose cell directly behind the head is off-board or already taken.
        private static void RemoveBlockedBehindDirections(HashSet<Direction> allowed, Vector2Int coord,
                                                          int width, int height, HashSet<int> occupied)
        {
            var blocked = new List<Direction>();
            foreach (var dir in allowed)
            {
                var behind = coord - dir.ToVector2Int();
                if (!InBounds(behind, width, height) || occupied.Contains(behind.y * width + behind.x))
                    blocked.Add(dir);
            }

            allowed.RemoveRange(blocked);
        }

        // occupied already holds every head + each head's reserved behind cell.
        private void GrowLines(List<HeadData> heads, int width, int height, HashSet<int> occupied)
        {
            for (int i = 0; i < heads.Count; i++)
            {
                var head = heads[i];
                head.line = GrowLine(head.coordinates, head.direction, width, height, occupied);
                heads[i] = head;
            }
        }

        // occupied is mutated as extra cells are claimed; forbidden = this head's own line of sight.
        private List<LineCell> GrowLine(Vector2Int head, Direction direction,
                                        int width, int height, HashSet<int> occupied)
        {
            var line = new List<LineCell>();
            if (direction == Direction.None)
                return line;

            // Mandatory first cell: directly behind the head. It was reserved during placement, so
            // it is guaranteed in-bounds, free, and (being behind) never in the forward line of sight.
            var behind = head - direction.ToVector2Int();
            line.Add(new LineCell { coordinates = behind, direction = direction.Opposite() });
            var cursor = behind;

            var forbidden = new HashSet<int>();
            CollectLineOfSight(head, direction, width, height, forbidden);

            // Random-walk extra cells into free, in-bounds, orthogonal neighbours.
            int extra = Random.Range(0, MaxExtraLineCells + 1);
            for (int i = 0; i < extra; i++)
            {
                var prev = cursor;
                if (!TryStepRandom(ref cursor, width, height, occupied, forbidden))
                    break;
                line.Add(new LineCell { coordinates = cursor, direction = (cursor - prev).ToDirection() });
            }

            return line;
        }

        private static bool TryClaim(Vector2Int c, int width, int height,
                                     HashSet<int> occupied, HashSet<int> forbidden)
        {
            if (!InBounds(c, width, height))
                return false;

            int index = c.y * width + c.x;
            if (forbidden.Contains(index)) // would cross own head's line of sight
                return false;

            return occupied.Add(index); // false if already taken
        }

        private static bool TryStepRandom(ref Vector2Int cursor, int width, int height,
                                          HashSet<int> occupied, HashSet<int> forbidden)
        {
            // Shuffle the four directions, take the first allowed neighbour.
            var dirs = new[] { Direction.Up, Direction.Down, Direction.Left, Direction.Right };
            for (int i = dirs.Length - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (dirs[i], dirs[j]) = (dirs[j], dirs[i]);
            }

            foreach (var d in dirs)
            {
                var next = cursor + d.ToVector2Int();
                if (TryClaim(next, width, height, occupied, forbidden))
                {
                    cursor = next;
                    return true;
                }
            }

            return false;
        }

        // Every cell from the head, stepping in its direction, to the board edge (excludes the head).
        private static void CollectLineOfSight(Vector2Int from, Direction direction,
                                               int width, int height, HashSet<int> into)
        {
            var step = direction.ToVector2Int();
            if (step == Vector2Int.zero)
                return;

            var c = from + step;
            while (InBounds(c, width, height))
            {
                into.Add(c.y * width + c.x);
                c += step;
            }
        }

        private Direction PickDirection(HashSet<Direction> allowed, List<Direction> hardBanned, int width, int height)
        {
            if (allowed.Count == 0)
            {
                allowed.AddRange(AllDirections);
                allowed.RemoveRange(hardBanned);
                if (allowed.Count == 0)
                    return Direction.None;
            }

            int pick = Random.Range(0, allowed.Count);
            foreach (var dir in allowed)
            {
                if (pick-- == 0)
                    return dir;
            }

            return Direction.None;
        }

        public BoardValidateResult ValidateBoard(BoardValidateInput input)
        {
            var violations = new List<BoardViolation>();
            var heads = input.heads;

            if (heads != null)
            {
                for (int i = 0; i < heads.Count; i++)
                {
                    var head = heads[i];

                    if (head.direction == Direction.None)
                    {
                        violations.Add(new BoardViolation
                        {
                            coordinates = head.coordinates,
                            reason = "has no direction"
                        });
                        continue;
                    }

                    var corner = EvaluateCorner(new BoardCornerEvaluateInput
                    {
                        coordinates = head.coordinates,
                        width = input.width,
                        height = input.height
                    });

                    if (corner.bannedDirections.Contains(head.direction))
                    {
                        violations.Add(new BoardViolation
                        {
                            coordinates = head.coordinates,
                            reason = "faces off-board"
                        });
                        continue;
                    }

                    for (int j = 0; j < heads.Count; j++)
                    {
                        if (j == i)
                            continue;

                        var other = heads[j];
                        var result = EvaluateIntersection(new BoardIntersectionEvaluateInput
                        {
                            coordinateA = head.coordinates,
                            coordinateB = other.coordinates,
                            directionB = other.direction
                        });

                        if (result.HasIntersection && result.bannedDirections.Contains(head.direction))
                        {
                            violations.Add(new BoardViolation
                            {
                                coordinates = head.coordinates,
                                reason = $"is facing wrong direction toward {other.coordinates}"
                            });
                            break;
                        }
                    }
                }

                ValidateLines(input, violations);
            }

            return new BoardValidateResult
            {
                isValid = violations.Count == 0,
                violations = violations
            };
        }

        // Line rules: every head has >= 1 line cell; the chain is in-bounds, contiguous, and
        // direction-consistent from the head; no cell overlaps; no cell sits in its own LOS.
        private void ValidateLines(BoardValidateInput input, List<BoardViolation> violations)
        {
            var heads = input.heads;
            int width = input.width;
            int height = input.height;

            // Seed occupancy with every head cell so lines can't land on a head.
            var occupied = new HashSet<int>();
            foreach (var h in heads)
                occupied.Add(h.coordinates.y * width + h.coordinates.x);

            foreach (var head in heads)
            {
                if (head.line == null || head.line.Count == 0)
                {
                    violations.Add(new BoardViolation
                    {
                        coordinates = head.coordinates,
                        reason = "has no line"
                    });
                    continue;
                }

                var los = new HashSet<int>();
                CollectLineOfSight(head.coordinates, head.direction, width, height, los);

                var prev = head.coordinates;
                foreach (var cell in head.line)
                {
                    var coord = cell.coordinates;

                    if (!InBounds(coord, width, height))
                    {
                        violations.Add(new BoardViolation { coordinates = coord, reason = "line cell off-board" });
                        break;
                    }

                    if ((coord - prev).sqrMagnitude != 1)
                    {
                        violations.Add(new BoardViolation { coordinates = coord, reason = "line is not contiguous" });
                        break;
                    }

                    int index = coord.y * width + coord.x;

                    if (los.Contains(index))
                    {
                        violations.Add(new BoardViolation { coordinates = coord, reason = "line crosses its own head's line of sight" });
                        break;
                    }

                    if (!occupied.Add(index))
                    {
                        violations.Add(new BoardViolation { coordinates = coord, reason = "lines overlap" });
                        break;
                    }

                    var expectedDir = (coord - prev).ToDirection();
                    if (cell.direction != expectedDir)
                    {
                        violations.Add(new BoardViolation { coordinates = coord, reason = "line direction mismatch" });
                        break;
                    }

                    prev = coord;
                }
            }
        }

        private static bool InBounds(Vector2Int coord, int width, int height) =>
            coord.x >= 0 && coord.x < width && coord.y >= 0 && coord.y < height;
    }
}
