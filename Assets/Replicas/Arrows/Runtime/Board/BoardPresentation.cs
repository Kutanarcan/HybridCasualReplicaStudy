using System.Collections.Generic;
using UnityEngine;

namespace ReplicaProjects.Arrows
{
    public class BoardPresentation : MonoBehaviour
    {
        public event System.Action AnimationFinished;

        [SerializeField] private ArrowHeadView _ArrowPrefab;
        [SerializeField] private GameObject _DotPrefab;

        private readonly List<GameObject> _dotList = new();
        private readonly Dictionary<GridCoord, ArrowHeadView> _arrowTable = new();

        public void Initialize(IReadOnlyList<HeadData> heads)
        {
            for (int i = 0; i < heads.Count; i++)
                SpawnArrow(heads[i]);
        }

        private void SpawnArrow(HeadData head)
        {
            var view = Instantiate(_ArrowPrefab);
            view.transform.SetPositionAndRotation(BoardSpace.ToWorld(head.coordinates), BoardSpace.ToRotation(head.direction));

            // Body polyline: head cell first, then each line cell in head -> tail order.
            int lineCount = head.line?.Count ?? 0;
            var points = new List<Vector3>(lineCount + 1) { BoardSpace.ToWorld(head.coordinates) };
            SpawnDot(head.coordinates);

            for (int i = 0; i < lineCount; i++)
            {
                var cell = head.line[i].coordinates;
                points.Add(BoardSpace.ToWorld(cell));
                SpawnDot(cell);
            }

            view.Initialize(points, head.direction);
            view.AnimationFinished += OnAnimationFinished;

            _arrowTable[head.coordinates] = view;
        }

        private void OnAnimationFinished() => AnimationFinished?.Invoke();

        private void SpawnDot(GridCoord coordinates)
        {
            var dot = Instantiate(_DotPrefab);
            dot.transform.position = BoardSpace.ToWorld(coordinates);
            _dotList.Add(dot);
        }

        public void DeInitialize()
        {
            foreach (var dot in _dotList)
                if (dot != null)
                    Destroy(dot);

            foreach (var view in _arrowTable.Values)
            {
                if (view == null)
                    continue;

                view.AnimationFinished -= OnAnimationFinished;
                Destroy(view.gameObject);
            }

            _dotList.Clear();
            _arrowTable.Clear();
        }

        public void EmptyArrow(GridCoord headCoordinate)
        {
            if (_arrowTable.TryGetValue(headCoordinate, out var view) && view != null)
                view.AnimateEmpty();
        }

        // Wrong-answer feedback: the arrow lunges at the blocking cell and springs back. The arrow
        // stays on the board, so it is NOT removed from the table.
        public void BumpArrow(GridCoord headCoordinate, GridCoord blockerCoordinate)
        {
            if (_arrowTable.TryGetValue(headCoordinate, out var view) && view != null)
                view.AnimateBump(BoardSpace.ToWorld(blockerCoordinate));
        }
    }
}
