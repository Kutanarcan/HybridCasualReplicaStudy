using DG.Tweening;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ReplicaProjects.MagicSort
{
    public class BoltThemeRoot : MonoBehaviour, ISortTheme, DiscreteItemBoard.IItemAnimator
    {
        public event Action<int> BarTapped;

        [SerializeField] private BoltBarView _BarViewPrefab;
        [SerializeField] private ColorPalette _colorPalette;

        // Rows drop on screen and come forward in depth, so front rows overlap the ones behind.
        private static readonly BarGridLayout Layout = new(columns: 3, columnSpacing: 1.75f, rowDrop: 3.0f, rowDepth: 2.0f);

        private const float BAR_VISUAL_HEIGHT = 2.5F; // somun yığını + kapak için üst pay
        private static readonly Vector2 CameraPadding = new(2f, 2f);
        private static readonly Vector3 CameraEuler = new(35, 0, 0);

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
                _views.Add(SpawnBar(i, bars[i]));

            AdjustCamera(bars.Count);
        }

        private BoltBarView SpawnBar(int barIndex, BarVisualData data)
        {
            Layout.Position(barIndex, out float x, out float y, out float z);

            BoltBarView barView = Instantiate(_BarViewPrefab, _barsRoot.transform);
            barView.transform.localPosition = new Vector3(x, y, z);
            barView.SetPalette(_colorPalette.colorList);
            barView.Clicked += () => BarTapped?.Invoke(barIndex);

            foreach (var item in barView.Setup(data))
                _board.Push(barIndex, item);

            if (data.IsSolved)
                barView.SetSolvedInstant();

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

        public void ShowSolved(int bar, bool animated)
        {
            if (animated)
                _views[bar].PlaySolvedAnimation();
            else
                _views[bar].SetSolvedInstant();
        }

        // DiscreteItemBoard.IItemAnimator: her item kendi bar view'ının içinde yaşar,
        // bu yüzden hedef bar view'a devredilir.
        public Sequence MoveItem(GameObject item, int targetBar, int targetSlot, Vector3 sourceTopWorldPos, float delay, bool isLast)
            => _views[targetBar].MoveItem(item, targetBar, targetSlot, sourceTopWorldPos, delay, isLast);

        public void Selected(GameObject item) => item.GetComponentInParent<BoltBarView>().Selected(item);
        public void Deselected(GameObject item) => item.GetComponentInParent<BoltBarView>().Deselected(item);

        // Bars stand on their origin, so the fit spans origin .. origin + visual height.
        private void AdjustCamera(int barCount)
        {
            Camera cam = Camera.main;
            if (cam == null)
                return;

            cam.transform.eulerAngles = CameraEuler;
            ThemeCameraFitter.Fit(cam, transform, Layout, barCount, 0f, BAR_VISUAL_HEIGHT, CameraPadding);
        }
    }
}
