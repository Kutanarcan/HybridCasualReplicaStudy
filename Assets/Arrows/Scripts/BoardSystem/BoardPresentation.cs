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

    public class PresentationData
    {
        public GameObject head;
    }

    public class BoardPresentation : MonoBehaviour
    {
        [SerializeField] private GameObject _ArrowPrefab;
        [SerializeField] private GameObject _LinePrefab;
        [SerializeField] private GameObject _DotPrefab;

        private readonly List<GameObject> _dotList = new();
        private Sequence _sequence;

        private Dictionary<Vector2Int, PresentationData> _presentationObjectTable;

        public void Initialize(List<BoardItemData> boardItemDataList)
        {
            _presentationObjectTable = new(boardItemDataList.Count);

            for (int i = 0; i < boardItemDataList.Count; i++)
            {
                var boardItemData = boardItemDataList[i];
                var prefab = SelectPrefab(boardItemData.type);

                if (prefab == null)
                    continue;

                var boardItem = Instantiate(prefab);
                var dot = Instantiate(_DotPrefab);

                dot.transform.position = new Vector3(boardItemData.coordinates.x, boardItemData.coordinates.y, 0);
                _dotList.Add(dot);

                boardItem.transform.position = new Vector3(boardItemData.coordinates.x, boardItemData.coordinates.y);
                boardItem.transform.rotation = boardItemData.direction.ToQuaternion();

                Add(boardItemData, boardItem);
            }
        }

        private void Add(BoardItemData itemData, GameObject boardItem)
        {
            if (itemData.type != BoardItemType.Arrow)
                return;

            _presentationObjectTable.Add(itemData.coordinates, new PresentationData()
            {
                head = boardItem
            });
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
            _presentationObjectTable.Clear();
        }

        public void EmptyAtCoordinate(Vector2Int coordinate)
        {
            if (!_presentationObjectTable.TryGetValue(coordinate, out var data))
                return;

            _presentationObjectTable.Remove(coordinate);
            data.head.SetActive(false); // TODO Animation Later
        }


        private GameObject SelectPrefab(BoardItemType type)
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
