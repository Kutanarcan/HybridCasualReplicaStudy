using System.Collections.Generic;

namespace ReplicaProjects.Arrows
{
    /// <summary>Per-head placement rules: board edges and facing relative to another head.</summary>
    public class BoardRules
    {
        private static readonly Direction[] AllDirections =
        {
            Direction.Up,
            Direction.Down,
            Direction.Left,
            Direction.Right
        };

        public BoardCornerEvaluateResult EvaluateCorner(BoardCornerEvaluateInput input)
        {
            var banned = new List<Direction>();

            foreach (var dir in AllDirections)
            {
                var behind = input.coordinates - dir.ToOffset();
                if (!BoardGeometry.InBounds(behind, input.width, input.height))
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

            var step = new GridCoord(System.Math.Sign(delta.x), System.Math.Sign(delta.y));

            var dirAtoB = step.ToDirection();
            var dirBtoA = (-step).ToDirection();
            bool adjacent = System.Math.Abs(delta.x) + System.Math.Abs(delta.y) == 1;

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
    }
}
