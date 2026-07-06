using DG.Tweening;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace ReplicaProjects.MagicSort
{
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

        private const float NUT_SPACING = 0.5f;
        private const string COLOR_PROPERTY = "_Color";

        private const float INTRO_START_Z = 1f;          // belirme noktası: barın arkası (local z)
        private const float INTRO_STAGGER = 0.4f;        // somunlar arası belirme gecikmesi
        private const float INTRO_TOP_MOVE_DURATION = 0.25f;
        private const float DESELECT_MOVE_DURATION = 0.5f;
        private const float DESELECT_ROTATE_DURATION = 0.65f;

        private static MaterialPropertyBlock _mpb;
        private static int _lastIntroSfxFrame = -1; // aynı frame'de tüm barlar drop başlatınca sesi tek sefer çal

        private List<Color> _palette;
        private Vector3 _nutBaseScale = Vector3.one;

        // Tema root'u palette'i Setup'tan önce enjekte eder
        public void SetPalette(List<Color> palette) => _palette = palette;

        /// <summary>Renk verisinden somunları yaratır. Dönüş: alttan üste sırayla (index 0 = alt slot).</summary>
        public List<GameObject> Setup(BarVisualData data)
        {
            _mpb ??= new MaterialPropertyBlock();

            var items = new List<GameObject>(data.Colors.Length);
            var topLocal = _NutHolder.InverseTransformPoint(_TopPositionHolder.position);

            for (int i = 0; i < data.Colors.Length; i++)
            {
                int colorIndex = data.Colors[i];
                if (colorIndex == MagicSortLevel.Empty)
                    continue;

                GameObject nut = Instantiate(_NutPrefab, _NutHolder);
                _nutBaseScale = nut.transform.localScale;

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

        /// <summary>items: alttan üste sırayla (Setup'ın döndürdüğü sıra); slot = dizideki index.</summary>
        public Sequence PlayIntroAnimation(IReadOnlyList<GameObject> items)
        {
            var sequence = DOTween.Sequence();
            var topLocal = _NutHolder.InverseTransformPoint(_TopPositionHolder.position);

            for (int slot = 0; slot < items.Count; slot++)
            {
                GameObject introNut = items[slot];

                // Alttaki somun önce belirsin: slot 0 → gecikme 0
                float appearTime = slot * INTRO_STAGGER;
                float dropTime = appearTime + INTRO_TOP_MOVE_DURATION;

                var slotPosition = new Vector3(0f, slot * NUT_SPACING, 0f);

                // Beliriş: anında görünür ol
                sequence.InsertCallback(appearTime, () => introNut.transform.localScale = _nutBaseScale);

                // Önce topPositionHolder'a git
                sequence.Insert(appearTime,
                    introNut.transform.DOLocalMove(topLocal, INTRO_TOP_MOVE_DURATION).SetEase(Ease.Linear));

                // Sonra DeSelect animasyonu: slota inerken 360 dön
                sequence.InsertCallback(dropTime, PlayIntroDropSfx);
                sequence.Insert(dropTime,
                    introNut.transform.DOLocalMove(slotPosition, DESELECT_MOVE_DURATION).SetEase(Ease.Linear));
                sequence.Insert(dropTime,
                    introNut.transform.DORotate(new Vector3(0, 360, 0), DESELECT_ROTATE_DURATION, RotateMode.FastBeyond360));
            }

            return sequence.SetLink(gameObject, LinkBehaviour.KillOnDisable);
        }

        private void PlayIntroDropSfx()
        {
            if (Time.frameCount == _lastIntroSfxFrame)
                return;

            _lastIntroSfxFrame = Time.frameCount;
            _AudioSource.PlayOneShot(_Down);
        }

        public Sequence MoveItemToSlotAnimation(GameObject item, Vector3 sourceTopWorldPosition, int targetSlot, float delay)
        {
            DOTween.Kill(item); // item ID'li her şeyi öldürür (wobble dahil)

            item.transform.SetParent(_NutHolder);
            item.transform.eulerAngles = Vector3.zero;
            var nutSlotPosition = new Vector3(0f, targetSlot * NUT_SPACING, 0f);

            var sourceLocalTarget = item.transform.parent.InverseTransformPoint(sourceTopWorldPosition);
            var localTarget = item.transform.parent.InverseTransformPoint(_TopPositionHolder.position);
            var rotationDelayApply = delay > 0 ? 1 : 0;
            var topMovementDelay = delay > 0 ? 0.25f : 0.1f;

            var sequence = DOTween.Sequence()
                .SetDelay(delay)
                .Append(item.transform.DOLocalMove(sourceLocalTarget, topMovementDelay).SetEase(Ease.Linear))
                .Join(item.transform.DORotate(new Vector3(0, -360 * rotationDelayApply, 0), 0.25f * rotationDelayApply, RotateMode.FastBeyond360))
                .Append(item.transform.DOLocalMove(localTarget, 0.25f).SetEase(Ease.Linear)
                    .OnComplete(() =>
                    {
                        _AudioSource.PlayOneShot(_Down);
                    }))
                .Append(item.transform.DOLocalMove(nutSlotPosition, 0.35f).SetEase(Ease.Linear))
                .Join(item.transform.DORotate(new Vector3(0, 360 * 2, 0), 0.35f, RotateMode.FastBeyond360))
                 .OnComplete(() =>
                 {
                     _AudioSource.PlayOneShot(_NutSeat);
                 })
                .SetId(item)
                .SetLink(item, LinkBehaviour.KillOnDisable);

            return sequence;
        }

        public Sequence PlaySolvedAnimation()
        {
            DOTween.Kill(_Cap);

            return DOTween.Sequence()
                .Append(_Cap.transform.DOJump(_CapEndPositionHolder.position, 1f, 1, 0.4f).SetEase(Ease.Linear))
                .Join(_Cap.transform.DOLocalRotate(new Vector3(90, 0, 0), 0.25f, RotateMode.FastBeyond360))
                   .OnComplete(() =>
                   {
                       _AudioSource.PlayOneShot(_CapWin);
                   })
                .Append(_Cap.transform.DOPunchScale(Vector3.one * 0.15f, 0.2f, 1))
                .SetId(_Cap)
                .SetLink(_Cap, LinkBehaviour.KillOnDisable);

        }

        public void SetSolvedInstant()
        {
            // PlaySolvedAnimation'ın bitiş pozu: tween'siz, sessiz
            _Cap.transform.position = _CapEndPositionHolder.position;
            _Cap.transform.localEulerAngles = new Vector3(90, 0, 0);
        }

        // DiscreteItemBoard.IItemAnimator
        // isLast: Bolt kendi koreografisini korur, bayrağı kullanmaz.
        public Sequence MoveItem(GameObject item, int targetBar, int targetSlot, Vector3 sourceTopWorldPos, float delay, bool isLast)
            => MoveItemToSlotAnimation(item, sourceTopWorldPos, targetSlot, delay);

        public void Selected(GameObject item)
        {
            DOTween.Kill(item);

            var localTarget = item.transform.parent.InverseTransformPoint(_TopPositionHolder.position);
            _AudioSource.PlayOneShot(_Up);

            var sequence = DOTween.Sequence()
                .Append(item.transform.DOLocalMove(localTarget, 0.5f).SetEase(Ease.Linear))
                .Join(item.transform.DORotate(new Vector3(0, -360, 0), 0.65f, RotateMode.FastBeyond360))
                .SetId(item)
                .SetLink(item, LinkBehaviour.KillOnDisable);

            sequence.AppendCallback(() =>
            {
                float tilt = 7f, period = 2.5f, fadeInDuration = 0.5f;
                float baseY = item.transform.eulerAngles.y;
                float elapsed = 0f;

                DOVirtual.Float(0f, Mathf.PI * 2f, period, angle =>
                {
                    elapsed += Time.deltaTime;
                    float amplitude = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / fadeInDuration));
                    float x = Mathf.Cos(angle) * tilt * amplitude;
                    float z = -Mathf.Sin(angle) * tilt * amplitude;
                    item.transform.rotation = Quaternion.Euler(x, baseY, z);
                })
                .SetEase(Ease.Linear)
                .SetLoops(-1, LoopType.Restart)
                .SetId(item) // <-- kritik: artık DOTween.Kill(item) ile ölebilir
                .SetLink(item, LinkBehaviour.KillOnDisable);
            });
        }

        public void Deselected(GameObject item)
        {
            DOTween.Kill(item); // item ID'li her şeyi öldürür (wobble dahil)

            // Çocuklar hep alttan üste sırayla eklenir (Setup) / en üste eklenir (MoveItem);
            // bu yüzden sibling index, item'in kendi barındaki slotuna eşittir.
            var pos = new Vector3(0f, item.transform.GetSiblingIndex() * NUT_SPACING, 0f);
            item.transform.eulerAngles = Vector3.zero;

            _AudioSource.PlayOneShot(_Down);

            DOTween.Sequence()
                 .Append(item.transform.DOLocalMove(pos, DESELECT_MOVE_DURATION).SetEase(Ease.Linear))
                 .Join(item.transform.DORotate(new Vector3(0, 360, 0), DESELECT_ROTATE_DURATION, RotateMode.FastBeyond360))
                 .InsertCallback(DESELECT_ROTATE_DURATION - 0.05f, () =>
                 {
                     _AudioSource.PlayOneShot(_NutSeat);
                 })
                 .SetId(item)
                 .SetLink(item, LinkBehaviour.KillOnDisable);
        }
    }
}
