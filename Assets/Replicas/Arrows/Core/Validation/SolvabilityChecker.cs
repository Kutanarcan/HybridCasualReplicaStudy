using System.Collections.Generic;

namespace ReplicaProjects.Arrows
{
    /// <summary>
    /// A head can be removed only when its forward ray (head -> board edge) holds no cell still owned
    /// by a present head. Removing a chunk only frees cells, so greedily removing every currently-clear
    /// head to a fixpoint reaches the same set regardless of order. Any head that survives the fixpoint
    /// is part of a deadlock (e.g. two heads each in the other's ray).
    /// </summary>
    public class SolvabilityChecker
    {
        public void Validate(BoardValidateInput input, List<BoardViolation> violations)
        {
            var heads = input.heads;
            if (heads == null || heads.Count == 0)
                return;

            var owner = BuildOwnerMap(heads, input.width, input.height);
            var removed = new bool[heads.Count];

            bool changed = true;
            while (changed)
            {
                changed = false;
                for (int i = 0; i < heads.Count; i++)
                {
                    if (removed[i] || !IsRayClear(i, heads[i], owner, removed, input.width, input.height))
                        continue;

                    removed[i] = true;
                    changed = true;
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

        // owner[index] = list-index of the head occupying the cell, or -1 if empty.
        private static int[] BuildOwnerMap(List<HeadData> heads, int width, int height)
        {
            var owner = new int[width * height];
            for (int i = 0; i < owner.Length; i++)
                owner[i] = -1;

            for (int i = 0; i < heads.Count; i++)
            {
                var head = heads[i];
                owner[BoardGeometry.ToIndex(head.coordinates, width)] = i;

                if (head.line == null)
                    continue;

                foreach (var cell in head.line)
                    owner[BoardGeometry.ToIndex(cell.coordinates, width)] = i;
            }

            return owner;
        }

        // True when no still-present head occupies the ray from this head to the board edge. The head's
        // own cells are never in front of it (self line-of-sight rule), but they're excluded anyway.
        private static bool IsRayClear(int headIndex, HeadData head, int[] owner, bool[] removed,
                                       int width, int height)
        {
            var step = head.direction.ToOffset();
            if (step == GridCoord.Zero)
                return false; // a head with no direction can never be removed

            var c = head.coordinates + step;
            while (BoardGeometry.InBounds(c, width, height))
            {
                int o = owner[BoardGeometry.ToIndex(c, width)];
                if (o != -1 && o != headIndex && !removed[o])
                    return false;

                c += step;
            }

            return true;
        }
    }
}
