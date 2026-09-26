using ReplicaProjects.Common;

namespace ReplicaProjects.Arrows
{
    public static class DirectionShuffler
    {
        /// <summary>Fills the 4-slot buffer with Up/Down/Left/Right in a Fisher–Yates random order.</summary>
        public static void Shuffle(IRandomSource random, Direction[] buffer)
        {
            buffer[0] = Direction.Up;
            buffer[1] = Direction.Down;
            buffer[2] = Direction.Left;
            buffer[3] = Direction.Right;

            for (int i = buffer.Length - 1; i > 0; i--)
            {
                int j = random.Next(0, i + 1);
                (buffer[i], buffer[j]) = (buffer[j], buffer[i]);
            }
        }
    }
}
