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
            int boardSize = width * height;
            headCount = Mathf.Clamp(headCount, 0, boardSize);

            var indices = PickRandomArray(new PickRandomArrayInput
            {
                boardSize = boardSize,
                headCount = headCount
            });

            var heads = new List<HeadData>(indices.Length);
            var allowed = new HashSet<Direction>();

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

                heads.Add(new HeadData
                {
                    coordinates = coord,
                    direction = PickDirection(allowed, corner.bannedDirections, width, height)
                });
            }

            GrowLines(heads, width, height);

            return heads;
        }

        // Number of extra (random-walk) line cells beyond the mandatory first cell.
        private const int MaxExtraLineCells = 5;

        private void GrowLines(List<HeadData> heads, int width, int height)
        {
            // Cells already taken by any head or any previously grown line.
            var occupied = new HashSet<int>();
            foreach (var h in heads)
                occupied.Add(h.coordinates.y * width + h.coordinates.x);

            for (int i = 0; i < heads.Count; i++)
            {
                var head = heads[i];
                head.line = GrowLine(head.coordinates, head.direction, width, height, occupied);
                heads[i] = head;
            }
        }

        // occupied is mutated as cells are claimed; forbidden = this head's own line of sight.
        private List<Vector2Int> GrowLine(Vector2Int head, Direction direction,
                                          int width, int height, HashSet<int> occupied)
        {
            var line = new List<Vector2Int>();

            var forbidden = new HashSet<int>();
            CollectLineOfSight(head, direction, width, height, forbidden);

            // 1) Mandatory first cell: directly behind the head (never in its forward line of sight).
            var behind = head - direction.ToVector2Int();
            if (!TryClaim(behind, width, height, occupied, forbidden))
                return line; // no room behind -> head gets no line (validation will flag it)

            line.Add(behind);
            var cursor = behind;

            // 2) Random-walk extra cells into free, in-bounds, orthogonal neighbours.
            int extra = Random.Range(0, MaxExtraLineCells + 1);
            for (int i = 0; i < extra; i++)
            {
                if (!TryStepRandom(ref cursor, width, height, occupied, forbidden))
                    break;
                line.Add(cursor);
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

        // Line rules: every head has >= 1 line cell; the chain is in-bounds and contiguous from the
        // head; no cell overlaps another head or line; no cell sits in its own head's line of sight.
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
                    if (!InBounds(cell, width, height))
                    {
                        violations.Add(new BoardViolation { coordinates = cell, reason = "line cell off-board" });
                        break;
                    }

                    if ((cell - prev).sqrMagnitude != 1)
                    {
                        violations.Add(new BoardViolation { coordinates = cell, reason = "line is not contiguous" });
                        break;
                    }

                    int index = cell.y * width + cell.x;

                    if (los.Contains(index))
                    {
                        violations.Add(new BoardViolation { coordinates = cell, reason = "line crosses its own head's line of sight" });
                        break;
                    }

                    if (!occupied.Add(index))
                    {
                        violations.Add(new BoardViolation { coordinates = cell, reason = "lines overlap" });
                        break;
                    }

                    prev = cell;
                }
            }
        }

        private static bool InBounds(Vector2Int coord, int width, int height) =>
            coord.x >= 0 && coord.x < width && coord.y >= 0 && coord.y < height;
    }
}
