using DG.Tweening;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ReplicaProjects.MagicSort
{
    public class TubeBarView : MonoBehaviour, DiscreteItemBoard.IItemAnimator
    {
        [SerializeField] private AudioClip _Appear;
        [SerializeField] private AudioClip _Up;
        [SerializeField] private AudioClip _Down;
        [SerializeField] private AudioClip _Win;
        [SerializeField] private AudioSource _AudioSource;

        [SerializeField] private GameObject _BallPrefab;
        [SerializeField] private Transform _BallHolder;
        [SerializeField] private Transform _TopPositionHolder;
        [SerializeField] private TubeInteraction _Interaction;

        public event Action Clicked;

        public Transform TopPositionHolder => _TopPositionHolder;

        private const float BALL_OFFSET_Y = 0.6f;

        private const float INTRO_STAGGER = 0.08f;       // toplar arası beliriş gecikmesi
        private const float INTRO_SCALE_DURATION = 0.1f;
        private const float INTRO_SETTLE_DURATION = 0.2f;   // scale-in bitince "pop" zıplaması
        private const float INTRO_SETTLE_STRENGTH = 0.2f;   // base scale'e oranla punch şiddeti
        private const float SELECT_MOVE_DURATION = 0.1f;
        private const float DESELECT_MOVE_DURATION = 0.2f;   // slota düşüp sert duvara çarpmış gibi zıplayarak oturur (OutBounce)

        private List<Color> _palette;
        private Vector3 _ballBaseScale = Vector3.one;

        // Tema root'u palette'i Setup'tan önce enjekte eder
        public void SetPalette(List<Color> palette) => _palette = palette;

        /// <summary>Renk verisinden topları yaratır. Dönüş: alttan üste sırayla (index 0 = alt slot).</summary>
        public List<GameObject> Setup(BarVisualData data)
        {
            var items = new List<GameObject>(data.Colors.Length);

            for (int i = 0; i < data.Colors.Length; i++)
            {
                int colorIndex = data.Colors[i];
                if (colorIndex == MagicSortLevel.Empty)
                    continue;

                GameObject ball = Instantiate(_BallPrefab, _BallHolder);
                _ballBaseScale = ball.transform.localScale;

                // Intro: son slot pozunda, görünmez (scale 0) başla; PlayIntroAnimation scale'ler.
                ball.transform.localPosition = new Vector3(0f, BALL_OFFSET_Y * i, 0f);
                ball.transform.localScale = Vector3.zero;

                var spriteRenderer = ball.GetComponentInChildren<SpriteRenderer>();
                spriteRenderer.color = _palette[colorIndex];

                items.Add(ball);
            }

            _Interaction.PointerDown += OnPointerDown;

            return items;
        }

        public void Teardown() => _Interaction.PointerDown -= OnPointerDown;

        /// <summary>Barın şu anki topları, alttan üste sırayla (sibling order = slot order).</summary>
        public List<GameObject> ItemsBottomToTop()
        {
            var items = new List<GameObject>(_BallHolder.childCount);
            for (int i = 0; i < _BallHolder.childCount; i++)
                items.Add(_BallHolder.GetChild(i).gameObject);
            return items;
        }

        public void PlaySolvedSound()
        {
            _AudioSource.PlayOneShot(_Win);
        }

        private void OnPointerDown() => Clicked?.Invoke();

        /// <summary>items: alttan üste sırayla (Setup'ın döndürdüğü sıra). Toplar yerinde scale-in olur.</summary>
        public Sequence PlayIntroAnimation(IReadOnlyList<GameObject> items)
        {
            var sequence = DOTween.Sequence();

            for (int slot = 0; slot < items.Count; slot++)
            {
                GameObject introBall = items[slot];
                float appearTime = slot * INTRO_STAGGER;

                sequence.Insert(appearTime,
                    introBall.transform.DOScale(_ballBaseScale, INTRO_SCALE_DURATION).SetEase(Ease.OutCubic));

                // Scale-in biter bitmez slota oturur gibi bir "pop" zıplaması
                sequence.Insert(appearTime + INTRO_SCALE_DURATION,
                    introBall.transform.DOPunchScale(_ballBaseScale * INTRO_SETTLE_STRENGTH, INTRO_SETTLE_DURATION, 1));
            }

            // Boş bar → boş sequence; OnComplete'in (dolayısıyla Unlock'un) yine de tetiklenmesi için.
            sequence.AppendInterval(0f);

            return sequence.SetLink(gameObject, LinkBehaviour.KillOnDisable);
        }

        // DiscreteItemBoard.IItemAnimator
        public Sequence MoveItem(GameObject item, int targetBar, int targetSlot, Vector3 sourceTopWorldPos, float delay)
        {
            DOTween.Kill(item);

            item.transform.SetParent(_BallHolder);
            var ballSlotPosition = new Vector3(0f, targetSlot * BALL_OFFSET_Y, 0f);

            var sourceLocalTarget = item.transform.parent.InverseTransformPoint(sourceTopWorldPos);
            var localTarget = item.transform.parent.InverseTransformPoint(_TopPositionHolder.position);
            var topMovementDelay = delay > 0 ? 0.15f : 0.05f;

            var sequence = DOTween.Sequence()
                .SetDelay(delay)
                .Append(item.transform.DOLocalMove(sourceLocalTarget, topMovementDelay).SetEase(Ease.Linear))
                .Append(item.transform.DOLocalMove(localTarget, 0.1f).SetEase(Ease.Linear))
                .Append(item.transform.DOLocalMove(ballSlotPosition, 0.1f).SetEase(Ease.Linear)
                 .OnComplete(() =>
                 {
                     _AudioSource.PlayOneShot(_Down);
                 }))
                .SetId(item)
                .SetLink(item, LinkBehaviour.KillOnDisable);

            return sequence;
        }

        public void Selected(GameObject item)
        {
            DOTween.Kill(item);

            var localTarget = item.transform.parent.InverseTransformPoint(_TopPositionHolder.position);

            DOTween.Sequence()
                .Append(item.transform.DOLocalMove(localTarget, SELECT_MOVE_DURATION).SetEase(Ease.Linear))
                   .OnComplete(() =>
                   {
                       _AudioSource.PlayOneShot(_Up);
                   })
                .SetId(item)
                .SetLink(item, LinkBehaviour.KillOnDisable);
        }

        public void Deselected(GameObject item)
        {
            DOTween.Kill(item);

            var pos = new Vector3(0f, item.transform.GetSiblingIndex() * BALL_OFFSET_Y, 0f);

            DOTween.Sequence()
                .Append(item.transform.DOLocalMove(pos, DESELECT_MOVE_DURATION).SetEase(Ease.OutBounce))
                   .OnComplete(() =>
                   {
                       _AudioSource.PlayOneShot(_Down);
                   })
                .SetId(item)
                .SetLink(item, LinkBehaviour.KillOnDisable);
        }
    }
}
