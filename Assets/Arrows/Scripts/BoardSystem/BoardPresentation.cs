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
        public Vector2Int headCoordinates; // owning head's coordinate (head items use their own)
        public Direction direction;
        public BoardItemType type;
    }

    public class PresentationData
    {
        public GameObject head;
        public readonly List<GameObject> line = new();
    }

    public class BoardPresentation : MonoBehaviour
    {
        [SerializeField] private GameObject _ArrowPrefab;
        [SerializeField] private GameObject _LinePrefab;
        [SerializeField] private GameObject _DotPrefab;

        private readonly List<GameObject> _dotList = new();
        private readonly List<GameObject> _boardItemList = new();
        private Sequence _sequence;

        private Dictionary<Vector2Int, PresentationData> _presentationObjectTable;

        public void Initialize(BoardItemData headData, List<BoardItemData> boardItemDataList)
        {

        }

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

                _boardItemList.Add(boardItem);
                Add(boardItemData, boardItem);
            }
        }

        private void Add(BoardItemData itemData, GameObject boardItem)
        {
            // Group head + line visuals under the owning head's coordinate.
            if (!_presentationObjectTable.TryGetValue(itemData.headCoordinates, out var data))
            {
                data = new PresentationData();
                _presentationObjectTable.Add(itemData.headCoordinates, data);
            }

            if (itemData.type == BoardItemType.Arrow)
                data.head = boardItem;
            else
                data.line.Add(boardItem);
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

            _boardItemList.Clear();
            _sequence?.Kill();
            _presentationObjectTable.Clear();
        }

        public void EmptyArrow(Vector2Int headCoordinate)
        {
            if (!_presentationObjectTable.TryGetValue(headCoordinate, out var data))
                return;

            _presentationObjectTable.Remove(headCoordinate);

            if (data.head != null)
                data.head.SetActive(false); // TODO Animation Later

            foreach (var lineObject in data.line)
                if (lineObject != null)
                    lineObject.SetActive(false);
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
