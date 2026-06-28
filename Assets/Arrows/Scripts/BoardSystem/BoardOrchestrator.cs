using System.Collections.Generic;

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
        private Dictionary<int, int[]> _chunkMap;
        private BoardLogic _boardLogic;
        public BoardDataArrays dataArrays;

        public void Initialize(LevelData level)
        {
            _boardLogic = new BoardLogic();
            _chunkMap = new Dictionary<int, int[]>();

            BuildFromHeads(level.width, level.height, level.heads);
        }

        // Runtime / editor randomizer: generate rule-valid heads, then build the board.
        public void Initialize(int width, int height, int headCount)
        {
            _boardLogic = new BoardLogic();
            _chunkMap = new Dictionary<int, int[]>();

            var heads = _boardLogic.GenerateRandomLevel(width, height, headCount);
            BuildFromHeads(width, height, heads);
        }

        private void BuildFromHeads(int width, int height, List<HeadData> heads)
        {
            dataArrays = new BoardDataArrays
            {
                headIndexArray = new int[heads.Count],
                directionArray = new Direction[width * height]
            };

            for (int i = 0; i < heads.Count; i++)
            {
                var head = heads[i];
                int index = head.coordinates.y * width + head.coordinates.x;

                dataArrays.headIndexArray[i] = index;
                dataArrays.directionArray[index] = head.direction;
            }
        }

        public void DeInitialize()
        {

        }

        public int[] GetLineChuck(int headIndex)
        {
            if (_chunkMap.TryGetValue(headIndex, out var chunk))
            {
                return chunk;
            }

            return null;
        }
    }
}
