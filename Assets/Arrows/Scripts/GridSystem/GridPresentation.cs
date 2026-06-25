using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

namespace ReplicaProjects.Arrows
{
    public class GridPresentation : MonoBehaviour
    {
        [SerializeField] private GameObject _DotPrefab;

        private readonly List<GameObject> _dotList = new();
        private Sequence _sequence;

        public void Initialize(int width, int height)
        {
            _dotList.Capacity = width * height;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    var dot = Instantiate(_DotPrefab);

                    dot.transform.position = new Vector3(x, y, 0);
                    _dotList.Add(dot);
                }
            }

            //AnimateDots(new List<int>()
            //{
            //   0,1,2,3,4
            //});
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
