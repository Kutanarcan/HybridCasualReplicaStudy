using System.Collections.Generic;
using UnityEngine;

namespace ReplicaProjects.Arrows
{
    public class GridPresentation : MonoBehaviour
    {
        [SerializeField] private GameObject _DotPrefab;

        private readonly List<GameObject> _dotList = new();

        public void Initialize(int width, int height)
        {
            _dotList.Capacity = width * height;

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    var dot = Instantiate(_DotPrefab);

                    dot.transform.position = new Vector3(x, y, 0);
                    _dotList.Add(dot);
                }
            }

            AnimeDots();
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
        }

        private void AnimeDots()
        {
            // TODO: When i add DoTween, I will scale animate this
            // Maybe i will use range for this because it will send chunk of arrows and i will animate this.
        }
    }
}
