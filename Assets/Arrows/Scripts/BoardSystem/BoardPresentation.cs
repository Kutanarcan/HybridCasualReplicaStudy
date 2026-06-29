using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;

namespace ReplicaProjects.Arrows
{
    public struct ArrowPresentationData
    {
        public Vector2Int headCoordinates;
        public Direction headDirection;
        public List<LineCell> line; // ordered head -> tail, EXCLUDES the head cell
    }

    public class PresentationData
    {
        public ArrowHeadView head;
    }

    public class BoardPresentation : MonoBehaviour
    {
        public event System.Action AnimationFinished;

        [SerializeField] private ArrowHeadView _ArrowPrefab;
        [SerializeField] private GameObject _DotPrefab;

        private readonly List<GameObject> _dotList = new();
        private readonly List<GameObject> _boardItemList = new();
        private Sequence _sequence;

        private Dictionary<Vector2Int, PresentationData> _presentationObjectTable;

        public void Initialize(List<ArrowPresentationData> arrowDataList)
        {
            _presentationObjectTable = new(arrowDataList.Count);

            for (int i = 0; i < arrowDataList.Count; i++)
            {
                var arrowData = arrowDataList[i];

                var view = Instantiate(_ArrowPrefab);
                view.transform.position = ToWorld(arrowData.headCoordinates);
                view.transform.rotation = arrowData.headDirection.ToQuaternion();

                // Body polyline: head cell first, then each line cell in head -> tail order.
                var points = new List<Vector3>(arrowData.line.Count + 1) { ToWorld(arrowData.headCoordinates) };
                SpawnDot(arrowData.headCoordinates);

                foreach (var cell in arrowData.line)
                {
                    points.Add(ToWorld(cell.coordinates));
                    SpawnDot(cell.coordinates);
                }

                view.Initialize(points, arrowData.headDirection);

                view.AnimationFinished += OnAnimationFinished;

                _boardItemList.Add(view.gameObject);
                _presentationObjectTable[arrowData.headCoordinates] = new PresentationData { head = view };
            }
        }

        private void OnAnimationFinished()
        {
            AnimationFinished?.Invoke();
        }

        private static Vector3 ToWorld(Vector2Int coordinates) =>
            new(coordinates.x, coordinates.y, 0);

        private void SpawnDot(Vector2Int coordinates)
        {
            var dot = Instantiate(_DotPrefab);
            dot.transform.position = ToWorld(coordinates);
            _dotList.Add(dot);
        }

        public void DeInitialize()
        {
            for (int i = _dotList.Count - 1; i >= 0; i--)
            {
                var dot = _dotList[i];

                if (dot == null)
                    continue;

                Destroy(dot);
            }

            _dotList.Clear();

            for (int i = _boardItemList.Count - 1; i >= 0; i--)
            {
                var boardItem = _boardItemList[i];

                if (boardItem == null)
                    continue;

                Destroy(boardItem);
            }

            foreach (var data in _presentationObjectTable)
            {
                data.Value.head.AnimationFinished -= OnAnimationFinished;
            }

            _boardItemList.Clear();
            _sequence?.Kill();
            _presentationObjectTable.Clear();
        }

        public void EmptyArrow(Vector2Int headCoordinate)
        {
            if (!_presentationObjectTable.TryGetValue(headCoordinate, out var data))
                return;

            if (data.head != null)
                data.head.AnimateEmpty();
        }

        private void AnimateDots(List<int> indexList)
        {
            _sequence = DOTween.Sequence();

            for (int i = 0; i < indexList.Count; i++)
            {
                var dot = _dotList[indexList[i]];

                dot.transform.localScale = Vector3.zero;

                _sequence.Join(dot.transform.DOScale(Vector3.one, 0.1f).SetEase(Ease.OutCubic).SetDelay(0.05f).SetLink(dot));

                // TODO: I will add some overshoot for this animation
            }
        }
    }
}
