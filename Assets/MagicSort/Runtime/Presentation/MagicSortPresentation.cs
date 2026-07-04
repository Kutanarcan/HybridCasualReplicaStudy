using System.Collections.Generic;
using UnityEngine;

namespace ReplicaProjects.MagicSort
{
    public class BarVisualData
    {
        public List<int> colorList;
    }

    public class MagicSortPresentation : MonoBehaviour
    {
        [SerializeField] private MagicSortBarView _MagicSortBarViewPrefab;

        private const float X_OFFSET = 1.75F;
        private const float Y_OFFSET = 4.0F;

        public readonly Vector2 Offset = new Vector2(X_OFFSET, Y_OFFSET);

        public void Initialize(List<BarVisualData> bars)
        {
            CreateItems(bars);
        }

        public void DeInitialize()
        {

        }



        private void CreateItems(List<BarVisualData> bars)
        {
            GameObject nutHolder = new GameObject("NutHolder");
            gameObject.transform.SetParent(transform, false);

            for (int i = 0; i < bars.Count; i++)
            {
                Instantiate(_MagicSortBarViewPrefab, new Vector2(1.75F, 0) * i, Quaternion.identity);
            }

        }
    }
}
