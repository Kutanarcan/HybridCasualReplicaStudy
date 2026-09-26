using System.Collections.Generic;

namespace ReplicaProjects.Arrows
{
    /// <summary>Flat, index-based arrow ownership: which head owns each cell, and each head's line chunk.</summary>
    public class BoardOrchestrator
    {
        private readonly Dictionary<int, int[]> _chunkMap = new();
        public BoardDataArrays dataArrays;

        public int RemainingCount => _chunkMap.Count;

        public void Initialize(int width, int height, IReadOnlyList<HeadData> heads)
        {
            _chunkMap.Clear();
            int size = width * height;

            dataArrays = new BoardDataArrays
            {
                headIndexArray = new int[heads.Count],
                directionArray = new Direction[size],
                chunkIndexArray = new int[size],
            };

            // -1 = cell belongs to no arrow.
            for (int i = 0; i < size; i++)
                dataArrays.chunkIndexArray[i] = -1;

            for (int i = 0; i < heads.Count; i++)
                AddHead(i, heads[i], width);
        }

        private void AddHead(int order, HeadData head, int width)
        {
            int headIndex = head.coordinates.y * width + head.coordinates.x;

            dataArrays.headIndexArray[order] = headIndex;
            dataArrays.directionArray[headIndex] = head.direction;
            dataArrays.chunkIndexArray[headIndex] = headIndex; // head points to itself

            int lineCount = head.line?.Count ?? 0;
            var cells = new int[lineCount];

            for (int c = 0; c < lineCount; c++)
            {
                var lineCell = head.line[c];
                int cellIndex = lineCell.coordinates.y * width + lineCell.coordinates.x;

                cells[c] = cellIndex;
                dataArrays.chunkIndexArray[cellIndex] = headIndex; // line cell -> owning head
                dataArrays.directionArray[cellIndex] = lineCell.direction;
            }

            _chunkMap.Add(headIndex, cells);
        }

        public void DeInitialize() => _chunkMap.Clear();

        // Resolves any cell (head or line) to its owning head's flat index, or -1 if empty.
        public int GetHeadIndex(int cellIndex)
        {
            if (cellIndex < 0 || cellIndex >= dataArrays.chunkIndexArray.Length)
                return -1;

            return dataArrays.chunkIndexArray[cellIndex];
        }

        public int[] GetLineChunk(int headIndex) =>
            _chunkMap.TryGetValue(headIndex, out var chunk) ? chunk : null;

        public void RemoveChunk(int headIndex) => _chunkMap.Remove(headIndex);
    }
}
