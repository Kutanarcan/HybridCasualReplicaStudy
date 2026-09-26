namespace ReplicaProjects.Arrows.EditorTools
{
    /// <summary>Applies grid clicks to the working board according to the active brush.</summary>
    public sealed class LevelPaintTool
    {
        private readonly LevelEditBuffer _buffer;
        private readonly LevelBrushBar _brushBar;

        public LinePainter Painter { get; }

        public LevelPaintTool(LevelEditBuffer buffer, LevelBrushBar brushBar)
        {
            _buffer = buffer;
            _brushBar = brushBar;
            Painter = new LinePainter(buffer);
            _brushBar.ModeChanged += OnModeChanged;
        }

        /// <summary>Returns true if the level changed.</summary>
        public bool Click(GridCoord coord, int button) => button == 1 ? EraseAt(coord) : PaintAt(coord);

        // Leaving line mode drops the selected head.
        private void OnModeChanged(BrushMode mode)
        {
            if (mode != BrushMode.Line)
                Painter.Deselect();
        }

        // Right-click: trims a line in Line mode, otherwise erases.
        private bool EraseAt(GridCoord coord) =>
            _brushBar.Mode == BrushMode.Line ? _buffer.TrimLineAt(coord) : Painter.Erase(coord);

        private bool PaintAt(GridCoord coord)
        {
            switch (_brushBar.Mode)
            {
                case BrushMode.Direction: _buffer.StampHead(coord, _brushBar.Brush); return true;
                case BrushMode.Erase: return Painter.Erase(coord);
                case BrushMode.Line: return Painter.Paint(coord);
                default: return false;
            }
        }
    }
}
