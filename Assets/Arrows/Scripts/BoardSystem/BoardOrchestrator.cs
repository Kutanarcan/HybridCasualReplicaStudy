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
        private Dictionary<int, int[]> _chunkMap;
        private BoardLogic _boardLogic;
        public BoardDataArrays dataArrays;

        public void Initialize(GridOrchestrator gridOrchestrator, int headCount)
        {
            var gridLenght = gridOrchestrator.Lenght();
            _boardLogic = new BoardLogic();
            dataArrays = new BoardDataArrays();
            _chunkMap = new Dictionary<int, int[]>();

            dataArrays.headIndexArray = _boardLogic.PickRandomArray(new PickRandomArrayInput()
            {
                boardSize = gridLenght,
                headCount = headCount
            });

            dataArrays.directionArray = new Direction[gridLenght];

            foreach (var headIndex in dataArrays.headIndexArray)
            {
                dataArrays.directionArray[headIndex] = (Direction)Random.Range(1, 5);
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
