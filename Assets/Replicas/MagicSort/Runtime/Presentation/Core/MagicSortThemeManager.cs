using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ReplicaProjects.MagicSort
{
    /// <summary>
    /// Tüm tema root prefab'larını bir kez instantiate edip havuzlar (SetActive ile aç/kapa).
    /// Tema değişiminde kamera, cache'lenen başlangıç pozuna resetlenir
    /// (FitCamera göreli += kaydırma yaptığı için şart).
    /// </summary>
    public class MagicSortThemeManager
    {
        public event Action<int> AnyBarViewClicked;

        private readonly List<MagicSortThemeRoot> _themeInstances = new();
        private int _activeIndex;

        private Vector3 _cameraInitialPosition;
        private Quaternion _cameraInitialRotation;
        private float _cameraInitialOrthoSize;

        public MagicSortThemeRoot Active => _themeInstances[_activeIndex];

        public MagicSortThemeManager(IReadOnlyList<MagicSortThemeRoot> themePrefabs)
        {
            CacheCameraDefaults();

            for (int i = 0; i < themePrefabs.Count; i++)
            {
                var instance = Object.Instantiate(themePrefabs[i]);
                instance.AnyBarViewClicked += OnAnyBarViewClicked;
                instance.gameObject.SetActive(i == _activeIndex);
                _themeInstances.Add(instance);
            }
        }

        public MagicSortThemeRoot ActivateNext()
        {
            Active.DeInitialize();
            Active.gameObject.SetActive(false);

            _activeIndex = (_activeIndex + 1) % _themeInstances.Count;

            Active.gameObject.SetActive(true);
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

        private void OnAnyBarViewClicked(int index)
        {
            AnyBarViewClicked?.Invoke(index);
        }
    }
}
