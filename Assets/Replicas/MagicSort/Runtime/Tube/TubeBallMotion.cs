using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace ReplicaProjects.MagicSort
{
    /// <summary>
    /// Ball choreography of one tube: pop-in intro, lift on select, bounce back on deselect and the
    /// hop onto another tube. Every tween is id'd by its ball.
    /// </summary>
    public sealed class TubeBallMotion
    {
        private const float BALL_OFFSET_Y = 0.6f;

        private const float INTRO_STAGGER = 0.08f;          // toplar arası beliriş gecikmesi
        private const float INTRO_SCALE_DURATION = 0.1f;
        private const float INTRO_SETTLE_DURATION = 0.2f;   // scale-in bitince "pop" zıplaması
        private const float INTRO_SETTLE_STRENGTH = 0.2f;   // base scale'e oranla punch şiddeti
        private const float SELECT_MOVE_DURATION = 0.1f;
        private const float DESELECT_MOVE_DURATION = 0.2f;  // slota düşüp sert duvara çarpmış gibi zıplayarak oturur (OutBounce)
        private const float MOVE_SLOT_DURATION = 0.1f;      // taşınan topların hızlı, düz oturması
        private const float MOVE_SETTLE_DURATION = 0.25f;   // grubun SON topu: OutBounce ile oturur

        private readonly AudioSource _audio;
        private readonly AudioClip _up;
        private readonly AudioClip _down;
        private readonly Transform _ballHolder;
        private readonly Transform _topPositionHolder;
        private readonly GameObject _owner;

        public Vector3 BallBaseScale { get; set; } = Vector3.one;

        public TubeBallMotion(AudioSource audio, AudioClip up, AudioClip down,
                              Transform ballHolder, Transform topPositionHolder, GameObject owner)
        {
            _audio = audio;
            _up = up;
            _down = down;
            _ballHolder = ballHolder;
            _topPositionHolder = topPositionHolder;
            _owner = owner;
        }

        public static Vector3 SlotPosition(int slot) => new(0f, slot * BALL_OFFSET_Y, 0f);

        /// <summary>items: alttan üste sırayla. Toplar yerinde scale-in olur.</summary>
        public Sequence PlayIntro(IReadOnlyList<GameObject> items)
        {
            var sequence = DOTween.Sequence();

            for (int slot = 0; slot < items.Count; slot++)
            {
                var ball = items[slot].transform;
                float appearTime = slot * INTRO_STAGGER;

                sequence.Insert(appearTime, ball.DOScale(BallBaseScale, INTRO_SCALE_DURATION).SetEase(Ease.OutCubic));

                // Scale-in biter bitmez slota oturur gibi bir "pop" zıplaması
                sequence.Insert(appearTime + INTRO_SCALE_DURATION,
                    ball.DOPunchScale(BallBaseScale * INTRO_SETTLE_STRENGTH, INTRO_SETTLE_DURATION, 1));
            }

            // Boş bar → boş sequence; OnComplete'in (dolayısıyla Unlock'un) yine de tetiklenmesi için.
            sequence.AppendInterval(0f);

            return sequence.SetLink(_owner, LinkBehaviour.KillOnDisable);
        }

        /// <summary>The group's last (topmost) ball bounces into its slot; the others land fast and flat.</summary>
        public Sequence MoveToSlot(GameObject item, Vector3 sourceTopWorldPos, int targetSlot, float delay, bool isLast)
        {
            DOTween.Kill(item);

            var ball = item.transform;
            ball.SetParent(_ballHolder);

            var sourceLocalTarget = _ballHolder.InverseTransformPoint(sourceTopWorldPos);
            var localTarget = _ballHolder.InverseTransformPoint(_topPositionHolder.position);
            var topMovementDuration = delay > 0 ? 0.15f : 0.05f;

            float slotDuration = isLast ? MOVE_SETTLE_DURATION : MOVE_SLOT_DURATION;
            Ease slotEase = isLast ? Ease.OutBounce : Ease.Linear;

            return DOTween.Sequence()
                .SetDelay(delay)
                .Append(ball.DOLocalMove(sourceLocalTarget, topMovementDuration).SetEase(Ease.Linear))
                .Append(ball.DOLocalMove(localTarget, 0.1f).SetEase(Ease.Linear))
                .Append(ball.DOLocalMove(SlotPosition(targetSlot), slotDuration).SetEase(slotEase).OnComplete(PlayDown))
                .SetId(item)
                .SetLink(item, LinkBehaviour.KillOnDisable);
        }

        public void Select(GameObject item)
        {
            DOTween.Kill(item);

            var localTarget = item.transform.parent.InverseTransformPoint(_topPositionHolder.position);

            DOTween.Sequence()
                .Append(item.transform.DOLocalMove(localTarget, SELECT_MOVE_DURATION).SetEase(Ease.Linear))
                .OnComplete(PlayUp)
                .SetId(item)
                .SetLink(item, LinkBehaviour.KillOnDisable);
        }

        public void Deselect(GameObject item)
        {
            DOTween.Kill(item);

            DOTween.Sequence()
                .Append(item.transform.DOLocalMove(SlotPosition(item.transform.GetSiblingIndex()), DESELECT_MOVE_DURATION).SetEase(Ease.OutBounce))
                .OnComplete(PlayDown)
                .SetId(item)
                .SetLink(item, LinkBehaviour.KillOnDisable);
        }

        private void PlayUp() => _audio.PlayOneShot(_up);
        private void PlayDown() => _audio.PlayOneShot(_down);
    }
}
