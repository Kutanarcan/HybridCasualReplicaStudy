using DG.Tweening;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace ReplicaProjects.MagicSort
{
    /// <summary>One bolt: spawns its nuts, reports taps, and plays the cap animation when solved. Nut motion lives in BoltNutMotion.</summary>
    public class BoltBarView : MonoBehaviour, DiscreteItemBoard.IItemAnimator
    {
        [SerializeField] private AudioClip _Up;
        [SerializeField] private AudioClip _Down;
        [SerializeField] private AudioClip _CapWin;
        [SerializeField] private AudioClip _NutSeat;
        [SerializeField] private AudioSource _AudioSource;

        [SerializeField] private GameObject _NutPrefab;
        [SerializeField] private Transform _NutHolder;

        [SerializeField] private Transform _CapEndPositionHolder;
        [SerializeField] private GameObject _Cap;

        [FormerlySerializedAs("topPositionHolder")]
        [SerializeField] private Transform _TopPositionHolder;
        [SerializeField] private BoltInteraction _BoltInteraction;

        public event Action Clicked;

        public Transform TopPositionHolder => _TopPositionHolder;

        private const string COLOR_PROPERTY = "_Color";
        private const float INTRO_START_Z = 1f; // belirme noktası: barın arkası (local z)
        private static readonly Vector3 CapSolvedEuler = new(90, 0, 0);

        private static MaterialPropertyBlock _mpb;

        private List<Color> _palette;
        private BoltSounds _sounds;
        private BoltNutMotion _motion;

        private void Awake()
        {
            _sounds = new BoltSounds(_AudioSource, _Up, _Down, _NutSeat, _CapWin);
            _motion = new BoltNutMotion(_sounds, _NutHolder, _TopPositionHolder, gameObject);
        }

        // Tema root'u palette'i Setup'tan önce enjekte eder
        public void SetPalette(List<Color> palette) => _palette = palette;

        /// <summary>Renk verisinden somunları yaratır. Dönüş: alttan üste sırayla (index 0 = alt slot).</summary>
        public List<GameObject> Setup(BarVisualData data)
        {
            _mpb ??= new MaterialPropertyBlock();

            var items = new List<GameObject>(data.Colors.Length);
            var topLocal = _NutHolder.InverseTransformPoint(_TopPositionHolder.position);

            foreach (int colorIndex in data.Colors)
            {
                if (colorIndex == ColorSlot.Empty)
                    continue;

                GameObject nut = Instantiate(_NutPrefab, _NutHolder);
                _motion.NutBaseScale = nut.transform.localScale;

                // Intro: topPositionHolder yüksekliğinde, barın arkasında (z=1), görünmez (scale 0) başla
                nut.transform.localPosition = new Vector3(0f, topLocal.y, INTRO_START_Z);
                nut.transform.localScale = Vector3.zero;

                var meshRenderer = nut.GetComponentInChildren<MeshRenderer>();
                meshRenderer.GetPropertyBlock(_mpb);
                _mpb.SetColor(COLOR_PROPERTY, _palette[colorIndex]);
                meshRenderer.SetPropertyBlock(_mpb);

                items.Add(nut);
            }

            _BoltInteraction.PointerDown += OnPointerDown;
            return items;
        }

        public void Teardown() => _BoltInteraction.PointerDown -= OnPointerDown;

        /// <summary>Barın şu anki somunları, alttan üste sırayla (sibling order = slot order).</summary>
        public List<GameObject> ItemsBottomToTop()
        {
            var items = new List<GameObject>(_NutHolder.childCount);
            for (int i = 0; i < _NutHolder.childCount; i++)
                items.Add(_NutHolder.GetChild(i).gameObject);
            return items;
        }

        private void OnPointerDown() => Clicked?.Invoke();

        public Sequence PlayIntroAnimation(IReadOnlyList<GameObject> items) => _motion.PlayIntro(items);

        public Sequence PlaySolvedAnimation()
        {
            DOTween.Kill(_Cap);

            return DOTween.Sequence()
                .Append(_Cap.transform.DOJump(_CapEndPositionHolder.position, 1f, 1, 0.4f).SetEase(Ease.Linear))
                .Join(_Cap.transform.DOLocalRotate(CapSolvedEuler, 0.25f, RotateMode.FastBeyond360))
                .OnComplete(_sounds.PlayCapWin)
                .Append(_Cap.transform.DOPunchScale(Vector3.one * 0.15f, 0.2f, 1))
                .SetId(_Cap)
                .SetLink(_Cap, LinkBehaviour.KillOnDisable);
        }

        public void SetSolvedInstant()
        {
            // PlaySolvedAnimation'ın bitiş pozu: tween'siz, sessiz
            _Cap.transform.position = _CapEndPositionHolder.position;
            _Cap.transform.localEulerAngles = CapSolvedEuler;
        }

        // DiscreteItemBoard.IItemAnimator — isLast: Bolt kendi koreografisini korur, bayrağı kullanmaz.
        public Sequence MoveItem(GameObject item, int targetBar, int targetSlot, Vector3 sourceTopWorldPos, float delay, bool isLast)
            => _motion.MoveToSlot(item, sourceTopWorldPos, targetSlot, delay);

        public void Selected(GameObject item) => _motion.Select(item);
        public void Deselected(GameObject item) => _motion.Deselect(item);
    }
}
