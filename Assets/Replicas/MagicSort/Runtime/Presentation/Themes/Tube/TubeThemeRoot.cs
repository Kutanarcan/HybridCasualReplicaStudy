using DG.Tweening;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ReplicaProjects.MagicSort
{
    public class TubeThemeRoot : MonoBehaviour, IMagicSortTheme, DiscreteItemBoard.IItemAnimator
    {
        public event Action<int> BarTapped;

        [SerializeField] private TubeBarView _BarViewPrefab;
        [SerializeField] private ColorPallete _colorPalette;

        private const float X_OFFSET = 1.25F;
        private const float ROW_Y_OFFSET = 4.5F;   // satır başına düşüş
        private const int MAX_COLUMNS = 3;

        private const float BAR_VISUAL_HEIGHT = 4.5F; // tüp yüksekliği için üst pay (gerekirse ayarla)
        private const float CAMERA_PAD_X = 1.0F;
        private const float CAMERA_PAD_Y = 0.75F;

        public readonly Vector3 Offset = new Vector3(X_OFFSET, ROW_Y_OFFSET, 0f);

        private GameObject _barsRoot;
        private List<TubeBarView> _views;
        private DiscreteItemBoard _board;

        public void Build(IReadOnlyList<BarVisualData> bars)
        {
            _barsRoot = new GameObject("BarsRoot");
            _barsRoot.transform.SetParent(transform, false);

            _views = new List<TubeBarView>(bars.Count);
            _board = new DiscreteItemBoard(bars.Count, this);

            for (int i = 0; i < bars.Count; i++)
            {
                int col = i % MAX_COLUMNS;
                int row = i / MAX_COLUMNS;

                TubeBarView barView = Instantiate(_BarViewPrefab, _barsRoot.transform);
                barView.transform.localPosition = new Vector3(col * Offset.x, -row * Offset.y, 0f);

                barView.SetPalette(_colorPalette.colorList);

                int barIndex = i;
                barView.Clicked += () => OnBarViewClicked(barIndex);

                var items = barView.Setup(bars[i]);
                foreach (var item in items)
                    _board.Push(barIndex, item);

                _views.Add(barView);
            }

            AdjustCamera(bars.Count);
        }

        public void Teardown()
        {
            for (int i = _views.Count - 1; i >= 0; i--)
            {
                var view = _views[i];
                if (view == null)
                    continue;

                view.Teardown();
            }

            Destroy(_barsRoot); // SetLink(KillOnDisable) sayesinde çalışan tween'ler de ölür
            _barsRoot = null;
            _views = null;
            _board = null;
        }

        public void PlayIntro(Action onComplete)
        {
            int remaining = _views.Count;
            if (remaining == 0)
            {
                onComplete();
                return;
            }

            foreach (var view in _views)
            {
                view.PlayIntroAnimation(view.ItemsBottomToTop()).OnComplete(() =>
                {
                    remaining--;
                    if (remaining == 0)
                        onComplete();
                });
            }
        }

        public void ShowSelected(int bar) => _board.Selected(bar);
        public void ShowDeselected(int bar) => _board.Deselected(bar);

        public void PlayTransport(in TransportData command, Action onComplete)
            => _board.PlayTransport(in command, _views[command.SourceBar].TopPositionHolder.position, onComplete);

        public void ShowSolved(int bar, bool animated)
        {
            _views[bar].PlaySolvedSound();
        }

        private void OnBarViewClicked(int index) => BarTapped?.Invoke(index);

        // DiscreteItemBoard.IItemAnimator: her top kendi bar view'ının içinde yaşar,
        // bu yüzden hedef bar view'a devredilir.
        public Sequence MoveItem(GameObject item, int targetBar, int targetSlot, Vector3 sourceTopWorldPos, float delay)
            => _views[targetBar].MoveItem(item, targetBar, targetSlot, sourceTopWorldPos, delay);

        public void Selected(GameObject item) => item.GetComponentInParent<TubeBarView>().Selected(item);
        public void Deselected(GameObject item) => item.GetComponentInParent<TubeBarView>().Deselected(item);

        private void AdjustCamera(int barCount)
        {
            Camera cam = Camera.main;
            if (cam == null)
                return;

            Transform camT = cam.transform;

            // Bar tepe/taban noktalarını kamera uzayına yansıt, board'un ekran sınırlarını bul.
            // Tüp sprite'ı bar orijininde MERKEZLENDİĞİ için (pivot = center) dikey açıklık
            // orijinin ETRAFINDA simetriktir; aksi halde board dikeyde kayar.
            float halfVisual = BAR_VISUAL_HEIGHT * 0.5f;

            float minX = float.MaxValue, maxX = float.MinValue;
            float minY = float.MaxValue, maxY = float.MinValue;

            for (int i = 0; i < barCount; i++)
            {
                int col = i % MAX_COLUMNS;
                int row = i / MAX_COLUMNS;

                Vector3 barCenter = transform.TransformPoint(new Vector3(col * Offset.x, -row * Offset.y, 0f));
                Vector3 barTop = barCenter + Vector3.up * halfVisual;
                Vector3 barBottom = barCenter - Vector3.up * halfVisual;

                Vector3 lp = camT.InverseTransformPoint(barTop);
                minX = Mathf.Min(minX, lp.x); maxX = Mathf.Max(maxX, lp.x);
                minY = Mathf.Min(minY, lp.y); maxY = Mathf.Max(maxY, lp.y);

                lp = camT.InverseTransformPoint(barBottom);
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
