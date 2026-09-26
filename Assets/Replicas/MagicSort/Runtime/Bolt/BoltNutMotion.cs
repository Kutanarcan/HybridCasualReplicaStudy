using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace ReplicaProjects.MagicSort
{
    /// <summary>
    /// Nut choreography of one bolt: intro drop, lift on select (with a wobble), screw back down on
    /// deselect, and the travel onto another bolt. Every tween is id'd by its nut, so killing the
    /// nut's tweens also kills the wobble.
    /// </summary>
    public sealed class BoltNutMotion
    {
        private const float NUT_SPACING = 0.5f;
        private const float INTRO_STAGGER = 0.4f;        // somunlar arası belirme gecikmesi
        private const float INTRO_TOP_MOVE_DURATION = 0.25f;
        private const float DESELECT_MOVE_DURATION = 0.5f;
        private const float DESELECT_ROTATE_DURATION = 0.65f;

        private const float WOBBLE_TILT = 7f;
        private const float WOBBLE_PERIOD = 2.5f;
        private const float WOBBLE_FADE_IN = 0.5f;

        private static int _lastIntroSfxFrame = -1; // aynı frame'de tüm barlar drop başlatınca sesi tek sefer çal

        private readonly BoltSounds _sounds;
        private readonly Transform _nutHolder;
        private readonly Transform _topPositionHolder;
        private readonly GameObject _owner;

        public Vector3 NutBaseScale { get; set; } = Vector3.one;

        public BoltNutMotion(BoltSounds sounds, Transform nutHolder, Transform topPositionHolder, GameObject owner)
        {
            _sounds = sounds;
            _nutHolder = nutHolder;
            _topPositionHolder = topPositionHolder;
            _owner = owner;
        }

        public static Vector3 SlotPosition(int slot) => new(0f, slot * NUT_SPACING, 0f);

        /// <summary>items: alttan üste sırayla; slot = dizideki index.</summary>
        public Sequence PlayIntro(IReadOnlyList<GameObject> items)
        {
            var sequence = DOTween.Sequence();
            var topLocal = _nutHolder.InverseTransformPoint(_topPositionHolder.position);

            for (int slot = 0; slot < items.Count; slot++)
            {
                var nut = items[slot].transform;

                // Alttaki somun önce belirsin: slot 0 → gecikme 0
                float appearTime = slot * INTRO_STAGGER;
                float dropTime = appearTime + INTRO_TOP_MOVE_DURATION;

                sequence.InsertCallback(appearTime, () => nut.localScale = NutBaseScale);
                sequence.Insert(appearTime, nut.DOLocalMove(topLocal, INTRO_TOP_MOVE_DURATION).SetEase(Ease.Linear));

                // Sonra DeSelect animasyonu: slota inerken 360 dön
                sequence.InsertCallback(dropTime, PlayIntroDropSfx);
                sequence.Insert(dropTime, nut.DOLocalMove(SlotPosition(slot), DESELECT_MOVE_DURATION).SetEase(Ease.Linear));
                sequence.Insert(dropTime, nut.DORotate(new Vector3(0, 360, 0), DESELECT_ROTATE_DURATION, RotateMode.FastBeyond360));
            }

            return sequence.SetLink(_owner, LinkBehaviour.KillOnDisable);
        }

        public Sequence MoveToSlot(GameObject item, Vector3 sourceTopWorldPosition, int targetSlot, float delay)
        {
            DOTween.Kill(item); // item ID'li her şeyi öldürür (wobble dahil)

            var nut = item.transform;
            nut.SetParent(_nutHolder);
            nut.eulerAngles = Vector3.zero;

            var sourceLocalTarget = _nutHolder.InverseTransformPoint(sourceTopWorldPosition);
            var localTarget = _nutHolder.InverseTransformPoint(_topPositionHolder.position);
            var rotationDelayApply = delay > 0 ? 1 : 0;
            var topMovementDuration = delay > 0 ? 0.25f : 0.1f;

            return DOTween.Sequence()
                .SetDelay(delay)
                .Append(nut.DOLocalMove(sourceLocalTarget, topMovementDuration).SetEase(Ease.Linear))
                .Join(nut.DORotate(new Vector3(0, -360 * rotationDelayApply, 0), 0.25f * rotationDelayApply, RotateMode.FastBeyond360))
                .Append(nut.DOLocalMove(localTarget, 0.25f).SetEase(Ease.Linear).OnComplete(_sounds.PlayDown))
                .Append(nut.DOLocalMove(SlotPosition(targetSlot), 0.35f).SetEase(Ease.Linear))
                .Join(nut.DORotate(new Vector3(0, 360 * 2, 0), 0.35f, RotateMode.FastBeyond360))
                .OnComplete(_sounds.PlaySeat)
                .SetId(item)
                .SetLink(item, LinkBehaviour.KillOnDisable);
        }

        public void Select(GameObject item)
        {
            DOTween.Kill(item);

            var localTarget = item.transform.parent.InverseTransformPoint(_topPositionHolder.position);
            _sounds.PlayUp();

            DOTween.Sequence()
                .Append(item.transform.DOLocalMove(localTarget, 0.5f).SetEase(Ease.Linear))
                .Join(item.transform.DORotate(new Vector3(0, -360, 0), 0.65f, RotateMode.FastBeyond360))
                .AppendCallback(() => StartWobble(item))
                .SetId(item)
                .SetLink(item, LinkBehaviour.KillOnDisable);
        }

        public void Deselect(GameObject item)
        {
            DOTween.Kill(item); // item ID'li her şeyi öldürür (wobble dahil)

            // Çocuklar hep alttan üste sırayla eklenir (Setup) / en üste eklenir (MoveToSlot);
            // bu yüzden sibling index, item'in kendi barındaki slotuna eşittir.
            item.transform.eulerAngles = Vector3.zero;
            _sounds.PlayDown();

            DOTween.Sequence()
                .Append(item.transform.DOLocalMove(SlotPosition(item.transform.GetSiblingIndex()), DESELECT_MOVE_DURATION).SetEase(Ease.Linear))
                .Join(item.transform.DORotate(new Vector3(0, 360, 0), DESELECT_ROTATE_DURATION, RotateMode.FastBeyond360))
                .InsertCallback(DESELECT_ROTATE_DURATION - 0.05f, _sounds.PlaySeat)
                .SetId(item)
                .SetLink(item, LinkBehaviour.KillOnDisable);
        }

        // Lifted nut circles its tilt around the vertical axis, fading the tilt in.
        private static void StartWobble(GameObject item)
        {
            float baseY = item.transform.eulerAngles.y;
            float elapsed = 0f;

            DOVirtual.Float(0f, Mathf.PI * 2f, WOBBLE_PERIOD, angle =>
                {
                    elapsed += Time.deltaTime;
                    float amplitude = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / WOBBLE_FADE_IN));
                    float x = Mathf.Cos(angle) * WOBBLE_TILT * amplitude;
                    float z = -Mathf.Sin(angle) * WOBBLE_TILT * amplitude;
                    item.transform.rotation = Quaternion.Euler(x, baseY, z);
                })
                .SetEase(Ease.Linear)
                .SetLoops(-1, LoopType.Restart)
                .SetId(item) // DOTween.Kill(item) ile ölebilsin
                .SetLink(item, LinkBehaviour.KillOnDisable);
        }

        private void PlayIntroDropSfx()
        {
            if (Time.frameCount == _lastIntroSfxFrame)
                return;

            _lastIntroSfxFrame = Time.frameCount;
            _sounds.PlayDown();
        }
    }
}
