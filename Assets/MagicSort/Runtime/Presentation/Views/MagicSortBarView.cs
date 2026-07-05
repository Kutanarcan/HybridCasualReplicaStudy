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
        [SerializeField] private Transform _TopPositionHolder;
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

        public bool TryPopNut(out GameObject nut)
        {
            return _nutStack.TryPop(out nut);
        }

        public void PushNut(GameObject nut)
        {
            _nutStack.Push(nut);
            nut.transform.SetParent(_NutHolder);
            nut.transform.localPosition = new Vector3(0f, (_nutStack.Count - 1) * NUT_SPACING, 0f);
        }

        public void DeInitialize()
        {
            _BoltInteraction.PointerDown -= BoltInteraction_PointerDown;
        }
    }
}
