using DG.Tweening;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace ReplicaProjects.MagicSort
{
    public class BarVisualData
    {
        public List<int> colorList;
    }

    public class MagicSortPresentation : MonoBehaviour
    {
        public event Action<int> AnyBarViewClicked;

        [SerializeField] private MagicSortBarView _MagicSortBarViewPrefab;
        [SerializeField] private List<Color> _colorPalette;

        private const float X_OFFSET = 1.75F;
        private const float Y_OFFSET = 4.0F;
        private const int MAX_COLUMNS = 3;

        private const float CAMERA_Y_BASE = 10.0F;
        private const float CAMERA_Y_PER_ROW = -3.0F;
        private const float CAMERA_SIZE_BASE = 3.5F;
        private const float CAMERA_SIZE_PER_ROW = 0.75F;

        public readonly Vector2 Offset = new Vector2(X_OFFSET, Y_OFFSET);

        private static MaterialPropertyBlock _mpb;

        private List<MagicSortBarView> _magicSortBarViewList;

        public void Initialize(List<BarVisualData> bars)
        {
            CreateItems(bars);
            FitCamera(bars.Count);
        }

        public void DeInitialize()
        {
            for (int i = _magicSortBarViewList.Count - 1; i >= 0; i--)
            {
                var view = _magicSortBarViewList[i];

                if (view == null)
                    continue;

                view.Clicked -= OnAnyBarViewClicked;
                view.DeInitialize();
            }
        }

        private void CreateItems(List<BarVisualData> bars)
        {
            GameObject barsRoot = new GameObject("BarsRoot");
            barsRoot.transform.SetParent(transform, false);

            _magicSortBarViewList = new List<MagicSortBarView>(bars.Count);

            for (int i = 0; i < bars.Count; i++)
            {
                int col = i % MAX_COLUMNS;
                int row = i / MAX_COLUMNS;

                MagicSortBarView barView = Instantiate(_MagicSortBarViewPrefab, barsRoot.transform);
                barView.transform.localPosition = new Vector3(col * Offset.x, -row * Offset.y, 0f);

                barView.Initialize(ResolveColors(bars[i].colorList), i);

                _magicSortBarViewList.Add(barView);

                barView.Clicked += OnAnyBarViewClicked;
            }
        }

        private void OnAnyBarViewClicked(int index)
        {
            AnyBarViewClicked?.Invoke(index);
        }

        private List<Color> ResolveColors(List<int> colorIndices)
        {
            var colors = new List<Color>(colorIndices.Count);

            for (int i = 0; i < colorIndices.Count; i++)
            {
                int colorIndex = colorIndices[i];
                if (colorIndex == MagicSortLevel.Empty)
                    continue;

                colors.Add(_colorPalette[colorIndex]);
            }

            return colors;
        }

        private void FitCamera(int barCount)
        {
            Camera cam = Camera.main;
            if (cam == null)
                return;

            int columns = Mathf.Min(barCount, MAX_COLUMNS);
            int rows = Mathf.CeilToInt(barCount / (float)MAX_COLUMNS);

            float boardWidth = (columns - 1) * Offset.x;

            Vector3 pos = cam.transform.position;
            pos.x = boardWidth / 2f;
            pos.y = CAMERA_Y_BASE + CAMERA_Y_PER_ROW * rows;
            cam.transform.position = pos;

            cam.orthographicSize = CAMERA_SIZE_BASE + CAMERA_SIZE_PER_ROW * rows;
        }

        public void HandleTapResponse(TapResult result)
        {
            //Debug.Log($"Tap Kind: {result.Kind} \n" +
            //    $"-> Source Bar Index {result.SourceBar}" +
            //    $"-> New Source Bar Index {result.NewSource}" +
            //    $"-> Target Bar Index {result.TargetBar}" +
            //    $"");

            switch (result.Kind)
            {
                case TapKind.Ignored:
                    break;
                case TapKind.SourceSelected:
                    SourceSelectedState(result);
                    break;
                case TapKind.Deselected:
                    SourceDeSelectedState(result);
                    break;
                case TapKind.Retargeted:
                    ReTargettedState(result);
                    break;
                case TapKind.Consumed:
                    ConsumeState(result);
                    break;
                default:
                    break;
            }
        }

        private void ReTargettedState(TapResult result)
        {
            if (!_magicSortBarViewList[result.SourceBar].TryPeekNut(out var nutSource))
                return;

            _magicSortBarViewList[result.SourceBar].MoveNutDeSelectedAnimation(nutSource);

            if (!_magicSortBarViewList[result.NewSource].TryPeekNut(out var nutTarget))
                return;

            _magicSortBarViewList[result.NewSource].MoveNutSelectedAnimation(nutTarget);
        }


        private void SourceDeSelectedState(TapResult result)
        {
            if (!_magicSortBarViewList[result.SourceBar].TryPeekNut(out var nut))
                return;

            _magicSortBarViewList[result.SourceBar].MoveNutDeSelectedAnimation(nut);
        }

        private void SourceSelectedState(TapResult result)
        {
            if (!_magicSortBarViewList[result.SourceBar].TryPeekNut(out var nut))
                return;

            _magicSortBarViewList[result.SourceBar].MoveNutSelectedAnimation(nut);
        }

        private void ConsumeState(TapResult result)
        {
            for (int i = 0; i < result.Pour.movedCount; i++)
            {
                var sourceBar = _magicSortBarViewList[result.SourceBar];

                if (!sourceBar.TryPopNut(out var nut))
                    continue;

                var targetBar = _magicSortBarViewList[result.TargetBar];

                if (targetBar == null)
                    continue;

                nut.transform.SetParent(null);
                targetBar.PushNut(nut);
                var sequence = targetBar.MoveBarSlotAnimation(nut, sourceBar.topPositionHolder.position, 0.25f * i);

                bool isLastNut = i == result.Pour.movedCount - 1;
                if (isLastNut && result.TargetBarSolved)
                    sequence.OnComplete(targetBar.PlayCapSolvedAnimation);
            }
        }
    }
}
