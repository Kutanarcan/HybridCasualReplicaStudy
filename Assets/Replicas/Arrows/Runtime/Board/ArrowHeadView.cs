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
        private const float FinishedNotifyDelay = 0.25f;

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
            var path = new ExitPath(_points, BoardSpace.ToWorldStep(_direction), ExitMargin);
            float bodyLength = path.BodyLength;
            float headArc = bodyLength;

            _tween?.Kill();
            _tween = DOTween.To(() => headArc, x => headArc = x, path.TotalLength + bodyLength, ExitDuration)
                .SetEase(Ease.InQuad)
                .OnUpdate(() => RenderWindow(path, Mathf.Max(0f, headArc - bodyLength), Mathf.Min(headArc, path.TotalLength)))
                .OnComplete(() =>
                {
                    gameObject.SetActive(false);
                    DOVirtual.DelayedCall(FinishedNotifyDelay, () => AnimationFinished?.Invoke());
                })
                .SetLink(gameObject);
        }

        private void RenderWindow(ExitPath path, float tailArc, float headArc)
        {
            var headPoint = path.CollectWindow(tailArc, headArc, _buffer);

            _ChunkRenderer.positionCount = _buffer.Count;
            for (int i = 0; i < _buffer.Count; i++)
                _ChunkRenderer.SetPosition(i, _buffer[i]);

            transform.position = headPoint; // keep the head sprite riding the snake's nose
        }

        // Wrong-answer feedback: the head lunges along its direction up to the blocking cell, then
        // springs back to its original position. The body's head-end (line vertex 0) follows so the
        // line stays attached, stretching on the lunge and recoiling on return.
        public void AnimateBump(Vector3 blockerPosition)
        {
            var origin = _points[0];
            var contact = blockerPosition - BoardSpace.ToWorldStep(_direction) * ContactGap; // stop just short so the sprites touch

            _tween?.Kill();
            PlaceHead(origin); // clear any leftover offset from an interrupted bump
            SetTint(0f);       // and any leftover red

            var pos = origin;
            float tint = 0f;
            _tween = DOTween.Sequence()
                .Append(DOTween.To(() => pos, p => { pos = p; PlaceHead(p); }, contact, BumpOutDuration)
                    .SetEase(Ease.OutQuad))
                .Append(DOTween.To(() => pos, p => { pos = p; PlaceHead(p); }, origin, BumpReturnDuration)
                    .SetEase(Ease.OutBack))
                // Flash to red on the lunge and hold it — a bumped arrow stays red afterwards.
                .Insert(0f, DOTween.To(() => tint, x => { tint = x; SetTint(x); }, 1f, BumpOutDuration))
                .SetLink(gameObject);
        }

        private void PlaceHead(Vector3 position)
        {
            transform.position = position;
            _ChunkRenderer.SetPosition(0, position);
        }

        // t = 0 -> original colours, t = 1 -> full red (head sprite + body line).
        private void SetTint(float t)
        {
            if (_headRenderer != null)
                _headRenderer.color = Color.Lerp(_headColor, BumpColor, t);

            var line = Color.Lerp(_lineColor, BumpColor, t);
            _ChunkRenderer.startColor = line;
            _ChunkRenderer.endColor = line;
        }
    }
}
