using System.Collections.Generic;

namespace ReplicaProjects.Arrows.Tests
{
    /// <summary>Compact head builders for board fixtures.</summary>
    public static class TestHeads
    {
        public static HeadData Head(int x, int y, Direction direction, params (int x, int y)[] line)
        {
            var cells = new List<LineCell>(line.Length);
            var prev = new GridCoord(x, y);
            foreach (var (lx, ly) in line)
            {
                var coord = new GridCoord(lx, ly);
                cells.Add(new LineCell { coordinates = coord, direction = (coord - prev).ToDirection() });
                prev = coord;
            }

            return new HeadData { coordinates = new GridCoord(x, y), direction = direction, line = cells };
        }

        public static List<HeadData> List(params HeadData[] heads) => new(heads);
    }
}
