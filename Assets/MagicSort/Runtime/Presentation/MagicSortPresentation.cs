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
        private const float ROW_Y_OFFSET = 3.0F;   // ekran aralığı: satır başına ekstra düşüş
        private const float ROW_Z_OFFSET = 2.0F;   // derinlik: ön satırlar arkadakileri örter
        private const int MAX_COLUMNS = 3;

        private const float BAR_VISUAL_HEIGHT = 2.5F; // somun yığını + kapak için üst pay
        private const float CAMERA_PAD_X = 1.0F;
        private const float CAMERA_PAD_Y = 0.75F;

        public readonly Vector3 Offset = new Vector3(X_OFFSET, ROW_Y_OFFSET, ROW_Z_OFFSET);

        private static MaterialPropertyBlock _mpb;

        private List<MagicSortBarView> _magicSortBarViewList;
        private GameObject _barsRoot;

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

            Destroy(_barsRoot); // SetLink(KillOnDisable) sayesinde çalışan tween'ler de ölür
            _barsRoot = null;
            _magicSortBarViewList = null;
        }

        private void CreateItems(List<BarVisualData> bars)
        {
            _barsRoot = new GameObject("BarsRoot");
            _barsRoot.transform.SetParent(transform, false);

            _magicSortBarViewList = new List<MagicSortBarView>(bars.Count);

            for (int i = 0; i < bars.Count; i++)
            {
                int col = i % MAX_COLUMNS;
                int row = i / MAX_COLUMNS;

                MagicSortBarView barView = Instantiate(_MagicSortBarViewPrefab, _barsRoot.transform);
                barView.transform.localPosition = new Vector3(col * Offset.x, -row * Offset.y, -row * Offset.z);

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
