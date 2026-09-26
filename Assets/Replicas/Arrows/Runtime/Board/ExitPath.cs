using System.Collections.Generic;
using UnityEngine;

namespace ReplicaProjects.Arrows
{
    /// <summary>
    /// The polyline an arrow slides along when it leaves the board: tail -> head -> exit point, with
    /// cumulative arc lengths so the body can be drawn as a sliding window over it.
    /// </summary>
    public sealed class ExitPath
    {
        private readonly Vector3[] _path;
        private readonly float[] _arc; // arc length at each vertex, measured from the tail

        public float BodyLength { get; }
        public float TotalLength => _arc[^1];

        /// <param name="body">Head first, then line cells head -> tail.</param>
        public ExitPath(IReadOnlyList<Vector3> body, Vector3 exitStep, float exitMargin)
        {
            int bodyCount = body.Count;

            // Reverse the body so the path runs tail -> head, matching travel order.
            _path = new Vector3[bodyCount + 1];
            for (int i = 0; i < bodyCount; i++)
                _path[i] = body[bodyCount - 1 - i];
            _path[bodyCount] = body[0] + exitStep * exitMargin;

            _arc = new float[_path.Length];
            for (int i = 1; i < _path.Length; i++)
                _arc[i] = _arc[i - 1] + Vector3.Distance(_path[i - 1], _path[i]);

            BodyLength = _arc[bodyCount - 1];
        }

        /// <summary>World position at the given arc length, clamped to the path ends.</summary>
        public Vector3 Sample(float target)
        {
            if (target <= 0f)
                return _path[0];
            if (target >= _arc[^1])
                return _path[^1];

            for (int i = 1; i < _path.Length; i++)
            {
                if (target > _arc[i])
                    continue;

                float segment = _arc[i] - _arc[i - 1];
                float t = segment > 0f ? (target - _arc[i - 1]) / segment : 0f;
                return Vector3.Lerp(_path[i - 1], _path[i], t);
            }

            return _path[^1];
        }

        /// <summary>
        /// Fills <paramref name="into"/> with the visible body between two arc lengths: the tail point,
        /// every pinned corner strictly inside the window, then the head point (returned).
        /// </summary>
        public Vector3 CollectWindow(float tailArc, float headArc, List<Vector3> into)
        {
            into.Clear();
            into.Add(Sample(tailArc));

            for (int v = 0; v < _path.Length; v++)
                if (_arc[v] > tailArc && _arc[v] < headArc)
                    into.Add(_path[v]);

            var head = Sample(headArc);
            into.Add(head);
            return head;
        }
    }
}
