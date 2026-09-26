using System.Collections.Generic;

namespace ReplicaProjects.Arrows
{
    /// <summary>
    /// Line rules: every head has >= 1 line cell; the chain is in-bounds, contiguous, and
    /// direction-consistent from the head; no cell overlaps; no cell sits in its own line of sight.
    /// </summary>
    public class LineValidator
    {
        public void Validate(BoardValidateInput input, List<BoardViolation> violations)
        {
            // Seed occupancy with every head cell so lines can't land on a head.
            var occupied = new HashSet<int>();
            foreach (var head in input.heads)
                occupied.Add(BoardGeometry.ToIndex(head.coordinates, input.width));

            var lineOfSight = new HashSet<int>();
            foreach (var head in input.heads)
            {
                if (head.line == null || head.line.Count == 0)
                {
                    violations.Add(new BoardViolation { coordinates = head.coordinates, reason = "has no line" });
                    continue;
                }

                lineOfSight.Clear();
                BoardGeometry.CollectLineOfSight(head.coordinates, head.direction, input.width, input.height, lineOfSight);

                if (TryFindLineViolation(input, head, lineOfSight, occupied, out var violation))
                    violations.Add(violation);
            }
        }

        // Walks the chain and reports the first broken rule.
        private static bool TryFindLineViolation(BoardValidateInput input, HeadData head, HashSet<int> lineOfSight,
                                                 HashSet<int> occupied, out BoardViolation violation)
        {
            var prev = head.coordinates;
            foreach (var cell in head.line)
            {
                var reason = CellViolation(input, cell, prev, lineOfSight, occupied);
                if (reason != null)
                {
                    violation = new BoardViolation { coordinates = cell.coordinates, reason = reason };
                    return true;
                }

                prev = cell.coordinates;
            }

            violation = default;
            return false;
        }

        private static string CellViolation(BoardValidateInput input, LineCell cell, GridCoord prev,
                                            HashSet<int> lineOfSight, HashSet<int> occupied)
        {
            var coord = cell.coordinates;

            if (!BoardGeometry.InBounds(coord, input.width, input.height))
                return "line cell off-board";

            if ((coord - prev).SqrMagnitude != 1)
                return "line is not contiguous";

            int index = BoardGeometry.ToIndex(coord, input.width);

            if (lineOfSight.Contains(index))
                return "line crosses its own head's line of sight";

            if (!occupied.Add(index))
                return "lines overlap";

            if (cell.direction != (coord - prev).ToDirection())
                return "line direction mismatch";

            return null;
        }
    }
}
