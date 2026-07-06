using DG.Tweening;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ReplicaProjects.MagicSort
{
    public abstract class MagicSortThemeRoot : MonoBehaviour
    {
        public event Action<int> AnyBarViewClicked;

        protected List<MagicSortBarViewBase> BarViewList;
        protected GameObject BarsRoot;
        private bool _inputLocked;

        public void Initialize(List<BarVisualData> bars)
        {
            BarsRoot = new GameObject("BarsRoot");
            BarsRoot.transform.SetParent(transform, false);

            BarViewList = CreateBarViews(bars);

            for (int i = 0; i < BarViewList.Count; i++)
            {
                BarViewList[i].Clicked += OnAnyBarViewClicked;

                if (bars[i].isSolved)
                    BarViewList[i].SetSolvedInstant();
            }

            AdjustCamera(bars.Count);
            PlayIntroAnimations();
        }

        public void DeInitialize()
        {
            for (int i = BarViewList.Count - 1; i >= 0; i--)
            {
                var view = BarViewList[i];

                if (view == null)
                    continue;

                view.Clicked -= OnAnyBarViewClicked;
                view.DeInitialize();
            }

            Destroy(BarsRoot); // SetLink(KillOnDisable) sayesinde çalışan tween'ler de ölür
            BarsRoot = null;
            BarViewList = null;
        }

        // Tema yüzeyi: her tema kendi bar view'larını yaratır ve BarsRoot altına yerleştirir
        protected abstract List<MagicSortBarViewBase> CreateBarViews(List<BarVisualData> bars);

        // 3D/2D temalar override eder; canvas temaları için no-op
        protected virtual void AdjustCamera(int barCount) { }

        private void PlayIntroAnimations()
        {
            _inputLocked = true;

            int remaining = BarViewList.Count;

            foreach (var view in BarViewList)
            {
                view.PlayIntroAnimation().OnComplete(() =>
                {
                    remaining--;
                    if (remaining == 0)
                        _inputLocked = false;
                });
            }
        }

        private void OnAnyBarViewClicked(int index)
        {
            if (_inputLocked)
                return;

            AnyBarViewClicked?.Invoke(index);
        }

        public void HandleTapResponse(TapResult result)
        {
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
            if (!BarViewList[result.SourceBar].TryPeekItem(out var itemSource))
                return;

            BarViewList[result.SourceBar].PlayItemDeselectedAnimation(itemSource);

            if (!BarViewList[result.NewSource].TryPeekItem(out var itemTarget))
                return;

            BarViewList[result.NewSource].PlayItemSelectedAnimation(itemTarget);
        }

        private void SourceDeSelectedState(TapResult result)
        {
            if (!BarViewList[result.SourceBar].TryPeekItem(out var item))
                return;

            BarViewList[result.SourceBar].PlayItemDeselectedAnimation(item);
        }

        private void SourceSelectedState(TapResult result)
        {
            if (!BarViewList[result.SourceBar].TryPeekItem(out var item))
                return;

            BarViewList[result.SourceBar].PlayItemSelectedAnimation(item);
        }

        private void ConsumeState(TapResult result)
        {
            for (int i = 0; i < result.Pour.movedCount; i++)
            {
                var sourceBar = BarViewList[result.SourceBar];

                if (!sourceBar.TryPopItem(out var item))
                    continue;

                var targetBar = BarViewList[result.TargetBar];

                if (targetBar == null)
                    continue;

                item.transform.SetParent(null);
                targetBar.PushItem(item);
                var sequence = targetBar.MoveItemToSlotAnimation(item, sourceBar.TopPositionHolder.position, 0.25f * i);

                bool isLastItem = i == result.Pour.movedCount - 1;
                if (isLastItem && result.TargetBarSolved)
                    sequence.Append(targetBar.PlaySolvedAnimation())
                        .SetLink(targetBar.gameObject);
            }
        }
    }
}
