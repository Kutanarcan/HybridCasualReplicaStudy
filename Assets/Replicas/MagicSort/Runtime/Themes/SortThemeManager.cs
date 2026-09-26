using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ReplicaProjects.MagicSort
{
    /// <summary>
    /// Tüm tema prefab'larını bir kez instantiate edip havuzlar (SetActive ile aç/kapa).
    /// Tema değişiminde kamera, cache'lenen başlangıç pozuna resetlenir
    /// (FitCamera göreli += kaydırma yaptığı için şart).
    /// </summary>
    public class SortThemeManager
    {
        public event Action<int> BarTapped;

        private readonly List<ISortTheme> _themes = new();
        private readonly List<GameObject> _themeObjects = new();
        private int _activeIndex;

        private Vector3 _cameraInitialPosition;
        private Quaternion _cameraInitialRotation;
        private float _cameraInitialOrthoSize;

        public ISortTheme Active => _themes[_activeIndex];

        public SortThemeManager(IReadOnlyList<GameObject> themePrefabs)
        {
            CacheCameraDefaults();

            for (int i = 0; i < themePrefabs.Count; i++)
            {
                var instance = Object.Instantiate(themePrefabs[i]);
                var theme = instance.GetComponent<ISortTheme>();
                theme.BarTapped += OnBarTapped;
                instance.SetActive(i == _activeIndex);

                _themes.Add(theme);
                _themeObjects.Add(instance);
            }
        }

        public ISortTheme ActivateNext()
        {
            Active.Teardown();
            _themeObjects[_activeIndex].SetActive(false);

            _activeIndex = (_activeIndex + 1) % _themes.Count;

            _themeObjects[_activeIndex].SetActive(true);
            return Active;
        }

        public void ResetCamera()
        {
            Camera cam = Camera.main;
            if (cam == null)
                return;

            cam.transform.SetPositionAndRotation(_cameraInitialPosition, _cameraInitialRotation);
            cam.orthographicSize = _cameraInitialOrthoSize;
        }

        private void CacheCameraDefaults()
        {
            Camera cam = Camera.main;
            if (cam == null)
                return;

            _cameraInitialPosition = cam.transform.position;
            _cameraInitialRotation = cam.transform.rotation;
            _cameraInitialOrthoSize = cam.orthographicSize;
        }

        private void OnBarTapped(int index) => BarTapped?.Invoke(index);
    }
}
