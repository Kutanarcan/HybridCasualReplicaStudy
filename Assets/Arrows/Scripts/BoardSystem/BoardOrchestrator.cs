using System.Collections.Generic;
using UnityEngine;

namespace ReplicaProjects.Arrows
{
    public enum Direction
    {
        None,
        Up,
        Down,
        Left,
        Right
    }

    public struct BoardDataArrays
    {
        public int[] headIndexArray;
        public Direction[] directionArray;
        public int[] chunkIndexArray;
    }

    public class BoardOrchestrator
    {
        public event System.Action NoHeadLeft;

        private Dictionary<int, int[]> _chunkMap;
        private BoardLogic _boardLogic;
        public BoardDataArrays dataArrays;

        public void Initialize(LevelData level)
        {
            _boardLogic = new BoardLogic();
            _chunkMap = new Dictionary<int, int[]>();

            BuildFromHeads(level.width, level.height, level.heads);
        }

        public void Initialize(int width, int height, int headCount)
        {
            _boardLogic = new BoardLogic();
            _chunkMap = new Dictionary<int, int[]>();

            var heads = _boardLogic.GenerateRandomLevel(width, height, headCount);
            BuildFromHeads(width, height, heads);
        }

        private void BuildFromHeads(int width, int height, List<HeadData> heads)
        {
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
            {
                var head = heads[i];
                int headIndex = head.coordinates.y * width + head.coordinates.x;

                dataArrays.headIndexArray[i] = headIndex;
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
        }

        public void DeInitialize()
        {

        }

        // Resolves any cell (head or line) to its owning head's flat index, or -1 if empty.
        public int GetHeadIndex(int cellIndex)
        {
            if (cellIndex < 0 || cellIndex >= dataArrays.chunkIndexArray.Length)
                return -1;

            return dataArrays.chunkIndexArray[cellIndex];
        }

        public int[] GetLineChuck(int headIndex)
        {
            if (_chunkMap.TryGetValue(headIndex, out var chunk))
            {
                return chunk;
            }

            return null;
        }

        public void RemoveChuck(int headIndex)
        {
            _chunkMap.Remove(headIndex);

            if (_chunkMap.Count > 0)
                return;

            NoHeadLeft?.Invoke();
        }
    }
}
