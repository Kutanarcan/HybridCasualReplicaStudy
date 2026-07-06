using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace ReplicaProjects.MagicSort
{
    public class BoltThemeRoot : MagicSortThemeRoot
    {
        [FormerlySerializedAs("_MagicSortBarViewPrefab")]
        [SerializeField] private BoltBarView _BarViewPrefab;
        [SerializeField] private List<Color> _colorPalette;

        private const float X_OFFSET = 1.75F;
        private const float ROW_Y_OFFSET = 3.0F;   // ekran aralığı: satır başına ekstra düşüş
        private const float ROW_Z_OFFSET = 2.0F;   // derinlik: ön satırlar arkadakileri örter
        private const int MAX_COLUMNS = 3;

        private const float BAR_VISUAL_HEIGHT = 2.5F; // somun yığını + kapak için üst pay
        private const float CAMERA_PAD_X = 1.0F;
        private const float CAMERA_PAD_Y = 0.75F;

        public readonly Vector3 Offset = new Vector3(X_OFFSET, ROW_Y_OFFSET, ROW_Z_OFFSET);

        protected override List<MagicSortBarViewBase> CreateBarViews(List<BarVisualData> bars)
        {
            var barViewList = new List<MagicSortBarViewBase>(bars.Count);

            for (int i = 0; i < bars.Count; i++)
            {
                int col = i % MAX_COLUMNS;
                int row = i / MAX_COLUMNS;

                BoltBarView barView = Instantiate(_BarViewPrefab, BarsRoot.transform);
                barView.transform.localPosition = new Vector3(col * Offset.x, -row * Offset.y, -row * Offset.z);

                barView.SetPalette(_colorPalette);
                barView.Initialize(bars[i], i);

                barViewList.Add(barView);
            }

            return barViewList;
        }

        protected override void AdjustCamera(int barCount)
        {
            Camera cam = Camera.main;
            if (cam == null)
                return;

            Transform camT = cam.transform;

            // Bar taban ve tepe noktalarını kamera uzayına yansıt, board'un ekran sınırlarını bul
            float minX = float.MaxValue, maxX = float.MinValue;
            float minY = float.MaxValue, maxY = float.MinValue;

            for (int i = 0; i < barCount; i++)
            {
                int col = i % MAX_COLUMNS;
                int row = i / MAX_COLUMNS;

                Vector3 barBase = transform.TransformPoint(new Vector3(col * Offset.x, -row * Offset.y, -row * Offset.z));
                Vector3 barTop = barBase + Vector3.up * BAR_VISUAL_HEIGHT;

                Vector3 lp = camT.InverseTransformPoint(barBase);
                minX = Mathf.Min(minX, lp.x); maxX = Mathf.Max(maxX, lp.x);
                minY = Mathf.Min(minY, lp.y); maxY = Mathf.Max(maxY, lp.y);

                lp = camT.InverseTransformPoint(barTop);
                minX = Mathf.Min(minX, lp.x); maxX = Mathf.Max(maxX, lp.x);
                minY = Mathf.Min(minY, lp.y); maxY = Mathf.Max(maxY, lp.y);
            }

            // Kamerayı kendi sağ/yukarı eksenlerinde kaydırarak board'u ortala
            float centerX = (minX + maxX) * 0.5f;
            float centerY = (minY + maxY) * 0.5f;
            camT.position += camT.right * centerX + camT.up * centerY;

            float halfHeight = (maxY - minY) * 0.5f + CAMERA_PAD_Y;
            float halfWidth = (maxX - minX) * 0.5f + CAMERA_PAD_X;

            cam.orthographicSize = Mathf.Max(halfHeight, halfWidth / cam.aspect);
        }
    }
}
