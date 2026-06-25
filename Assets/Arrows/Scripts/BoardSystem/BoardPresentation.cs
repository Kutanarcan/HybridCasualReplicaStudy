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

        public void Initialize(List<BoardItemData> boardItemDataList)
        {
            for (int i = 0; i < boardItemDataList.Count; i++)
            {
                var arrowData = boardItemDataList[i];
                var prefab = SelectPrefab(arrowData.type);

                if (prefab == null)
                    continue;

                var arrow = Instantiate(prefab);

                arrow.transform.position = new Vector3(arrowData.coordinates.x, arrowData.coordinates.y);
                arrow.transform.rotation = arrowData.direction.ToQuaternion();
            }
        }

        public void DeInitialize()
        {

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
    }
}
