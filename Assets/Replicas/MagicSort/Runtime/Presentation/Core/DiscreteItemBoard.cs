using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace ReplicaProjects.MagicSort
{
    /// <summary>
    /// Ayrık objelerle çalışan temaların (Bolt, Ball) ortak koreografi motoru.
    /// LiquidTheme bu sınıfı HİÇ bilmez.
    /// </summary>
    public sealed class DiscreteItemBoard
    {
        public interface IItemAnimator
        {
            // Tema, tek bir objenin nasıl uçacağını tanımlar; sıralamayı motor yönetir.
            Sequence MoveItem(GameObject item, int targetBar, int targetSlot,
                              Vector3 sourceTopWorldPos, float delay, bool isLast);
            void Selected(GameObject item);
            void Deselected(GameObject item);
        }

        private readonly List<Stack<GameObject>> _stacks;
        private readonly IItemAnimator _animator;
        private const float STAGGER = 0.25f;

        public DiscreteItemBoard(int barCount, IItemAnimator animator)
        {
            _animator = animator;
            _stacks = new List<Stack<GameObject>>(barCount);
            for (int i = 0; i < barCount; i++) _stacks.Add(new Stack<GameObject>());
        }

        public void Push(int bar, GameObject item) => _stacks[bar].Push(item);
        public bool TryPeek(int bar, out GameObject item) => _stacks[bar].TryPeek(out item);

        public void Selected(int bar)
        {
            if (_stacks[bar].TryPeek(out var item))
                _animator.Selected(item);
        }

        public void Deselected(int bar)
        {
            if (_stacks[bar].TryPeek(out var item))
                _animator.Deselected(item);
        }

        public void PlayTransport(in TransportData cmd, Vector3 sourceTopWorldPos, Action onComplete)
        {
            Sequence last = null;
            for (int i = 0; i < cmd.MovedCount; i++)
            {
                if (!_stacks[cmd.SourceBar].TryPop(out var item)) break;
                _stacks[cmd.TargetBar].Push(item);
                int slot = _stacks[cmd.TargetBar].Count - 1;
                bool isLast = i == cmd.MovedCount - 1;
                last = _animator.MoveItem(item, cmd.TargetBar, slot, sourceTopWorldPos, STAGGER * i, isLast);
            }
            if (last != null) last.OnComplete(() => onComplete());
            else onComplete();
        }
    }
}
