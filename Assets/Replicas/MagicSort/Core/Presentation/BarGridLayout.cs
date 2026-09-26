namespace ReplicaProjects.MagicSort
{
    /// <summary>
    /// Row-major bar placement shared by the themes: fixed column count, each row drops down (and
    /// optionally forward in depth) so front rows can overlap the ones behind.
    /// </summary>
    public readonly struct BarGridLayout
    {
        public readonly int Columns;
        public readonly float ColumnSpacing;
        public readonly float RowDrop;
        public readonly float RowDepth;

        public BarGridLayout(int columns, float columnSpacing, float rowDrop, float rowDepth)
        {
            Columns = columns;
            ColumnSpacing = columnSpacing;
            RowDrop = rowDrop;
            RowDepth = rowDepth;
        }

        /// <summary>Local position of bar <paramref name="index"/> relative to the bars root.</summary>
        public void Position(int index, out float x, out float y, out float z)
        {
            int column = index % Columns;
            int row = index / Columns;

            x = column * ColumnSpacing;
            y = -row * RowDrop;
            z = -row * RowDepth;
        }
    }
}
