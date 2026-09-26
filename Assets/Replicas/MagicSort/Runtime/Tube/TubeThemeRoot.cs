using DG.Tweening;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ReplicaProjects.MagicSort
{
    public class TubeThemeRoot : MonoBehaviour, ISortTheme, DiscreteItemBoard.IItemAnimator
    {
        public event Action<int> BarTapped;

        [SerializeField] private TubeBarView _BarViewPrefab;
        [SerializeField] private ColorPalette _colorPalette;

        private static readonly BarGridLayout Layout = new(columns: 3, columnSpacing: 1.25f, rowDrop: 4.5f, rowDepth: 0f);

        private const float BAR_VISUAL_HEIGHT = 4.5F; // tüp yüksekliği için üst pay (gerekirse ayarla)
        private static readonly Vector2 CameraPadding = new(1.0f, 0.75f);

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
                _views.Add(SpawnBar(i, bars[i]));

            AdjustCamera(bars.Count);
        }

        private TubeBarView SpawnBar(int barIndex, BarVisualData data)
        {
            Layout.Position(barIndex, out float x, out float y, out float z);

            TubeBarView barView = Instantiate(_BarViewPrefab, _barsRoot.transform);
            barView.transform.localPosition = new Vector3(x, y, z);
            barView.SetPalette(_colorPalette.colorList);
            barView.Clicked += () => BarTapped?.Invoke(barIndex);

            foreach (var item in barView.Setup(data))
                _board.Push(barIndex, item);

            return barView;
        }

        public void Teardown()
        {
            foreach (var view in _views)
                if (view != null)
                    view.Teardown();

            Destroy(_barsRoot); // SetLink(KillOnDisable) sayesinde çalışan tween'ler de ölür
            _barsRoot = null;
            _views = null;
            _board = null;
        }

        public void PlayIntro(Action onComplete)
        {
            var counter = new CompletionCounter(_views.Count, onComplete);
            foreach (var view in _views)
                view.PlayIntroAnimation(view.ItemsBottomToTop()).OnComplete(counter.Signal);
        }

        public void ShowSelected(int bar) => _board.Selected(bar);
        public void ShowDeselected(int bar) => _board.Deselected(bar);

        public void PlayTransport(in TransportData command, Action onComplete)
            => _board.PlayTransport(in command, _views[command.SourceBar].TopPositionHolder.position, onComplete);

        // Tubes have no solved pose; only the sound plays.
        public void ShowSolved(int bar, bool animated) => _views[bar].PlaySolvedSound();

        // DiscreteItemBoard.IItemAnimator: her top kendi bar view'ının içinde yaşar,
        // bu yüzden hedef bar view'a devredilir.
        public Sequence MoveItem(GameObject item, int targetBar, int targetSlot, Vector3 sourceTopWorldPos, float delay, bool isLast)
            => _views[targetBar].MoveItem(item, targetBar, targetSlot, sourceTopWorldPos, delay, isLast);

        public void Selected(GameObject item) => item.GetComponentInParent<TubeBarView>().Selected(item);
        public void Deselected(GameObject item) => item.GetComponentInParent<TubeBarView>().Deselected(item);

        // The tube sprite is centred on the bar origin (pivot = center), so the vertical span is
        // symmetric around it; otherwise the board would drift vertically.
        private void AdjustCamera(int barCount)
        {
            Camera cam = Camera.main;
            if (cam == null)
                return;

            float half = BAR_VISUAL_HEIGHT * 0.5f;
            ThemeCameraFitter.Fit(cam, transform, Layout, barCount, -half, half, CameraPadding);
        }
    }
}
