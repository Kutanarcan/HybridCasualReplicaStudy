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

            // BuildLevel is solvable-by-construction, so it passes ValidateBoard on the first attempt;
            // the loop is a cheap safety net only.
            const int maxAttempts = 8;
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

        // Solvable-by-construction: arrows are placed one at a time and each new arrow's forward ray is
        // kept clear of every already-placed arrow. The reverse of placement order is then always a
        // valid clearing order — when an arrow is removed, the arrows still on the board are exactly the
        // ones placed before it, which its ray was built to avoid; arrows placed after it are already
        // gone. So every arrow is removable at its turn, with no reroll or search needed.
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

                var coord = new Vector2Int(index % width, index / width);

                var direction = PickConstructiveDirection(coord, width, height, occupied);
                if (direction == Direction.None)
                    continue;

                occupied.Add(index); // reserve the head cell
                var line = GrowLine(coord, direction, width, height, occupied);
                heads.Add(new HeadData { coordinates = coord, direction = direction, line = line });
            }

            return heads;
        }

        // Number of extra (random-walk) line cells beyond the mandatory first cell.
        private const int MaxExtraLineCells = 15;

        // Picks a random direction whose behind cell is free (room for the mandatory first line cell)
        // and whose forward ray to the edge is clear of already-placed arrows. None if no direction
        // qualifies, so the candidate cell is skipped.
        private Direction PickConstructiveDirection(Vector2Int coord, int width, int height,
                                                    HashSet<int> occupied)
        {
            var dirs = new[] { Direction.Up, Direction.Down, Direction.Left, Direction.Right };
            for (int i = dirs.Length - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (dirs[i], dirs[j]) = (dirs[j], dirs[i]);
            }

            foreach (var dir in dirs)
            {
                var behind = coord - dir.ToVector2Int();
                if (!InBounds(behind, width, height) || occupied.Contains(behind.y * width + behind.x))
                    continue;

                if (ForwardRayClear(coord, dir, width, height, occupied))
                    return dir;
            }

            return Direction.None;
        }

        // True when no already-placed arrow occupies the ray from the head to the board edge.
        private static bool ForwardRayClear(Vector2Int head, Direction direction,
                                            int width, int height, HashSet<int> occupied)
        {
            var step = direction.ToVector2Int();
            var c = head + step;
            while (InBounds(c, width, height))
            {
                if (occupied.Contains(c.y * width + c.x))
                    return false;
                c += step;
            }

            return true;
        }

        // occupied is mutated as the behind cell and extra cells are claimed; forbidden = this head's
        // own line of sight (the first cell is behind the head, never in its forward ray).
        private List<LineCell> GrowLine(Vector2Int head, Direction direction,
                                        int width, int height, HashSet<int> occupied)
        {
            var line = new List<LineCell>();
            if (direction == Direction.None)
                return line;

            // Mandatory first cell: directly behind the head. PickConstructiveDirection guarantees it
            // is in-bounds and free, and (being behind) it is never in the forward line of sight.
            var behind = head - direction.ToVector2Int();
            occupied.Add(behind.y * width + behind.x); // claim it before random-walking further
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

                // Only meaningful once the structure is sound (cell ownership is well-defined).
                if (violations.Count == 0)
                    ValidateSolvability(input, violations);
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

        // Solvability: a head can be removed only when its forward ray (head -> board edge) holds no
        // cell still owned by a present head. Removing a chunk only frees cells, so greedily removing
        // every currently-clear head to a fixpoint reaches the same set regardless of order. Any head
        // that survives the fixpoint is part of a deadlock (e.g. two heads each in the other's ray).
        private void ValidateSolvability(BoardValidateInput input, List<BoardViolation> violations)
        {
            var heads = input.heads;
            int width = input.width;
            int height = input.height;

            if (heads == null || heads.Count == 0)
                return;

            // owner[index] = list-index of the head occupying the cell, or -1 if empty.
            var owner = new int[width * height];
            for (int i = 0; i < owner.Length; i++)
                owner[i] = -1;

            for (int i = 0; i < heads.Count; i++)
            {
                var head = heads[i];
                owner[head.coordinates.y * width + head.coordinates.x] = i;

                if (head.line == null)
                    continue;

                foreach (var cell in head.line)
                    owner[cell.coordinates.y * width + cell.coordinates.x] = i;
            }

            var removed = new bool[heads.Count];
            bool changed = true;
            while (changed)
            {
                changed = false;
                for (int i = 0; i < heads.Count; i++)
                {
                    if (removed[i])
                        continue;

                    if (IsRayClear(i, heads[i], owner, removed, width, height))
                    {
                        removed[i] = true;
                        changed = true;
                    }
                }
            }

            for (int i = 0; i < heads.Count; i++)
                if (!removed[i])
                    violations.Add(new BoardViolation
                    {
                        coordinates = heads[i].coordinates,
                        reason = "deadlocked: path can never clear"
                    });
        }

        // True when no still-present head occupies the ray from this head to the board edge. The head's
        // own cells are never in front of it (self line-of-sight rule), but they're excluded anyway.
        private static bool IsRayClear(int headIndex, HeadData head, int[] owner, bool[] removed,
                                       int width, int height)
        {
            var step = head.direction.ToVector2Int();
            if (step == Vector2Int.zero)
                return false; // a head with no direction can never be removed

            var c = head.coordinates + step;
            while (InBounds(c, width, height))
            {
                int o = owner[c.y * width + c.x];
                if (o != -1 && o != headIndex && !removed[o])
                    return false;

                c += step;
            }

            return true;
        }

        private static bool InBounds(Vector2Int coord, int width, int height) =>
            coord.x >= 0 && coord.x < width && coord.y >= 0 && coord.y < height;
    }
}
