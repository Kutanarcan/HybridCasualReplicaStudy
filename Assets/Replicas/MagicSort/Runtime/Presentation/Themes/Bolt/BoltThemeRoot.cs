using DG.Tweening;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace ReplicaProjects.MagicSort
{
    public class BoltThemeRoot : MonoBehaviour, IMagicSortTheme, DiscreteItemBoard.IItemAnimator
    {
        public event Action<int> BarTapped;

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

        private GameObject _barsRoot;
        private List<BoltBarView> _views;
        private DiscreteItemBoard _board;

        public void Build(IReadOnlyList<BarVisualData> bars)
        {
            _barsRoot = new GameObject("BarsRoot");
            _barsRoot.transform.SetParent(transform, false);

            _views = new List<BoltBarView>(bars.Count);
            _board = new DiscreteItemBoard(bars.Count, this);

            for (int i = 0; i < bars.Count; i++)
            {
                int col = i % MAX_COLUMNS;
                int row = i / MAX_COLUMNS;

                BoltBarView barView = Instantiate(_BarViewPrefab, _barsRoot.transform);
                barView.transform.localPosition = new Vector3(col * Offset.x, -row * Offset.y, -row * Offset.z);

                barView.SetPalette(_colorPalette);

                int barIndex = i;
                barView.Clicked += () => OnBarViewClicked(barIndex);

                var items = barView.Setup(bars[i]);
                foreach (var item in items)
                    _board.Push(barIndex, item);

                if (bars[i].IsSolved)
                    barView.SetSolvedInstant();

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
            => _board.PlayPour(in command, _views[command.SourceBar].TopPositionHolder.position, onComplete);

        public void ShowSolved(int bar, bool animated)
        {
            if (animated)
                _views[bar].PlaySolvedAnimation();
            else
                _views[bar].SetSolvedInstant();
        }

        private void OnBarViewClicked(int index) => BarTapped?.Invoke(index);

        // DiscreteItemBoard.IItemAnimator: her item kendi bar view'ının içinde yaşar,
        // bu yüzden hedef bar view'a devredilir.
        public Sequence MoveItem(GameObject item, int targetBar, int targetSlot, Vector3 sourceTopWorldPos, float delay)
            => _views[targetBar].MoveItem(item, targetBar, targetSlot, sourceTopWorldPos, delay);

        public void Selected(GameObject item) => item.GetComponentInParent<BoltBarView>().Selected(item);
        public void Deselected(GameObject item) => item.GetComponentInParent<BoltBarView>().Deselected(item);

        private void AdjustCamera(int barCount)
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
