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
        public int height;
        public int width;

        public int[] headIndexArray;
        public Direction[] directionArray;
        public int[] chunkIndexArray;
    }

    public class BoardOrchestrator
    {
        private readonly List<Direction> _directions = new List<Direction>()
        {
            Direction.Up,
            Direction.Down,
            Direction.Left,
            Direction.Right
        };

        private Dictionary<int, int[]> _chunkMap;
        private BoardLogic _boardLogic;
        public BoardDataArrays dataArrays;

        public void Initialize(int width, int height, int headCount)
        {
            _boardLogic = new BoardLogic();
            dataArrays = new BoardDataArrays();
            _chunkMap = new Dictionary<int, int[]>();

            dataArrays.height = height;
            dataArrays.width = width;

            var gridLenght = width * height;

            dataArrays.headIndexArray = _boardLogic.PickRandomArray(new PickRandomArrayInput()
            {
                boardSize = gridLenght,
                headCount = headCount
            });

            dataArrays.directionArray = new Direction[gridLenght];

            //foreach (var headIndex in dataArrays.headIndexArray)
            //{
            //    dataArrays.directionArray[headIndex] = (Direction)Random.Range(1, 5);
            //}

            SelectDirection();
        }

        private void SelectDirection()
        {
            HashSet<Direction> directions = new();

            for (int i = 0; i < dataArrays.headIndexArray.Length; i++)
            {
                directions.Clear();
                directions.AddRange(_directions);

                for (int j = i + 1; j < dataArrays.headIndexArray.Length; j++)
                {

                }
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
