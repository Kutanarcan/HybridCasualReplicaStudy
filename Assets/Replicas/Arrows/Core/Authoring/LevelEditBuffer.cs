using System;
using System.Collections.Generic;

namespace ReplicaProjects.Arrows
{
    /// <summary>
    /// The level editor's in-memory working copy: board size plus a deep-copied head list.
    /// Nothing here touches the asset; the editor writes it back only on a validated save.
    /// </summary>
    public sealed class LevelEditBuffer
    {
        private readonly List<HeadData> _heads = new();

        public int Width { get; private set; } = 5;
        public int Height { get; private set; } = 5;

        // List (not IReadOnlyList) because BoardValidateInput takes one; callers must not mutate it.
        public List<HeadData> Heads => _heads;

        public void Load(int width, int height, IEnumerable<HeadData> heads)
        {
            Width = Math.Max(1, width);
            Height = Math.Max(1, height);
            ReplaceHeads(heads);
        }

        public void ReplaceHeads(IEnumerable<HeadData> heads)
        {
            _heads.Clear();
            foreach (var head in heads)
                _heads.Add(Clone(head));
        }

        public void Clear() => _heads.Clear();

        public List<HeadData> CloneHeads()
        {
            var copy = new List<HeadData>(_heads.Count);
            foreach (var head in _heads)
                copy.Add(Clone(head));
            return copy;
        }

        public bool HasHeadsOutside(int width, int height) =>
            _heads.Exists(head => IsOutside(head.coordinates, width, height));

        // Drops heads outside the new bounds and trims line cells from the first offender onward.
        public void Resize(int width, int height)
        {
            Width = Math.Max(1, width);
            Height = Math.Max(1, height);
            _heads.RemoveAll(head => IsOutside(head.coordinates, Width, Height));

            foreach (var head in _heads)
            {
                if (head.line == null)
                    continue;

                int cut = head.line.FindIndex(c => IsOutside(c.coordinates, Width, Height));
                if (cut >= 0)
                    head.line.RemoveRange(cut, head.line.Count - cut);
            }
        }

        public bool TryGetHeadIndex(GridCoord coord, out int index)
        {
            index = _heads.FindIndex(head => head.coordinates == coord);
            return index >= 0;
        }

        // Finds the head that owns a line cell, returning the head index and the cell's slot in its line.
        public bool TryGetLineOwner(GridCoord coord, out int headIndex, out int cellIndex)
        {
            for (int i = 0; i < _heads.Count; i++)
            {
                int slot = _heads[i].line?.FindIndex(c => c.coordinates == coord) ?? -1;
                if (slot >= 0)
                {
                    headIndex = i;
                    cellIndex = slot;
                    return true;
                }
            }

            headIndex = -1;
            cellIndex = -1;
            return false;
        }

        // In-bounds, not a head, not already part of any line.
        public bool IsCellFree(GridCoord coord) =>
            !IsOutside(coord, Width, Height) && !TryGetHeadIndex(coord, out _) && !TryGetLineOwner(coord, out _, out _);

        // Adds a head, or only changes the direction of an existing one (keeping its line).
        public void StampHead(GridCoord coord, Direction direction)
        {
            if (TryGetHeadIndex(coord, out int i))
            {
                var head = _heads[i];
                head.direction = direction;
                _heads[i] = head;
                return;
            }

            _heads.Add(new HeadData { coordinates = coord, direction = direction, line = new List<LineCell>() });
        }

        public void AppendLineCell(int headIndex, LineCell cell)
        {
            var head = _heads[headIndex];
            head.line ??= new List<LineCell>();
            head.line.Add(cell);
            _heads[headIndex] = head;
        }

        // Removes a whole head (head + its line) or trims a line cell. Returns true if changed.
        public bool RemoveAt(GridCoord coord)
        {
            if (!TryGetHeadIndex(coord, out int headIndex))
                return TrimLineAt(coord);

            _heads.RemoveAt(headIndex);
            return true;
        }

        // Removes a line cell and everything past it on the chain (keeps contiguity).
        public bool TrimLineAt(GridCoord coord)
        {
            if (!TryGetLineOwner(coord, out int headIndex, out int cellIndex))
                return false;

            var line = _heads[headIndex].line;
            line.RemoveRange(cellIndex, line.Count - cellIndex);
            return true;
        }

        private static bool IsOutside(GridCoord coord, int width, int height) =>
            !BoardGeometry.InBounds(coord, width, height);

        private static HeadData Clone(HeadData head) => new()
        {
            coordinates = head.coordinates,
            direction = head.direction,
            line = head.line != null ? new List<LineCell>(head.line) : new List<LineCell>()
        };
    }
}
