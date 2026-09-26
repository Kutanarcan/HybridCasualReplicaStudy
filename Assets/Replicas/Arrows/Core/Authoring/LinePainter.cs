namespace ReplicaProjects.Arrows
{
    /// <summary>
    /// Line brush: click a head to select it, then click free cells adjacent to the chain's tail to
    /// extend its line — never into the head's own line of sight.
    /// </summary>
    public sealed class LinePainter
    {
        private readonly LevelEditBuffer _buffer;

        public GridCoord? SelectedHead { get; private set; }

        public LinePainter(LevelEditBuffer buffer) => _buffer = buffer;

        public void Deselect() => SelectedHead = null;

        /// <summary>Selects a head or extends the selected head's line. Returns true if the level changed.</summary>
        public bool Paint(GridCoord coord)
        {
            if (_buffer.TryGetHeadIndex(coord, out _))
            {
                SelectedHead = coord;
                return false;
            }

            if (!SelectedHead.HasValue || !_buffer.TryGetHeadIndex(SelectedHead.Value, out int headIndex))
                return false;

            var head = _buffer.Heads[headIndex];
            var tail = head.line is { Count: > 0 } ? head.line[^1].coordinates : head.coordinates;

            if ((coord - tail).SqrMagnitude != 1 || !_buffer.IsCellFree(coord) || InOwnLineOfSight(head, coord))
                return false;

            _buffer.AppendLineCell(headIndex, new LineCell { coordinates = coord, direction = (coord - tail).ToDirection() });
            return true;
        }

        /// <summary>Erases a head or trims a line, dropping the selection if its head was erased.</summary>
        public bool Erase(GridCoord coord)
        {
            if (SelectedHead == coord)
                SelectedHead = null;

            return _buffer.RemoveAt(coord);
        }

        public void DropSelectionOutsideBoard()
        {
            if (SelectedHead.HasValue && !BoardGeometry.InBounds(SelectedHead.Value, _buffer.Width, _buffer.Height))
                SelectedHead = null;
        }

        // True if coord lies on the ray from the head, in its direction, to the board edge.
        private bool InOwnLineOfSight(HeadData head, GridCoord coord)
        {
            var step = head.direction.ToOffset();
            if (step == GridCoord.Zero)
                return false;

            for (var c = head.coordinates + step; BoardGeometry.InBounds(c, _buffer.Width, _buffer.Height); c += step)
                if (c == coord)
                    return true;

            return false;
        }
    }
}
