using DG.Tweening;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ReplicaProjects.MagicSort
{
    public abstract class MagicSortBarViewBase : MonoBehaviour
    {
        public event Action<int> Clicked;

        protected readonly Stack<GameObject> ItemStack = new();
        protected int Index { get; private set; }

        public abstract Transform TopPositionHolder { get; }

        public bool TryPeekItem(out GameObject item) => ItemStack.TryPeek(out item);

        public bool TryPopItem(out GameObject item) => ItemStack.TryPop(out item);

        public virtual void PushItem(GameObject item) => ItemStack.Push(item);

        protected void RaiseClicked() => Clicked?.Invoke(Index);

        public void Initialize(BarVisualData data, int barIndex)
        {
            Index = barIndex;
            OnInitialize(data);
        }

        protected abstract void OnInitialize(BarVisualData data);

        public abstract void DeInitialize();

        // Tema animasyon API'si — HandleTapResponse sadece bunları çağırır
        public abstract Sequence PlayIntroAnimation();
        public abstract Sequence MoveItemToSlotAnimation(GameObject item, Vector3 sourceTopWorldPosition, float delay);
        public abstract Sequence PlaySolvedAnimation();
        public abstract void PlayItemSelectedAnimation(GameObject item);
        public abstract void PlayItemDeselectedAnimation(GameObject item);
        public abstract void SetSolvedInstant(); // tema değişiminde tween'siz kapak pozu
    }
}
