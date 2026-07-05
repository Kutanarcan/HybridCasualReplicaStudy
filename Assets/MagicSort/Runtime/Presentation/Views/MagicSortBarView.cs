using DG.Tweening;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ReplicaProjects.MagicSort
{
    public class MagicSortBarView : MonoBehaviour
    {
        public event Action<int> Clicked;

        [SerializeField] private GameObject _NutPrefab;
        [SerializeField] private Transform _NutHolder;
        
        [SerializeField] private Transform _CapEndPositionHolder;
        [SerializeField] private GameObject _Cap;

        [SerializeField] public Transform topPositionHolder;
        [SerializeField] private BoltInteraction _BoltInteraction;

        private const float NUT_SPACING = 0.5f;
        private const string COLOR_PROPERTY = "_Color";

        private static MaterialPropertyBlock _mpb;

        private Stack<GameObject> _nutStack;
        private int _index;

        public void Initialize(List<Color> colors, int index)
        {
            _mpb ??= new MaterialPropertyBlock();
            _nutStack = new Stack<GameObject>(colors.Count);
            _index = index;

            for (int i = 0; i < colors.Count; i++)
            {
                GameObject nut = Instantiate(_NutPrefab, _NutHolder);
                nut.transform.localPosition = new Vector3(0f, i * NUT_SPACING, 0f);

                var meshRenderer = nut.GetComponentInChildren<MeshRenderer>();
                meshRenderer.GetPropertyBlock(_mpb);
                _mpb.SetColor(COLOR_PROPERTY, colors[i]);
                meshRenderer.SetPropertyBlock(_mpb);

                _nutStack.Push(nut);
            }

            _BoltInteraction.PointerDown += BoltInteraction_PointerDown;
        }

        private void BoltInteraction_PointerDown()
        {
            Clicked?.Invoke(_index);
        }

        public bool TryPeekNut(out GameObject nut)
        {
            return _nutStack.TryPeek(out nut);
        }

        public bool TryPopNut(out GameObject nut)
        {
            return _nutStack.TryPop(out nut);
        }

        public void PushNut(GameObject nut)
        {
            DOTween.Kill(nut); // nut ID'li her �eyi �ld�r�r (wobble dahil)

            _nutStack.Push(nut);
            nut.transform.SetParent(_NutHolder);
        }

        public void DeInitialize()
        {
            _BoltInteraction.PointerDown -= BoltInteraction_PointerDown;
        }

        public Sequence MoveBarSlotAnimation(GameObject nut, Vector3 sourceBarTopWorldPosition, float delay)
        {
            DOTween.Kill(nut); // nut ID'li her �eyi �ld�r�r (wobble dahil)

            nut.transform.eulerAngles = Vector3.zero;
            var nutSlotPosition = new Vector3(0f, (_nutStack.Count - 1) * NUT_SPACING, 0f);

            var sourceLocalTarget = nut.transform.parent.InverseTransformPoint(sourceBarTopWorldPosition);
            var localTarget = nut.transform.parent.InverseTransformPoint(topPositionHolder.position);
            var rotationDelayApply = delay > 0 ? 1 : 0;
            var topMovementDelay = delay > 0 ? 0.25f : 0.1f;

            var sequence = DOTween.Sequence()
                .SetDelay(delay)
                .Append(nut.transform.DOLocalMove(sourceLocalTarget, topMovementDelay).SetEase(Ease.Linear))
                .Join(nut.transform.DORotate(new Vector3(0, -360 * rotationDelayApply, 0), 0.25f * rotationDelayApply, RotateMode.FastBeyond360))
                .Append(nut.transform.DOLocalMove(localTarget, 0.25f).SetEase(Ease.Linear))
                .Append(nut.transform.DOLocalMove(nutSlotPosition, 0.35f).SetEase(Ease.Linear))
                .Join(nut.transform.DORotate(new Vector3(0, 360, 0), 0.35f, RotateMode.FastBeyond360))
                .SetId(nut)
                .SetLink(nut, LinkBehaviour.KillOnDisable);

            return sequence;
        }

        public void PlayCapSolvedAnimation()
        {
            DOTween.Kill(_Cap);

            DOTween.Sequence()
                .Append(_Cap.transform.DOJump(_CapEndPositionHolder.position, 1f, 1, 0.4f).SetEase(Ease.Linear))
                .Join(_Cap.transform.DOLocalRotate(new Vector3(90, 0, 0), 0.25f, RotateMode.FastBeyond360))
                .Append(_Cap.transform.DOPunchScale(Vector3.one * 0.15f, 0.2f, 1))
                .SetId(_Cap)
                .SetLink(_Cap, LinkBehaviour.KillOnDisable);
        }

        public void MoveNutDeSelectedAnimation(GameObject nut)
        {
            DOTween.Kill(nut); // nut ID'li her �eyi �ld�r�r (wobble dahil)

            var pos = new Vector3(0f, (_nutStack.Count - 1) * NUT_SPACING, 0f);
            nut.transform.eulerAngles = Vector3.zero;

            DOTween.Sequence()
                .Append(nut.transform.DOLocalMove(pos, 0.5f).SetEase(Ease.Linear))
                .Join(nut.transform.DORotate(new Vector3(0, 360, 0), 0.65f, RotateMode.FastBeyond360))
                .SetId(nut)
                .SetLink(nut, LinkBehaviour.KillOnDisable);
        }

        public void MoveNutSelectedAnimation(GameObject nut)
        {
            DOTween.Kill(nut);

            var localTarget = nut.transform.parent.InverseTransformPoint(topPositionHolder.position);

            var sequence = DOTween.Sequence()
                .Append(nut.transform.DOLocalMove(localTarget, 0.5f).SetEase(Ease.Linear))
                .Join(nut.transform.DORotate(new Vector3(0, -360, 0), 0.65f, RotateMode.FastBeyond360))
                .SetId(nut)
                .SetLink(nut, LinkBehaviour.KillOnDisable);

            sequence.AppendCallback(() =>
            {
                float tilt = 7f, period = 2.5f, fadeInDuration = 0.5f;
                float baseY = nut.transform.eulerAngles.y;
                float elapsed = 0f;

                DOVirtual.Float(0f, Mathf.PI * 2f, period, angle =>
                {
                    elapsed += Time.deltaTime;
                    float amplitude = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / fadeInDuration));
                    float x = Mathf.Cos(angle) * tilt * amplitude;
                    float z = -Mathf.Sin(angle) * tilt * amplitude;
                    nut.transform.rotation = Quaternion.Euler(x, baseY, z);
                })
                .SetEase(Ease.Linear)
                .SetLoops(-1, LoopType.Restart)
                .SetId(nut) // <-- kritik: art�k DOTween.Kill(nut) ile �lebilir
                .SetLink(nut, LinkBehaviour.KillOnDisable);
            });
        }
    }
}
