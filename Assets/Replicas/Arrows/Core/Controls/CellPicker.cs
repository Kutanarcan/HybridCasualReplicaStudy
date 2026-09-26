using System;

namespace ReplicaProjects.Arrows
{
    /// <summary>
    /// Maps a world point to the nearest cell (cells sit on integer world coordinates) and accepts it
    /// only inside a circular hit area around the cell centre.
    /// </summary>
    public sealed class CellPicker
    {
        private readonly float _hitRadiusSqr;

        public CellPicker(float hitDiameter)
        {
            float radius = hitDiameter * 0.5f;
            _hitRadiusSqr = radius * radius;
        }

        public bool TryPick(float worldX, float worldY, out GridCoord cell)
        {
            int x = (int)Math.Round(worldX);
            int y = (int)Math.Round(worldY);
            cell = new GridCoord(x, y);

            float dx = worldX - x;
            float dy = worldY - y;
            return dx * dx + dy * dy <= _hitRadiusSqr;
        }
    }
}
