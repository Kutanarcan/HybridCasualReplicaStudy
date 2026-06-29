using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

namespace ReplicaProjects.Arrows
{
    public class ArrowHeadView : MonoBehaviour
    {
        public event System.Action AnimationFinished;

        [SerializeField] private LineRenderer _ChunkRenderer;

        private const float ExitMargin = 10f;
        private const float ExitDuration = 0.825f;

        // Wrong-answer bump: how far short of the blocker's centre the head stops, and the lunge/recoil
        // timings.
        private const float ContactGap = 0.5f;
        private const float BumpOutDuration = 0.12f;
        private const float BumpReturnDuration = 0.22f;

        // Tint flashed on the head + body while bumping a wrong answer.
        private static readonly Color BumpColor = Color.red;

        private Vector3[] _points;
        private Direction _direction;
        private Tween _tween;

        // Reused each frame so the snake render doesn't allocate while animating.
        private readonly List<Vector3> _buffer = new();

        // Cached so the bump tint can lerp back to the originals.
        private SpriteRenderer _headRenderer;
        private Color _headColor;
        private Color _lineColor;

        public void Initialize(IReadOnlyList<Vector3> points, Direction direction)
        {
            _direction = direction;

            _points = new Vector3[points.Count];
            for (int i = 0; i < points.Count; i++)
                _points[i] = points[i];

            _ChunkRenderer.useWorldSpace = true; // head-root rotation must not skew the body
            _ChunkRenderer.positionCount = _points.Length;
            _ChunkRenderer.SetPositions(_points);

            _headRenderer = GetComponentInChildren<SpriteRenderer>();
            if (_headRenderer != null)
                _headColor = _headRenderer.color;
            _lineColor = _ChunkRenderer.startColor;
        }

        public void AnimateEmpty()
        {
            var path = BuildExitPath(out var pathArc, out float totalLength);

            float bodyLength = pathArc[_points.Length - 1];

            _tween?.Kill();
            float headArc = bodyLength;

            _tween = DOTween.To(() => headArc, x => headArc = x, totalLength + bodyLength, ExitDuration)
                .SetEase(Ease.InQuad)
                .OnUpdate(() =>
                {
                    float headClamped = Mathf.Min(headArc, totalLength);
                    float tailArc = Mathf.Max(0f, headArc - bodyLength);

                    _buffer.Clear();
                    _buffer.Add(SamplePath(path, pathArc, tailArc));

                    // Pinned corners still inside the snake's window (strictly between the endpoints).
                    for (int v = 0; v < path.Length; v++)
                        if (pathArc[v] > tailArc && pathArc[v] < headClamped)
                            _buffer.Add(path[v]);

                    var headPoint = SamplePath(path, pathArc, headClamped);
                    _buffer.Add(headPoint);

                    _ChunkRenderer.positionCount = _buffer.Count;
                    for (int i = 0; i < _buffer.Count; i++)
                        _ChunkRenderer.SetPosition(i, _buffer[i]);

                    transform.position = headPoint; // keep the head sprite riding the snake's nose
                })
                .OnComplete(() =>
                {
                    gameObject.SetActive(false);

                    DOVirtual.DelayedCall(0.25f, () =>
                    {
                        AnimationFinished?.Invoke();
                    });
                })
                .SetLink(gameObject);
        }

        // Wrong-answer feedback: the head lunges along its direction up to the blocking cell, then
        // springs back to its original position. The body's head-end (line vertex 0) follows so the
        // line stays attached, stretching on the lunge and recoiling on return.
        public void AnimateBump(Vector3 blockerPosition)
        {
            var origin = _points[0];
            var step = (Vector3)(Vector2)_direction.ToVector2Int();
            var contact = blockerPosition - step * ContactGap; // stop just short so the sprites touch

            void Apply(Vector3 p)
            {
                transform.position = p;
                _ChunkRenderer.SetPosition(0, p);
            }

            // t = 0 -> original colours, t = 1 -> full red (head sprite + body line).
            void SetTint(float t)
            {
                if (_headRenderer != null)
                    _headRenderer.color = Color.Lerp(_headColor, BumpColor, t);

                var line = Color.Lerp(_lineColor, BumpColor, t);
                _ChunkRenderer.startColor = line;
                _ChunkRenderer.endColor = line;
            }

            _tween?.Kill();
            Apply(origin); // clear any leftover offset from an interrupted bump
            SetTint(0f);   // and any leftover red

            var pos = origin;
            float tint = 0f;
            _tween = DOTween.Sequence()
                .Append(DOTween.To(() => pos, p => { pos = p; Apply(p); }, contact, BumpOutDuration)
                    .SetEase(Ease.OutQuad))
                .Append(DOTween.To(() => pos, p => { pos = p; Apply(p); }, origin, BumpReturnDuration)
                    .SetEase(Ease.OutBack))
                // Flash to red on the lunge and hold it — a bumped arrow stays red afterwards.
                .Insert(0f, DOTween.To(() => tint, x => { tint = x; SetTint(x); }, 1f, BumpOutDuration))
                .SetLink(gameObject);
        }

        // Returns the polyline (tail -> head -> exit), its per-vertex cumulative arc-lengths measured
        // from the tail, and the total length.
        private Vector3[] BuildExitPath(out float[] pathArc, out float totalLength)
        {
            int bodyCount = _points.Length;

            // Reverse the body so the path runs tail -> head, matching travel order.
            var reversed = new Vector3[bodyCount];
            for (int i = 0; i < bodyCount; i++)
                reversed[i] = _points[bodyCount - 1 - i];

            var step = (Vector3)(Vector2)_direction.ToVector2Int();
            var headPos = _points[0];
            var exit = headPos + step * ExitMargin;

            var path = new Vector3[bodyCount + 1];
            System.Array.Copy(reversed, path, bodyCount);
            path[bodyCount] = exit;

            // Cumulative arc-length at each path vertex (index 0 = tail = 0).
            pathArc = new float[path.Length];
            for (int i = 1; i < path.Length; i++)
                pathArc[i] = pathArc[i - 1] + Vector3.Distance(path[i - 1], path[i]);

            totalLength = pathArc[^1];

            return path;
        }

        // Samples a world position at the given arc-length along the path, clamped to its ends.
        private static Vector3 SamplePath(Vector3[] path, float[] arc, float target)
        {
            if (target <= 0f)
                return path[0];
            if (target >= arc[^1])
                return path[^1];

            for (int i = 1; i < path.Length; i++)
            {
                if (target > arc[i])
                    continue;

                float segment = arc[i] - arc[i - 1];
                float t = segment > 0f ? (target - arc[i - 1]) / segment : 0f;
                return Vector3.Lerp(path[i - 1], path[i], t);
            }

            return path[^1];
        }
    }
}
