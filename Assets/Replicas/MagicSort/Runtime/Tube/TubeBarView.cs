using DG.Tweening;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ReplicaProjects.MagicSort
{
    /// <summary>One tube: spawns its balls and reports taps. Ball motion lives in TubeBallMotion.</summary>
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

        private List<Color> _palette;
        private TubeBallMotion _motion;

        private void Awake()
        {
            _motion = new TubeBallMotion(_AudioSource, _Up, _Down, _BallHolder, _TopPositionHolder, gameObject);
        }

        // Tema root'u palette'i Setup'tan önce enjekte eder
        public void SetPalette(List<Color> palette) => _palette = palette;

        /// <summary>Renk verisinden topları yaratır. Dönüş: alttan üste sırayla (index 0 = alt slot).</summary>
        public List<GameObject> Setup(BarVisualData data)
        {
            var items = new List<GameObject>(data.Colors.Length);

            for (int i = 0; i < data.Colors.Length; i++)
            {
                int colorIndex = data.Colors[i];
                if (colorIndex == ColorSlot.Empty)
                    continue;

                GameObject ball = Instantiate(_BallPrefab, _BallHolder);
                _motion.BallBaseScale = ball.transform.localScale;

                // Intro: son slot pozunda, görünmez (scale 0) başla; PlayIntroAnimation scale'ler.
                ball.transform.localPosition = TubeBallMotion.SlotPosition(i);
                ball.transform.localScale = Vector3.zero;
                ball.GetComponentInChildren<SpriteRenderer>().color = _palette[colorIndex];

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

        public void PlaySolvedSound() => _AudioSource.PlayOneShot(_Win);

        private void OnPointerDown() => Clicked?.Invoke();

        public Sequence PlayIntroAnimation(IReadOnlyList<GameObject> items) => _motion.PlayIntro(items);

        // DiscreteItemBoard.IItemAnimator
        public Sequence MoveItem(GameObject item, int targetBar, int targetSlot, Vector3 sourceTopWorldPos, float delay, bool isLast)
            => _motion.MoveToSlot(item, sourceTopWorldPos, targetSlot, delay, isLast);

        public void Selected(GameObject item) => _motion.Select(item);
        public void Deselected(GameObject item) => _motion.Deselect(item);
    }
}
