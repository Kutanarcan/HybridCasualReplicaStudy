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

            return heads;
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
            }

            return new BoardValidateResult
            {
                isValid = violations.Count == 0,
                violations = violations
            };
        }

        private static bool InBounds(Vector2Int coord, int width, int height) =>
            coord.x >= 0 && coord.x < width && coord.y >= 0 && coord.y < height;
    }
}
