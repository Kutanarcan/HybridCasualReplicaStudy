namespace ReplicaProjects.Arrows
{
    public class BoardLogic
    {

        public BoardLogic()
        {
        }

        public int[] PickRandomArray(PickRandomArrayInput input)
        {
            var pool = new int[input.boardSize];
            var result = new int[input.headCount];

            for (int i = 0; i < input.boardSize; i++)
                pool[i] = i;

            for (int i = 0; i < input.headCount; i++)
            {
                int j = UnityEngine.Random.Range(i, input.boardSize);
                (pool[i], pool[j]) = (pool[j], pool[i]);
                result[i] = pool[i];
            }

            return result;
        }

        public BoardCornerEvaluateResult IsAtCorner(BoardCornerEvaluateInput input)
        {


            return new BoardCornerEvaluateResult();
        }

        public BoardIntersectionEvaluateResult IsAtCorner(BoardIntersectionEvaluateInput input)
        {

            return new BoardIntersectionEvaluateResult();
        }
    }
}
