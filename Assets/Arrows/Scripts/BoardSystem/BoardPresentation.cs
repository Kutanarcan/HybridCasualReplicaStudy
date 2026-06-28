using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;

namespace ReplicaProjects.Arrows
{
    public enum BoardItemType
    {
        Arrow,
        Line
    }

    public struct BoardItemData
    {
        public Vector2Int coordinates;
        public Direction direction;
        public BoardItemType type;
    }

    public class BoardPresentation : MonoBehaviour
    {
        [SerializeField] private GameObject _ArrowPrefab;
        [SerializeField] private GameObject _LinePrefab;
        [SerializeField] private GameObject _DotPrefab;

        private readonly List<GameObject> _dotList = new();
        private Sequence _sequence;

        public void Initialize(List<BoardItemData> boardItemDataList)
        {
            for (int i = 0; i < boardItemDataList.Count; i++)
            {
                var boardItemData = boardItemDataList[i];
                var prefab = SelectPrefab(boardItemData.type);

                if (prefab == null)
                    continue;

                var arrow = Instantiate(prefab);
                var dot = Instantiate(_DotPrefab);

                dot.transform.position = new Vector3(boardItemData.coordinates.x, boardItemData.coordinates.y, 0);
                _dotList.Add(dot);

                arrow.transform.position = new Vector3(boardItemData.coordinates.x, boardItemData.coordinates.y);
                arrow.transform.rotation = boardItemData.direction.ToQuaternion();
            }
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
            _sequence.Kill();
        }


        public GameObject SelectPrefab(BoardItemType type)
        {
            switch (type)
            {
                case BoardItemType.Arrow:
                    return _ArrowPrefab;
                case BoardItemType.Line:
                    return _LinePrefab;
            }

            return null;

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
