using System.Collections.Generic;

namespace ReplicaProjects.Arrows
{
    /// <summary>
    /// Validates a whole board: head facing rules, then line structure, then solvability.
    /// Solvability only runs on a structurally sound board (cell ownership must be well-defined).
    /// </summary>
    public class BoardValidator
    {
        private readonly BoardRules _rules = new();
        private readonly LineValidator _lines = new();
        private readonly SolvabilityChecker _solvability = new();

        public BoardValidateResult ValidateBoard(BoardValidateInput input)
        {
            var violations = new List<BoardViolation>();

            if (input.heads != null)
            {
                for (int i = 0; i < input.heads.Count; i++)
                    ValidateHead(input, i, violations);

                _lines.Validate(input, violations);

                if (violations.Count == 0)
                    _solvability.Validate(input, violations);
            }

            return new BoardValidateResult
            {
                isValid = violations.Count == 0,
                violations = violations
            };
        }

        private void ValidateHead(BoardValidateInput input, int index, List<BoardViolation> violations)
        {
            var head = input.heads[index];

            if (head.direction == Direction.None)
            {
                violations.Add(Violation(head.coordinates, "has no direction"));
                return;
            }

            var corner = _rules.EvaluateCorner(new BoardCornerEvaluateInput
            {
                coordinates = head.coordinates,
                width = input.width,
                height = input.height
            });

            if (corner.bannedDirections.Contains(head.direction))
            {
                violations.Add(Violation(head.coordinates, "faces off-board"));
                return;
            }

            if (TryFindFacingConflict(input.heads, index, out var other))
                violations.Add(Violation(head.coordinates, $"is facing wrong direction toward {other}"));
        }

        private bool TryFindFacingConflict(List<HeadData> heads, int index, out GridCoord other)
        {
            var head = heads[index];

            for (int j = 0; j < heads.Count; j++)
            {
                if (j == index)
                    continue;

                var result = _rules.EvaluateIntersection(new BoardIntersectionEvaluateInput
                {
                    coordinateA = head.coordinates,
                    coordinateB = heads[j].coordinates,
                    directionB = heads[j].direction
                });

                if (result.HasIntersection && result.bannedDirections.Contains(head.direction))
                {
                    other = heads[j].coordinates;
                    return true;
                }
            }

            other = default;
            return false;
        }

        private static BoardViolation Violation(GridCoord coordinates, string reason) =>
            new() { coordinates = coordinates, reason = reason };
    }
}
