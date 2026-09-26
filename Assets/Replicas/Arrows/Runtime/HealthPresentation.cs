
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ReplicaProjects.Arrows
{
    public class HealthPresentation : MonoBehaviour
    {
        [SerializeField] private Image _HealthImagePrefab;
        [SerializeField] private Transform _HealthImageContainer;

        private List<Image> _healthImageList;

        public void Initialize(int max)
        {
            _healthImageList = new List<Image>(max);

            CreateHealthImages(max);
        }

        public void DeInitialize()
        {
            foreach (Image image in _healthImageList)
            {
                if (image == null)
                    continue;

                Destroy(image.gameObject);
            }

            _healthImageList?.Clear();
            _healthImageList = null;
        }

        public void SetHealthAmount(int healthAmount)
        {
            healthAmount = Mathf.Clamp(healthAmount, 0, _healthImageList.Count);

            for (int i = 0; i < _healthImageList.Count; i++)
            {
                _healthImageList[i].color = i < healthAmount ? Color.red : Color.white;
            }
        }

        private void CreateHealthImages(int amount)
        {
            for (int i = 0; i < amount; i++)
            {
                var healthImage = Instantiate(_HealthImagePrefab, _HealthImageContainer);

                healthImage.color = Color.red;
                _healthImageList.Add(healthImage);
            }
        }
    }
}
